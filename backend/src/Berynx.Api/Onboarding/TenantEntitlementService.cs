using System.Security.Claims;
using System.Text.Json;
using Berynx.Application.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Onboarding;

public sealed class TenantApiQuotaExceededException()
    : Exception("This plan's monthly API request quota has been reached. Contact your Platform Admin to review or upgrade the plan.");

public sealed class TenantEntitlementService(
    BerynxDbContext db,
    ICurrentTenantContext tenantScope,
    IHttpContextAccessor http)
{
    public async Task RequireModule(
        string moduleCode,
        bool write,
        bool cancelledExport = false,
        CancellationToken cancellationToken = default)
    {
        var context = http.HttpContext;
        var principal = context?.User;
        if (principal?.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(principal.FindFirstValue("sub"), out var identityUserId) ||
            !Guid.TryParse(principal.FindFirstValue("tenant_id"), out var tenantId) ||
            tenantId != tenantScope.TenantId ||
            !Guid.TryParse(principal.FindFirstValue("user_id"), out var userId))
            throw new UnauthorizedAccessException("An active tenant session is required.");

        var membership = await db.Set<M02User>().AsNoTracking().SingleOrDefaultAsync(
            x => x.UserId == userId && x.IdentityUserId == identityUserId && x.Status == "Active",
            cancellationToken);
        if (membership is null)
            throw new UnauthorizedAccessException("An active tenant membership is required.");

        var tenant = await db.Set<Tenant>().AsNoTracking().SingleOrDefaultAsync(
            x => x.TenantId == tenantId, cancellationToken);
        if (tenant is null)
            throw new UnauthorizedAccessException("The tenant is unavailable.");

        var subscription = tenant.CurrentSubscriptionId is { } subscriptionId
            ? await db.Set<TenantSubscription>().AsNoTracking().SingleOrDefaultAsync(
                x => x.SubscriptionId == subscriptionId && x.IsCurrent, cancellationToken)
            : null;

        var now = DateTimeOffset.UtcNow;
        var status = subscription is null
            ? tenant.Status
            : SubscriptionLifecycle.EffectiveStatus(subscription, now);
        if (subscription is not null && now < subscription.StartsAt)
            throw new UnauthorizedAccessException("The tenant subscription is not effective yet.");

        if (status is SubscriptionLifecycle.Suspended or SubscriptionLifecycle.Cancelled)
        {
            var canExport = !write && cancelledExport && status == SubscriptionLifecycle.Cancelled;
            if (!canExport && (write || status == SubscriptionLifecycle.Cancelled))
                throw new UnauthorizedAccessException(
                    status == SubscriptionLifecycle.Cancelled
                        ? "This tenant is cancelled. Use the permitted data export flow."
                        : "This tenant is read-only while its subscription is suspended.");
        }

        var enabled = await db.Set<TenantModuleEntitlement>().AsNoTracking().AnyAsync(
            x => x.ModuleCode == moduleCode && x.IsEnabled && x.EffectiveFrom <= now &&
                 (x.EffectiveUntil == null || x.EffectiveUntil > now),
            cancellationToken);

        // Existing M01 tenants predate the catalogue. Preserve their server-stored
        // module snapshot until a PlatformAdmin assigns a catalog-backed subscription.
        if (subscription is null && !enabled)
        {
            var legacyModules = JsonSerializer.Deserialize<Setup>(tenant.Configuration,
                Workflow.Json)?.Modules ?? [];
            enabled = legacyModules.Contains(moduleCode, StringComparer.Ordinal);
        }

        if (!enabled)
            throw new UnauthorizedAccessException($"The {moduleCode} module is not enabled for this tenant.");

        var quotaExempt = cancelledExport ||
            context!.Request.Path == "/api/v1/onboarding/usage";
        if (subscription is not null && !quotaExempt)
        {
            var meterKey = $"saas-api-metered:{tenantId}:{now:yyyy-MM}";
            if (!context!.Items.ContainsKey(meterKey))
            {
                var setup = JsonSerializer.Deserialize<Setup>(tenant.Configuration, Workflow.Json)
                    ?? throw new UnauthorizedAccessException("Tenant limits are unavailable.");
                if (setup.Limits.ApiRequests < 1)
                    throw new UnauthorizedAccessException("The tenant API quota is unavailable.");

                var monthStart = new DateOnly(now.Year, now.Month, 1);
                var enforceLimit = status != SubscriptionLifecycle.Suspended || write;
                var accepted = await db.Database.SqlQuery<long>($"""
                    INSERT INTO platform_tenant_api_usage ("TenantId", "MonthStart", "RequestCount")
                    VALUES ({tenantId}, {monthStart}, 1)
                    ON CONFLICT ("TenantId", "MonthStart") DO UPDATE
                    SET "RequestCount" = platform_tenant_api_usage."RequestCount" + 1
                    WHERE NOT {enforceLimit} OR
                          platform_tenant_api_usage."RequestCount" < {setup.Limits.ApiRequests}
                    RETURNING "RequestCount" AS "Value"
                    """).SingleOrDefaultAsync(cancellationToken);
                if (accepted == 0)
                    throw new TenantApiQuotaExceededException();
                context.Items[meterKey] = true;
            }
        }
    }

    public async Task<IReadOnlyList<string>> EnabledModules(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tenant = await db.Set<Tenant>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantScope.TenantId, cancellationToken);
        if (tenant is null)
            return [];
        var subscription = tenant.CurrentSubscriptionId is { } subscriptionId
            ? await db.Set<TenantSubscription>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.SubscriptionId == subscriptionId && x.IsCurrent, cancellationToken)
            : null;
        if (subscription is not null &&
            (now < subscription.StartsAt ||
             SubscriptionLifecycle.EffectiveStatus(subscription, now) == SubscriptionLifecycle.Cancelled))
            return [];

        var modules = await db.Set<TenantModuleEntitlement>().AsNoTracking()
            .Where(x => x.IsEnabled && x.EffectiveFrom <= now &&
                        (x.EffectiveUntil == null || x.EffectiveUntil > now))
            .Select(x => x.ModuleCode)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
        if (modules.Count > 0)
            return modules;

        if (subscription is not null)
            return [];
        return JsonSerializer.Deserialize<Setup>(tenant.Configuration, Workflow.Json)?.Modules ?? [];
    }

    public async Task EnsureInviteCapacity(CancellationToken cancellationToken = default)
    {
        var tenant = await db.Set<Tenant>().SingleOrDefaultAsync(
            x => x.TenantId == tenantScope.TenantId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The tenant is unavailable.");
        var setup = JsonSerializer.Deserialize<Setup>(tenant.Configuration, Workflow.Json)
            ?? throw new UnauthorizedAccessException("Tenant limits are unavailable.");
        var users = await db.Set<M02User>().LongCountAsync(
            x => x.Status == "Active" || x.Status == "Invited", cancellationToken);
        SetupRules.Capacity(users, 1, setup.Limits.Users);

        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var messages = await db.Set<M02AuditEvent>().LongCountAsync(
            x => x.At >= monthStart &&
                 (x.Action == "Invite" || x.Action == "First-admin invitation queued" ||
                  x.Action == "Resend invitation"),
            cancellationToken);
        SetupRules.Capacity(messages, 1, setup.Limits.Messages);
    }
}
