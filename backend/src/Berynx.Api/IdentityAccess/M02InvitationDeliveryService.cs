using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.IdentityAccess;

public sealed class M02InvitationDeliveryService(
    IServiceScopeFactory scopes,
    ILogger<M02InvitationDeliveryService> logger) : BackgroundService
{
    private sealed record TenantScope(Guid TenantId) : ICurrentTenantContext;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<IM02InvitationDeliveryProvider>();
                var inviteTokens = scope.ServiceProvider.GetRequiredService<M02InviteTokenService>();
                var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<BerynxDbContext>>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<M02IdentityUser>>();
                await DeliverOne(provider, inviteTokens, users, options, stoppingToken);
            }
            catch (Exception error) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(error, "M02 invitation delivery deferred.");
            }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task DeliverOne(IM02InvitationDeliveryProvider provider, M02InviteTokenService inviteTokens,
        UserManager<M02IdentityUser> users, DbContextOptions<BerynxDbContext> options,
        CancellationToken cancellationToken)
    {
        await using var discovery = new BerynxDbContext(options, new TenantScope(Guid.Empty));
        var candidate = await discovery.Database.SqlQueryRaw<DeliveryCandidate>("""
            SELECT "TenantId", "UserId"
            FROM m02_user
            WHERE "Status"='Invited' AND "InviteExpiresAt">now()
              AND "InvitationDeliveryId" IS NOT NULL AND "InvitationDeliveredAt" IS NULL
              AND "InvitationAvailableAt"<=now()
              AND NOT EXISTS (
                SELECT 1 FROM onboarding_invitation i
                WHERE i.tenant_id=m02_user."TenantId" AND i.email=m02_user."Email"
                  AND i.delivery_id=m02_user."InvitationDeliveryId")
            ORDER BY "InvitationAvailableAt"
            LIMIT 1
            """).FirstOrDefaultAsync(cancellationToken);
        if (candidate is null)
            return;

        await using var db = new BerynxDbContext(options, new TenantScope(candidate.TenantId));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var locked = await db.Database.SqlQuery<Guid>($"""
            SELECT "UserId" AS "Value" FROM m02_user
            WHERE "TenantId"={candidate.TenantId} AND "UserId"={candidate.UserId}
              AND "Status"='Invited' AND "InviteExpiresAt">now()
              AND "InvitationDeliveryId" IS NOT NULL AND "InvitationDeliveredAt" IS NULL
              AND "InvitationAvailableAt"<=now()
              AND NOT EXISTS (
                SELECT 1 FROM onboarding_invitation i
                WHERE i.tenant_id=m02_user."TenantId" AND i.email=m02_user."Email"
                  AND i.delivery_id=m02_user."InvitationDeliveryId")
            FOR UPDATE SKIP LOCKED
            """).FirstOrDefaultAsync(cancellationToken);
        if (locked == Guid.Empty)
            return;

        var user = await db.Set<M02User>().SingleAsync(x => x.UserId == candidate.UserId, cancellationToken);
        var roleKeys = await (
            from assignment in db.Set<M02UserRole>()
            join role in db.Set<M02Role>()
                on new { assignment.TenantId, assignment.RoleId } equals new { role.TenantId, role.RoleId }
            where assignment.UserId == user.UserId
            select role.RoleKey
        ).Distinct().ToArrayAsync(cancellationToken);
        var deliveryId = user.InvitationDeliveryId!.Value;
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("Local password activation requires an email-linked identity.");
        if (user.IdentityUserId is null)
        {
            var identity = await users.FindByEmailAsync(user.Email);
            if (identity is null)
            {
                identity = new M02IdentityUser
                {
                    UserName = user.Email,
                    Email = user.Email,
                    LockoutEnabled = true
                };
                var created = await users.CreateAsync(identity);
                if (!created.Succeeded)
                    identity = await users.FindByEmailAsync(user.Email);
                if (identity is null)
                    throw new InvalidOperationException("Could not create the invited Identity account.");
            }
            user.IdentityUserId = identity.Id;
            user.ProviderSubject = identity.Id.ToString("D");
            user.Version++;
            await db.SaveChangesAsync(cancellationToken);
        }
        var activationUrl = inviteTokens.CreateUrl(
            new M02InvitationTicket(user.IdentityUserId.Value, user.TenantId, user.UserId, deliveryId),
            user.Email);
        var invitation = new M02InvitationDelivery(user.TenantId, user.UserId, user.Email, user.Mobile,
            roleKeys, deliveryId, user.InviteExpiresAt!.Value, activationUrl);
        try
        {
            await provider.DeliverInvitation(invitation, cancellationToken);
            user.InvitationDeliveredAt = DateTimeOffset.UtcNow;
            user.Version++;
            db.Add(Audit(user, "Invitation delivered", new
            {
                deliveryId, user.Email, user.Mobile, roleKeys, user.InviteExpiresAt
            }));
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            user.InvitationAttempts++;
            user.InvitationAvailableAt = DateTimeOffset.UtcNow.AddMinutes(
                Math.Min(60, Math.Pow(2, Math.Min(user.InvitationAttempts, 6))));
            user.Version++;
            db.Add(Audit(user, "Invitation delivery failed", new
            {
                deliveryId, attempt = user.InvitationAttempts, retryAt = user.InvitationAvailableAt
            }));
            logger.LogWarning("Identity provider invitation failed with {ExceptionType}; retry scheduled.",
                error.GetType().Name);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static M02AuditEvent Audit(M02User user, string action, object after) => new()
    {
        TenantId = user.TenantId,
        EventId = Guid.NewGuid(),
        ActorSubject = "identity-provider-worker",
        Action = action,
        EntityType = "User",
        EntityId = user.UserId,
        Before = "{}",
        After = System.Text.Json.JsonSerializer.Serialize(after),
        At = DateTimeOffset.UtcNow
    };

    private sealed class DeliveryCandidate
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
    }
}
