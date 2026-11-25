using System.Security.Claims;
using System.Text.Json;
using Berynx.Application.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Onboarding;

public static class PlatformSaaSEndpoints
{
    public sealed record PlanWrite(
        string PlanCode,
        string Name,
        string Description,
        long MaxUsers,
        long MaxStorageBytes,
        long MaxDocumentsPerMonth,
        long MaxMessagesPerMonth,
        long MaxApiRequestsPerMonth,
        int TrialDays,
        int GracePeriodDays,
        decimal? PriceAmount,
        string Currency,
        string[] IncludedModules,
        long Version = 0);

    public sealed record SubscriptionAssignment(
        string PlanCode,
        string[] AddOnModules,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string Status,
        long Version,
        string Reason);

    public sealed record SubscriptionAction(
        long Version,
        string Reason,
        DateTimeOffset? EndsAt = null);
    public sealed record TenantProvisionRequest(
        Guid RequestId,
        string Name,
        string Plan,
        string AdminEmail,
        string[] AddOnModules);

    private sealed record PlanTerms(
        string PlanCode,
        long PlanVersion,
        long MaxUsers,
        long MaxStorageBytes,
        long MaxDocumentsPerMonth,
        long MaxMessagesPerMonth,
        long MaxApiRequestsPerMonth,
        int TrialDays,
        int GracePeriodDays,
        decimal? PriceAmount,
        string Currency,
        string[] IncludedModules,
        string[] AddOnModules);
    private sealed record UsageQuotas(
        long users, long storageBytes, long documents, long messages, long apiRequests);
    private sealed record UsageSnapshot(
        long users, long storageBytes, long documents, long messages, long apiRequests,
        UsageQuotas quotas);

    public static void MapPlatformSaaS(this WebApplication app)
    {
        var platform = app.MapGroup("/api/v1/platform")
            .RequireAuthorization(policy => policy.RequireRole("PlatformAdmin").RequireClaim("sub"));

        platform.MapGet("/plans", async (BerynxDbContext db, CancellationToken ct) =>
        {
            var plans = await db.Set<PlatformPlan>().AsNoTracking()
                .OrderBy(x => x.Name).ToListAsync(ct);
            var modules = await db.Set<PlatformPlanModule>().AsNoTracking()
                .GroupBy(x => x.PlanCode)
                .ToDictionaryAsync(x => x.Key, x => x.Select(m => m.ModuleCode).OrderBy(m => m).ToArray(), ct);
            return plans.Select(plan => new
            {
                plan.PlanCode,
                plan.Name,
                plan.Description,
                plan.IsActive,
                plan.MaxUsers,
                plan.MaxStorageBytes,
                plan.MaxDocumentsPerMonth,
                plan.MaxMessagesPerMonth,
                plan.MaxApiRequestsPerMonth,
                plan.TrialDays,
                plan.GracePeriodDays,
                plan.PriceAmount,
                plan.Currency,
                plan.Version,
                includedModules = modules.GetValueOrDefault(plan.PlanCode, [])
            });
        });

        platform.MapPost("/plans", async (PlanWrite request, BerynxDbContext db, CancellationToken ct) =>
        {
            var plan = ToPlan(request);
            plan.Validate();
            if (await db.Set<PlatformPlan>().AnyAsync(x => x.PlanCode == plan.PlanCode, ct))
                return Results.Conflict(new { detail = "A plan with this code already exists." });
            var modules = await ValidatePlanModules(db, request.IncludedModules, ct);
            if (modules is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["includedModules"] = ["Select active catalogue modules and include every core module."]
                });
            db.Add(plan);
            db.AddRange(modules.Select(code => new PlatformPlanModule
            {
                PlanCode = plan.PlanCode,
                ModuleCode = code
            }));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/platform/plans/{plan.PlanCode}", new { plan.PlanCode, plan.Version });
        });

        platform.MapPut("/plans/{code}", async (
            string code, PlanWrite request, BerynxDbContext db, CancellationToken ct) =>
        {
            var plan = await db.Set<PlatformPlan>().SingleOrDefaultAsync(x => x.PlanCode == code, ct);
            if (plan is null)
                return Results.NotFound();
            if (request.Version != plan.Version)
                return Results.Conflict(new { detail = "The plan changed. Reload it before saving." });
            var proposed = ToPlan(request);
            proposed.PlanCode = code;
            proposed.Validate();
            var modules = await ValidatePlanModules(db, request.IncludedModules, ct);
            if (modules is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["includedModules"] = ["Select active catalogue modules and include every core module."]
                });

            plan.Name = proposed.Name;
            plan.Description = proposed.Description;
            plan.IsActive = proposed.IsActive;
            plan.MaxUsers = proposed.MaxUsers;
            plan.MaxStorageBytes = proposed.MaxStorageBytes;
            plan.MaxDocumentsPerMonth = proposed.MaxDocumentsPerMonth;
            plan.MaxMessagesPerMonth = proposed.MaxMessagesPerMonth;
            plan.MaxApiRequestsPerMonth = proposed.MaxApiRequestsPerMonth;
            plan.TrialDays = proposed.TrialDays;
            plan.GracePeriodDays = proposed.GracePeriodDays;
            plan.PriceAmount = proposed.PriceAmount;
            plan.Currency = proposed.Currency;
            plan.Version++;
            var existingModules = await db.Set<PlatformPlanModule>()
                .Where(x => x.PlanCode == code).ToListAsync(ct);
            db.RemoveRange(existingModules);
            db.AddRange(modules.Select(module => new PlatformPlanModule
            {
                PlanCode = code,
                ModuleCode = module
            }));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { plan.PlanCode, plan.Version });
        });

        platform.MapGet("/modules", async (BerynxDbContext db, CancellationToken ct) =>
            await db.Set<PlatformModule>().AsNoTracking()
                .OrderByDescending(x => x.IsCore).ThenBy(x => x.SortOrder)
                .Select(x => new { x.ModuleCode, x.Name, x.Description, x.IsCore, x.IsActive, x.SortOrder })
                .ToListAsync(ct));

        platform.MapGet("/tenants/{tenantId:guid}", async (
            Guid tenantId, DbContextOptions<BerynxDbContext> options, CancellationToken ct) =>
        {
            await using var db = NewTenantDb(options, tenantId);
            var tenant = await db.Set<Tenant>().SingleOrDefaultAsync(ct);
            if (tenant is null)
                return Results.NotFound();
            var subscription = tenant.CurrentSubscriptionId is { } subscriptionId
                ? await db.Set<TenantSubscription>().AsNoTracking()
                    .SingleOrDefaultAsync(x => x.SubscriptionId == subscriptionId, ct)
                : null;
            var setup = OnboardingStore.Decode(tenant);
            var usage = await Usage(db, tenant, setup, ct);
            var entitlements = await db.Set<TenantModuleEntitlement>().AsNoTracking()
                .OrderBy(x => x.ModuleCode)
                .Select(x => new { x.ModuleCode, x.Source, x.IsEnabled, x.EffectiveFrom, x.EffectiveUntil })
                .ToListAsync(ct);
            return Results.Ok(new
            {
                tenant.TenantId,
                tenant.Name,
                tenant.Plan,
                status = subscription is null
                    ? tenant.Status
                    : SubscriptionLifecycle.EffectiveStatus(subscription, DateTimeOffset.UtcNow),
                tenant.Version,
                subscription,
                usage,
                entitlements,
                exportUntil = tenant.CancelledAt?.AddDays(30)
            });
        });

        platform.MapGet("/tenants/{tenantId:guid}/audit", async (
            Guid tenantId, DbContextOptions<BerynxDbContext> options, CancellationToken ct) =>
        {
            await using var db = NewTenantDb(options, tenantId);
            if (!await db.Set<Tenant>().AnyAsync(ct))
                return Results.NotFound();
            var audit = await db.Set<SaaSLicenseAuditEvent>().AsNoTracking()
                .OrderByDescending(x => x.At).Take(200)
                .Select(x => new
                {
                    x.EventId, x.ActorEmail, x.Action, x.EntityType, x.EntityId,
                    x.Reason, x.Before, x.After, x.At, x.Origin
                })
                .ToListAsync(ct);
            return Results.Ok(audit);
        });

        platform.MapPut("/tenants/{tenantId:guid}/subscription", async (
            Guid tenantId,
            SubscriptionAssignment request,
            DbContextOptions<BerynxDbContext> options,
            ClaimsPrincipal principal,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["reason"] = ["A reason is required."] });
            if (request.Status is not (SubscriptionLifecycle.Trial or SubscriptionLifecycle.Active))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Choose Trial or Active for a plan assignment."] });
            if (request.StartsAt >= request.EndsAt)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["endsAt"] = ["End time must follow the effective start."] });
            if (request.EndsAt <= DateTimeOffset.UtcNow)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["endsAt"] = ["Choose a future end date; use renewal controls to extend an existing term."] });
            await using var db = NewTenantDb(options, tenantId);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var tenant = await db.Set<Tenant>().SingleOrDefaultAsync(ct);
            if (tenant is null)
                return Results.NotFound();
            var current = tenant.CurrentSubscriptionId is { } currentId
                ? await db.Set<TenantSubscription>().SingleOrDefaultAsync(x => x.SubscriptionId == currentId, ct)
                : null;
            var now = DateTimeOffset.UtcNow;
            if (current is not null && request.StartsAt > now)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["startsAt"] = ["A future plan change cannot replace an effective subscription. Choose the current effective date."]
                });
            if ((current?.Version ?? 0) != request.Version)
                return Results.Conflict(new { detail = "The subscription changed. Reload tenant details before applying this change." });
            if (tenant.Status == SubscriptionLifecycle.Cancelled ||
                current?.Status == SubscriptionLifecycle.Cancelled)
                return Results.Conflict(new { detail = "Cancelled tenants are terminal; provision a new tenant instead." });
            if (current is not null &&
                SubscriptionLifecycle.EffectiveStatus(current, now) == SubscriptionLifecycle.Suspended)
                return Results.Conflict(new { detail = "Reactivate the suspended subscription before changing its plan." });

            var plan = await db.Set<PlatformPlan>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.PlanCode == request.PlanCode && x.IsActive, ct);
            if (plan is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["planCode"] = ["Choose an active plan."] });
            var included = await db.Set<PlatformPlanModule>().AsNoTracking()
                .Where(x => x.PlanCode == plan.PlanCode)
                .Select(x => x.ModuleCode).ToArrayAsync(ct);
            var addons = await ValidateAddOns(db, request.AddOnModules, ct);
            if (addons is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["addOnModules"] = ["Choose active non-core modules not already included in the plan."] });
            if (included.Length == 0 || !await IncludesAllCoreModules(db, included, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["planCode"] = ["The plan must include all required core modules."] });

            var setup = OnboardingStore.Decode(tenant);
            var usage = await Usage(db, tenant, setup, ct);
            if (usage.users > plan.MaxUsers || usage.storageBytes > plan.MaxStorageBytes ||
                usage.documents > plan.MaxDocumentsPerMonth || usage.messages > plan.MaxMessagesPerMonth ||
                usage.apiRequests > plan.MaxApiRequestsPerMonth)
                return Results.Conflict(new { detail = "The proposed plan is below current usage. Reduce usage or choose a plan with sufficient limits." });

            var modules = included.Concat(addons).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();
            var terms = new PlanTerms(
                plan.PlanCode, plan.Version, plan.MaxUsers, plan.MaxStorageBytes,
                plan.MaxDocumentsPerMonth, plan.MaxMessagesPerMonth, plan.MaxApiRequestsPerMonth,
                plan.TrialDays, plan.GracePeriodDays, plan.PriceAmount, plan.Currency,
                included, addons);
            var before = current is null ? "{}" : current.TermsSnapshot;
            var isNewSubscription = current is null;
            if (current is null)
            {
                current = new TenantSubscription
                {
                    TenantId = tenantId,
                    SubscriptionId = Guid.NewGuid(),
                    PlanCode = plan.PlanCode,
                    PlanVersion = plan.Version,
                    Version = 1
                };
                db.Add(current);
                tenant.CurrentSubscriptionId = current.SubscriptionId;
            }
            current!.PlanCode = plan.PlanCode;
            current.PlanVersion = plan.Version;
            if (isNewSubscription)
                current.Status = request.Status;
            current.StartsAt = request.StartsAt;
            current.EndsAt = request.EndsAt;
            current.GraceEndsAt = request.EndsAt.AddDays(plan.GracePeriodDays);
            current.IsCurrent = true;
            current.TermsSnapshot = JsonSerializer.Serialize(terms, Workflow.Json);
            if (!isNewSubscription)
                current.Version++;
            tenant.Plan = plan.PlanCode;
            tenant.Status = SubscriptionLifecycle.EffectiveStatus(current, now);
            tenant.Version++;
            setup.Modules = modules;
            setup.Limits = new Limits(plan.MaxUsers, plan.MaxStorageBytes,
                plan.MaxDocumentsPerMonth, plan.MaxMessagesPerMonth, plan.MaxApiRequestsPerMonth);
            tenant.Configuration = OnboardingStore.Encode(setup);
            await ReplaceEntitlements(db, tenantId, modules, included, request.StartsAt, ct);
            AddAudit(db, tenantId, principal, http, current.SubscriptionId, "Plan assigned",
                "Subscription", request.Reason, before, current.TermsSnapshot);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { current.SubscriptionId, current.PlanCode, current.Status, current.Version });
        });

        platform.MapPost("/tenants/{tenantId:guid}/subscription/{action}", async (
            Guid tenantId,
            string action,
            SubscriptionAction request,
            DbContextOptions<BerynxDbContext> options,
            ClaimsPrincipal principal,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["reason"] = ["A reason is required."] });
            await using var db = NewTenantDb(options, tenantId);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var tenant = await db.Set<Tenant>().SingleOrDefaultAsync(ct);
            if (tenant is null)
                return Results.NotFound();
            var subscription = tenant.CurrentSubscriptionId is { } id
                ? await db.Set<TenantSubscription>().SingleOrDefaultAsync(x => x.SubscriptionId == id, ct)
                : null;
            if (subscription is null)
                return Results.Conflict(new { detail = "Assign a plan before changing subscription status." });
            if (subscription.Version != request.Version)
                return Results.Conflict(new { detail = "The subscription changed. Reload tenant details before retrying." });

            var before = JsonSerializer.Serialize(subscription, Workflow.Json);
            var now = DateTimeOffset.UtcNow;
            switch (action.ToLowerInvariant())
            {
                case "activate":
                case "reactivate":
                    if (request.EndsAt is not { } activationEnd || activationEnd <= now)
                        return Results.ValidationProblem(new Dictionary<string, string[]> { ["endsAt"] = ["Set an end date in the future."] });
                    SubscriptionLifecycle.Transition(subscription, SubscriptionLifecycle.Active);
                    subscription.StartsAt = now;
                    subscription.EndsAt = activationEnd;
                    subscription.GraceEndsAt = await GraceEnd(db, subscription.PlanCode, activationEnd, ct);
                    break;
                case "renew":
                    if (request.EndsAt is not { } renewalEnd ||
                        renewalEnd <= (subscription.EndsAt ?? now))
                        return Results.ValidationProblem(new Dictionary<string, string[]> { ["endsAt"] = ["Renewal must extend the existing term."] });
                    SubscriptionLifecycle.Transition(subscription, SubscriptionLifecycle.Active);
                    subscription.EndsAt = renewalEnd;
                    subscription.GraceEndsAt = await GraceEnd(db, subscription.PlanCode, renewalEnd, ct);
                    break;
                case "suspend":
                    SubscriptionLifecycle.Transition(subscription, SubscriptionLifecycle.Suspended);
                    break;
                case "cancel":
                    SubscriptionLifecycle.Transition(subscription, SubscriptionLifecycle.Cancelled);
                    tenant.CancelledAt = now;
                    break;
                default:
                    return Results.NotFound();
            }

            subscription.Version++;
            tenant.Status = subscription.Status;
            tenant.Version++;
            AddAudit(db, tenantId, principal, http, subscription.SubscriptionId,
                "Subscription " + action.ToLowerInvariant(), "Subscription", request.Reason,
                before, JsonSerializer.Serialize(subscription, Workflow.Json));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { subscription.SubscriptionId, subscription.Status, subscription.EndsAt, subscription.GraceEndsAt, subscription.Version });
        });
    }

    private static PlatformPlan ToPlan(PlanWrite request) => new()
    {
        PlanCode = request.PlanCode.Trim(),
        Name = request.Name.Trim(),
        Description = request.Description?.Trim() ?? "",
        IsActive = true,
        MaxUsers = request.MaxUsers,
        MaxStorageBytes = request.MaxStorageBytes,
        MaxDocumentsPerMonth = request.MaxDocumentsPerMonth,
        MaxMessagesPerMonth = request.MaxMessagesPerMonth,
        MaxApiRequestsPerMonth = request.MaxApiRequestsPerMonth,
        TrialDays = request.TrialDays,
        GracePeriodDays = request.GracePeriodDays,
        PriceAmount = request.PriceAmount,
        Currency = request.Currency,
        Version = request.Version == 0 ? 1 : request.Version
    };

    private static async Task<string[]?> ValidatePlanModules(
        BerynxDbContext db, string[]? selected, CancellationToken ct)
    {
        if (selected is null || selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
            return null;
        var modules = await db.Set<PlatformModule>().AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(ct);
        var codes = selected.ToHashSet(StringComparer.Ordinal);
        return modules.Where(x => codes.Contains(x.ModuleCode)).Count() == codes.Count &&
               modules.Where(x => x.IsCore).All(x => codes.Contains(x.ModuleCode))
            ? selected
            : null;
    }

    private static async Task<string[]?> ValidateAddOns(
        BerynxDbContext db, string[]? selected, CancellationToken ct)
    {
        if (selected is null || selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
            return null;
        var modules = await db.Set<PlatformModule>().AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(ct);
        var codes = selected.ToHashSet(StringComparer.Ordinal);
        return modules.Where(x => codes.Contains(x.ModuleCode) && !x.IsCore).Count() == codes.Count
            ? selected
            : null;
    }

    private static async Task<bool> IncludesAllCoreModules(
        BerynxDbContext db, string[] included, CancellationToken ct)
    {
        var core = await db.Set<PlatformModule>().AsNoTracking()
            .Where(x => x.IsCore && x.IsActive)
            .Select(x => x.ModuleCode).ToListAsync(ct);
        return core.All(included.Contains);
    }

    internal static async Task ReplaceEntitlements(
        BerynxDbContext db, Guid tenantId, string[] enabled, string[] included,
        DateTimeOffset startsAt, CancellationToken ct)
    {
        var old = await db.Set<TenantModuleEntitlement>().ToListAsync(ct);
        db.RemoveRange(old);
        var catalog = await db.Set<PlatformModule>().AsNoTracking()
            .Select(module => module.ModuleCode).ToListAsync(ct);
        db.AddRange(catalog.Select(code => new TenantModuleEntitlement
        {
            TenantId = tenantId,
            ModuleCode = code,
            Source = included.Contains(code, StringComparer.Ordinal) ? "Plan" : "AddOn",
            IsEnabled = enabled.Contains(code, StringComparer.Ordinal),
            EffectiveFrom = startsAt
        }));
    }

    internal static void CreateInitialSubscription(
        BerynxDbContext db,
        Tenant tenant,
        PlatformPlan plan,
        string[] included,
        string[] addons,
        ClaimsPrincipal principal,
        HttpContext http,
        DateTimeOffset now)
    {
        var modules = included.Concat(addons).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();
        var endsAt = now.AddDays(plan.TrialDays);
        var subscription = new TenantSubscription
        {
            TenantId = tenant.TenantId,
            SubscriptionId = Guid.NewGuid(),
            PlanCode = plan.PlanCode,
            PlanVersion = plan.Version,
            Status = SubscriptionLifecycle.Trial,
            StartsAt = now,
            EndsAt = endsAt,
            GraceEndsAt = endsAt.AddDays(plan.GracePeriodDays),
            IsCurrent = true,
            Version = 1,
            TermsSnapshot = JsonSerializer.Serialize(new PlanTerms(
                plan.PlanCode, plan.Version, plan.MaxUsers, plan.MaxStorageBytes,
                plan.MaxDocumentsPerMonth, plan.MaxMessagesPerMonth, plan.MaxApiRequestsPerMonth,
                plan.TrialDays, plan.GracePeriodDays, plan.PriceAmount, plan.Currency,
                included, addons), Workflow.Json)
        };
        tenant.CurrentSubscriptionId = subscription.SubscriptionId;
        tenant.Plan = plan.PlanCode;
        tenant.Status = SubscriptionLifecycle.Trial;
        db.Add(subscription);
        var catalog = db.Set<PlatformModule>().AsNoTracking()
            .Select(module => module.ModuleCode).ToArray();
        db.AddRange(catalog.Select(code => new TenantModuleEntitlement
        {
            TenantId = tenant.TenantId,
            ModuleCode = code,
            Source = included.Contains(code, StringComparer.Ordinal) ? "Plan" : "AddOn",
            IsEnabled = modules.Contains(code, StringComparer.Ordinal),
            EffectiveFrom = now
        }));
        AddAudit(db, tenant.TenantId, principal, http, subscription.SubscriptionId,
            "Tenant provisioned", "Subscription", "Initial plan and add-ons assigned.",
            "{}", subscription.TermsSnapshot);
    }

    private static async Task<DateTimeOffset> GraceEnd(
        BerynxDbContext db, string planCode, DateTimeOffset endsAt, CancellationToken ct)
    {
        var days = await db.Set<PlatformPlan>().AsNoTracking()
            .Where(x => x.PlanCode == planCode)
            .Select(x => x.GracePeriodDays).SingleAsync(ct);
        return endsAt.AddDays(days);
    }

    private static BerynxDbContext NewTenantDb(DbContextOptions<BerynxDbContext> options, Guid tenantId) =>
        new(options, new OnboardingEndpoints.PlatformScope(tenantId));

    private static async Task<UsageSnapshot> Usage(
        BerynxDbContext db, Tenant tenant, Setup setup, CancellationToken ct)
    {
        var users = await db.Set<M02User>().LongCountAsync(
            x => x.Status == "Active" || x.Status == "Invited", ct);
        var storage = await new OnboardingStore(db, new OnboardingEndpoints.PlatformScope(tenant.TenantId))
            .Storage(setup);
        var documents = await db.Database.SqlQuery<long>($"""
            SELECT count(*) AS "Value" FROM onboarding_number
            WHERE tenant_id={tenant.TenantId}
              AND posted_at>=date_trunc('month',now() AT TIME ZONE 'Asia/Kolkata') AT TIME ZONE 'Asia/Kolkata'
            """).SingleAsync(ct);
        var messages = await db.Set<SetupEvent>().LongCountAsync(
            x => x.Action == "Invitation delivered", ct);
        var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var apiRequests = await db.Set<TenantApiUsage>().AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.MonthStart == monthStart)
            .Select(x => x.RequestCount).SingleOrDefaultAsync(ct);
        var limits = setup.Limits;
        return new UsageSnapshot(users, storage, documents, messages, apiRequests,
            new UsageQuotas(limits.Users, limits.Storage, limits.Documents, limits.Messages,
                limits.ApiRequests));
    }

    internal static void AddAudit(
        BerynxDbContext db,
        Guid tenantId,
        ClaimsPrincipal principal,
        HttpContext http,
        Guid entityId,
        string action,
        string entityType,
        string reason,
        string before,
        string after)
    {
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var actorId))
            throw new UnauthorizedAccessException();
        db.Add(new SaaSLicenseAuditEvent
        {
            TenantId = tenantId,
            EventId = Guid.NewGuid(),
            ActorIdentityId = actorId,
            ActorEmail = principal.FindFirstValue("email") ?? "",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Reason = reason,
            Before = before,
            After = after,
            Origin = OnboardingStore.Encode(new
            {
                ip = http.Connection.RemoteIpAddress?.ToString(),
                device = http.Request.Headers.UserAgent.ToString(),
                trace = http.TraceIdentifier
            }),
            At = DateTimeOffset.UtcNow
        });
    }
}
