using System.Net.Http.Headers;
using Berynx.Api.IdentityAccess;
using Berynx.Domain.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Onboarding;
// The identity provider provisions the invitee and delivers its password/MFA activation email.
public sealed class InvitationDelivery(IServiceScopeFactory scopes, IHttpClientFactory clients, IConfiguration config, ILogger<InvitationDelivery> log) : BackgroundService
{
    public sealed record Delivery(Guid TenantId, string Email, string Role, Guid DeliveryId);
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        if (!Uri.TryCreate(config["Onboarding:InvitationEndpoint"], UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" || string.IsNullOrWhiteSpace(config["Onboarding:InvitationCredential"]))
            return;
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BerynxDbContext>();
                var inviteTokens = scope.ServiceProvider.GetRequiredService<M02InviteTokenService>();
                await using var tx = await db.Database.BeginTransactionAsync(stop);
                var batch = await db.Database.SqlQueryRaw<Delivery>("""
                    SELECT i.tenant_id AS "TenantId",i.email AS "Email",i.role AS "Role",i.delivery_id AS "DeliveryId"
                    FROM onboarding_invitation i JOIN onboarding_tenant t ON t."TenantId"=i.tenant_id
                    WHERE i.delivered_at IS NULL AND i.available_at<=now() AND t."Status" IN ('Trial','Active')
                    ORDER BY i.available_at LIMIT 1 FOR UPDATE OF i,t SKIP LOCKED
                    """).ToListAsync(stop);
                if (batch.Count != 0)
                {
                    var job = batch[0];
                    var membership = await db.Set<M02User>().IgnoreQueryFilters().SingleOrDefaultAsync(x =>
                        x.TenantId == job.TenantId && x.Email == job.Email &&
                        x.InvitationDeliveryId == job.DeliveryId && x.Status == "Invited", stop)
                        ?? throw new InvalidOperationException(
                            "First-admin Identity membership is not ready for invitation delivery.");
                    if (membership.IdentityUserId is null)
                        throw new InvalidOperationException(
                            "First-admin invitation is missing its local Identity account.");
                    var activationUrl = inviteTokens.CreateUrl(new M02InvitationTicket(
                        membership.IdentityUserId.Value, job.TenantId, membership.UserId, job.DeliveryId), job.Email);
                    using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = JsonContent.Create(new
                        {
                            job.TenantId,
                            job.Email,
                            job.Role,
                            job.DeliveryId,
                            activationUrl
                        })
                    };
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config["Onboarding:InvitationCredential"]);
                    message.Headers.Add("Idempotency-Key", job.DeliveryId.ToString());
                    using var client = clients.CreateClient("invitation");
                    using var result = await client.SendAsync(message, stop);
                    if (result.IsSuccessStatusCode)
                    {
                        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE onboarding_invitation SET delivered_at=now() WHERE tenant_id={job.TenantId} AND delivery_id={job.DeliveryId}", stop);
                        await db.Database.ExecuteSqlInterpolatedAsync($"""
                            UPDATE m02_user SET "InvitationDeliveredAt"=now(), "Version"="Version"+1
                            WHERE "TenantId"={job.TenantId} AND "UserId"={membership.UserId}
                              AND "InvitationDeliveryId"={job.DeliveryId} AND "Status"='Invited'
                            """, stop);
                        await db.Database.ExecuteSqlInterpolatedAsync($"""
                            INSERT INTO onboarding_event ("TenantId","Id","Actor","Action","Before","After","At")
                            VALUES ({job.TenantId},{job.DeliveryId},'invitation-worker','Invitation delivered',jsonb_build_object(),jsonb_build_object(),now())
                            """, stop);
                    }
                    else
                    {
                        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE onboarding_invitation SET attempts=attempts+1,available_at=now()+interval '5 minutes' WHERE tenant_id={job.TenantId} AND delivery_id={job.DeliveryId}", stop);
                        log.LogWarning("Invitation provider returned HTTP {Status}", (int)result.StatusCode);
                    }
                }

                await tx.CommitAsync(stop);
            }
            catch (Exception e)when (!stop.IsCancellationRequested)
            {
                log.LogWarning(e, "Invitation delivery deferred");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stop);
        }
    }
}
