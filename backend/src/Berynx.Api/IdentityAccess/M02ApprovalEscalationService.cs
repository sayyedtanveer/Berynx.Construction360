using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.IdentityAccess;

public sealed class M02ApprovalEscalationService(
    IServiceScopeFactory scopes,
    ILogger<M02ApprovalEscalationService> logger) : BackgroundService
{
    private sealed class TenantScope(Guid tenantId) : ICurrentTenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class DueRequest
    {
        public Guid TenantId { get; set; }
        public Guid RequestId { get; set; }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EscalateDueRequests(stoppingToken);
            }
            catch (Exception error) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(error, "Approval SLA escalation pass failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task EscalateDueRequests(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<BerynxDbContext>>();
        await using var discovery = new BerynxDbContext(options, new TenantScope(Guid.Empty));
        var due = await discovery.Database.SqlQueryRaw<DueRequest>("""
            SELECT "TenantId", "RequestId"
            FROM m02_approval_request
            WHERE "Status"='Pending' AND "DueAt"<=now() AND "EscalatedAt" IS NULL
            ORDER BY "DueAt"
            LIMIT 100
            """).ToListAsync(cancellationToken);

        foreach (var item in due)
        {
            await using var db = new BerynxDbContext(options, new TenantScope(item.TenantId));
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var request = await db.Set<M02ApprovalRequest>().SingleOrDefaultAsync(x =>
                x.RequestId == item.RequestId && x.Status == "Pending" &&
                x.DueAt <= DateTimeOffset.UtcNow && x.EscalatedAt == null, cancellationToken);
            if (request is null)
                continue;
            var level = await db.Set<M02ApprovalLevel>().SingleAsync(x =>
                x.RuleId == request.RuleId && x.Level == request.CurrentLevel, cancellationToken);
            var previousEscalatedAt = request.EscalatedAt;
            var roleId = level.EscalationRoleId ?? level.ApproverRoleId;
            var userId = level.EscalationUserId ?? level.ApproverUserId;
            var candidates = userId.HasValue
                ? [userId.Value]
                : await db.Set<M02UserRole>().Where(x => x.RoleId == roleId)
                    .Select(x => x.UserId).Distinct().ToArrayAsync(cancellationToken);
            var access = new M02AccessService(db, new HttpContextAccessor());
            var recipients = new List<Guid>();
            foreach (var recipient in candidates)
                if (recipient != request.RequesterUserId &&
                    await access.CanAccess(recipient, "approvals.view", request.BranchId,
                        request.PlantId, request.OwnerUserId, cancellationToken) &&
                    await access.CanAccess(recipient, "approvals.approve", request.BranchId,
                        request.PlantId, request.OwnerUserId, cancellationToken))
                    recipients.Add(recipient);
            var now = DateTimeOffset.UtcNow;
            request.EscalatedAt = now;
            request.Version++;
            foreach (var recipient in recipients)
                db.Add(new M02Notification
                {
                    TenantId = item.TenantId,
                    NotificationId = Guid.NewGuid(),
                    RecipientUserId = recipient,
                    Kind = "ApprovalEscalated",
                    EntityId = request.RequestId,
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        request.SourceType, request.SourceId, request.CurrentLevel, request.DueAt
                    }),
                    CreatedAt = now
                });
            db.Add(new M02AuditEvent
            {
                TenantId = item.TenantId,
                EventId = Guid.NewGuid(),
                ActorSubject = "approval-sla-worker",
                Action = "Approval SLA escalated",
                EntityType = "ApprovalRequest",
                EntityId = request.RequestId,
                Before = System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.Status, escalatedAt = previousEscalatedAt, request.CurrentLevel, request.DueAt
                }),
                After = System.Text.Json.JsonSerializer.Serialize(new { request.CurrentLevel, request.DueAt }),
                At = now
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
    }
}
