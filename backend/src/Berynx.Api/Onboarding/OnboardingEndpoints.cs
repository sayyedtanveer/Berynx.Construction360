using System.IO.Compression;
using System.Security.Claims;
using System.Text.Json;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Onboarding;
public static class OnboardingEndpoints
{
    internal sealed record PlatformScope(Guid TenantId) : ICurrentTenantContext;
    internal static string Origin(HttpContext http) => OnboardingStore.Encode(new { roles = http.User.FindAll("role").Select(c => c.Value), ip = http.Connection.RemoteIpAddress?.ToString(), device = http.Request.Headers.UserAgent.ToString(), trace = http.TraceIdentifier });
    private static string Actor(ClaimsPrincipal p) => p.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();
    public sealed record StatusChange(string Status, string Reason);
    public sealed record Preview(Series Series, int StartMonth);
    public sealed record VersionRequest(long Version);
    public static void MapOnboarding(this WebApplication app)
    {
        app.MapGet("/api/v1/auth/session", async (ClaimsPrincipal p, IM02Access access, CancellationToken ct) =>
        {
            if (p.IsInRole("PlatformAdmin"))
                return Results.Ok(new { authenticated = true, platform = true, admin = false, accounts = false });
            var user = await access.CurrentUser(ct);
            return Results.Ok(new
            {
                authenticated = true,
                platform = false,
                admin = await access.HasPermission(user.UserId, "users.edit", ct),
                accounts = await access.HasPermission(user.UserId, "opening.edit", ct)
            });
        });
        var platform = app.MapGroup("/api/v1/platform/tenants").RequireAuthorization(p => p.RequireRole("PlatformAdmin").RequireClaim("sub"));
        platform.MapGet("", async (BerynxDbContext db, int? page) => await db.Set<Tenant>().IgnoreQueryFilters().OrderBy(t => t.Name).ThenBy(t => t.TenantId).Skip(Math.Clamp((page ?? 1) - 1, 0, 100000) * 50).Take(50).Select(t => new { t.TenantId, t.Name, t.Plan, t.Status, t.Version, exportUntil = t.CancelledAt.HasValue ? (DateTimeOffset? )t.CancelledAt.Value.AddDays(30) : null }).ToListAsync());
        platform.MapPost("", async (Provision r, DbContextOptions<BerynxDbContext> options, IConfiguration cfg,
            UserManager<M02IdentityUser> identityUsers, ClaimsPrincipal p, HttpContext http) =>
        {
            var scope = new PlatformScope(r.RequestId);
            await using var db = new BerynxDbContext(options, scope);
            var store = new OnboardingStore(db, scope)
            {
                Origin = Origin(http)
            };
            var existing = await db.Set<SetupEvent>().SingleOrDefaultAsync(x => x.Action == "Provision");
            if (existing != null)
            {
                if (existing.Actor != Actor(p) ||
                    !JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(existing.After),
                        JsonSerializer.SerializeToElement(r, Workflow.Json)))
                    return Results.Conflict();
            }
            else
            {
                var defaults = new Setup
                {
                    Taxes = cfg.GetSection("Onboarding:TaxDefaults").Get<List<TaxDefault>>() ?? []
                };
                defaults.Ledgers = cfg.GetSection("Onboarding:ChartOfAccounts").Get<LedgerDefinition[]>() ?? defaults.Ledgers;
                defaults.NumberingDefaults = SetupRules.Documents.ToDictionary(d => d,
                    d => cfg[$"Onboarding:Numbering:{d}"] ?? "{FY}/{SEQ:4}");
                await store.Create(r, Actor(p), defaults);
            }
            await EnsureFirstAdminIdentity(r, db, identityUsers, http.RequestAborted);
            return Results.Created($"/api/v1/platform/tenants/{r.RequestId}", new { tenantId = r.RequestId });
        });
        platform.MapPost("/{id:guid}/status", async (Guid id, Command c, DbContextOptions<BerynxDbContext> options, ClaimsPrincipal p, HttpContext http) =>
        {
            var scope = new PlatformScope(id);
            await using var db = new BerynxDbContext(options, scope);
            var change = c.Data.Read<StatusChange>();
            Workflow.Require(!string.IsNullOrWhiteSpace(change.Reason), "Provide a lifecycle change reason.");
            var store = new OnboardingStore(db, scope)
            {
                Origin = Origin(http)
            };
            await store.Mutate(c.Version, "Lifecycle: " + change.Reason, Actor(p), (t, s) => SetupRules.Transition(t, change.Status, DateTimeOffset.UtcNow), true);
            return Results.NoContent();
        });
        var tenant = app.MapGroup("/api/v1/onboarding").RequireAuthorization(p => p.RequireClaim("sub").RequireClaim("tenant_id"));
        tenant.MapGet("", async (OnboardingStore store) => await store.View());
        tenant.MapPost("/steps/{step:int}", async (int step, Command c, OnboardingStore store, ClaimsPrincipal p) =>
        {
            await store.Mutate(c.Version, $"Setup step {step}", Actor(p), (t, s) => Workflow.Save(s, step, c.Data));
            return Results.NoContent();
        });
        tenant.MapPost("/number-preview", (Preview r) => new { number = SetupRules.Number(r.Series, r.StartMonth, DateOnly.FromDateTime(DateTime.UtcNow), 1) });
        tenant.MapPost("/go-live", async (VersionRequest r, OnboardingStore store, ClaimsPrincipal p) =>
        {
            await store.GoLive(r.Version, Actor(p));
            return Results.NoContent();
        });
        tenant.MapGet("/usage", async (OnboardingStore store, BerynxDbContext db) =>
        {
            var t = await store.Current();
            var s = OnboardingStore.Decode(t);
            var volume = await db.Database.SqlQuery<long>($"""
                SELECT count(*) AS "Value" FROM onboarding_number WHERE tenant_id={t.TenantId}
                AND posted_at>=date_trunc('month',now() AT TIME ZONE 'Asia/Kolkata') AT TIME ZONE 'Asia/Kolkata'
                """).SingleAsync();
            var messages = await db.Set<SetupEvent>().LongCountAsync(x => x.Action == "Invitation delivered");
            return new
            {
                t.Plan,
                t.Status,
                metrics = new[]
                {
                    Metric("Users", s.Invitations.Count, s.Limits.Users),
                    Metric("Storage (bytes)", await store.Storage(s), s.Limits.Storage),
                    Metric("Documents this month", volume, s.Limits.Documents),
                    Metric("Messages sent", messages, s.Limits.Messages)
                }
            };
        });
        tenant.MapGet("/export", async (OnboardingStore store, BerynxDbContext db, ClaimsPrincipal p) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            var t = await store.Current();
            await db.Entry(t).ReloadAsync();
            if (t.CancelledAt?.AddDays(30) <= DateTimeOffset.UtcNow)
                return Results.StatusCode(410);
            var numbers = await db.Database.SqlQuery<string>($"""
                SELECT row_to_json(n)::text AS "Value" FROM onboarding_number n WHERE tenant_id={t.TenantId}
                """).ToListAsync();
            var deliveries = await db.Database.SqlQuery<string>($"""
                SELECT row_to_json(i)::text AS "Value" FROM onboarding_invitation i WHERE tenant_id={t.TenantId}
                """).ToListAsync();
            var data = new
            {
                tenant = t,
                deliveries,
                openings = await db.Set<OpeningItem>().ToListAsync(),
                audit = await db.Set<SetupEvent>().ToListAsync(),
                numbers
            };
            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            using (var output = zip.CreateEntry("tenant.json").Open())
                JsonSerializer.Serialize(output, data, Workflow.Json);
            store.Audit(t, "Export", Actor(p), "{}", "{}");
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Results.File(stream.ToArray(), "application/zip", "tenant-export.zip");
        });
        var balances = app.MapGroup("/api/v1/openings").RequireAuthorization(p => p.RequireClaim("sub").RequireClaim("tenant_id"));
        balances.MapGet("", async (OnboardingStore store, BerynxDbContext db) =>
        {
            var t = await store.Current();
            return new
            {
                t.Version,
                t.Status,
                t.LiveAt,
                rows = await db.Set<OpeningItem>().ToListAsync()
            };
        });
        balances.MapPost("", async (Command c, OnboardingStore store, ClaimsPrincipal p) =>
        {
            await store.SaveOpenings(c.Version, c.Data.Read<OpeningItem[]>(), Actor(p));
            return Results.NoContent();
        });
        balances.MapPost("/preview", (OpeningItem[] rows) => OpeningWorkbook.Preview(rows));
        balances.MapPost("/excel", async (HttpRequest request) => await OpeningWorkbook.Read(request.Body));
        tenant.AddEndpointFilter(TenantAccess);
        balances.AddEndpointFilter(TenantAccess);
    }

    private static async ValueTask<object?> TenantAccess(EndpointFilterInvocationContext call, EndpointFilterDelegate next)
    {
        var http = call.HttpContext;
        var user = http.User;
        if (user.HasClaim(c => c.Type == "branch_id"))
            return Results.Forbid();
        var store = http.RequestServices.GetRequiredService<OnboardingStore>();
        store.Origin = Origin(http);
        var t = await store.Current();
        if (t.Status == "Cancelled" && http.Request.Path.Value != "/api/v1/onboarding/export")
            return Results.Forbid();
        var access = http.RequestServices.GetRequiredService<IM02Access>();
        var current = await access.CurrentUser(http.RequestAborted);
        if (!await access.CanAccess(current.UserId, AccessPermission(http.Request.Path.Value, http.Request.Method),
                null, null, null, http.RequestAborted))
            return Results.Forbid();
        if (HttpMethods.IsGet(http.Request.Method))
            http.Response.Headers.ETag = $"\"{t.Version}\"";
        var expected = http.Request.Headers.IfMatch.ToString();
        if (expected.Length > 0 && expected != $"\"{t.Version}\"")
            return Results.StatusCode(412);
        return await next(call);
    }

    private static async Task EnsureFirstAdminIdentity(Provision request, BerynxDbContext db,
        UserManager<M02IdentityUser> identityUsers, CancellationToken ct)
    {
        var email = request.AdminEmail.Trim().ToLowerInvariant();
        var identity = await identityUsers.FindByEmailAsync(email);
        if (identity is null)
        {
            identity = new M02IdentityUser
            {
                UserName = email,
                Email = email,
                LockoutEnabled = true
            };
            var created = await identityUsers.CreateAsync(identity);
            if (!created.Succeeded)
                identity = await identityUsers.FindByEmailAsync(email);
            if (identity is null)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Could not create the first-admin identity account.");
        }

        var deliveryId = await db.Database.SqlQuery<Guid>($"""
            SELECT delivery_id AS "Value" FROM onboarding_invitation
            WHERE tenant_id={request.RequestId} AND email={email}
            """).SingleAsync(ct);
        var membership = await db.Set<M02User>().SingleOrDefaultAsync(x => x.Email == email, ct);
        if (membership is not null && membership.IdentityUserId != identity.Id)
            throw new InvalidOperationException("First-admin membership conflicts with an existing identity.");
        var role = await db.Set<M02Role>().SingleOrDefaultAsync(x => x.RoleKey == "TenantAdmin", ct)
            ?? throw new InvalidOperationException(
                "The M02 TenantAdmin role is unavailable; apply the reviewed M02 schema and seed first.");
        var now = DateTimeOffset.UtcNow;
        if (membership is null)
        {
            membership = new M02User
            {
                TenantId = request.RequestId,
                UserId = Guid.NewGuid(),
                IdentityUserId = identity.Id,
                ProviderSubject = identity.Id.ToString("D"),
                Email = email,
                DisplayName = email,
                Status = "Invited",
                InvitationDeliveryId = deliveryId,
                InviteExpiresAt = now.AddHours(72),
                InvitationDeliveredAt = null,
                InvitationAvailableAt = now
            };
            db.Add(membership);
        }
        else if (membership.Status == "Invited" && membership.InvitationDeliveryId is null)
        {
            membership.IdentityUserId = identity.Id;
            membership.ProviderSubject = identity.Id.ToString("D");
            membership.InvitationDeliveryId = deliveryId;
            membership.InviteExpiresAt = now.AddHours(72);
            membership.InvitationDeliveredAt = null;
            membership.Version++;
        }
        var hasAdmin = await db.Set<M02UserRole>().AnyAsync(x =>
            x.UserId == membership.UserId && x.RoleId == role.RoleId, ct);
        if (!hasAdmin)
            db.Add(new M02UserRole
            {
                TenantId = request.RequestId,
                AssignmentId = Guid.NewGuid(),
                UserId = membership.UserId,
                RoleId = role.RoleId,
                ScopeType = "All",
                AssignedAt = now,
                AssignedBy = "m01-first-admin"
            });
        if (db.Entry(membership).State == EntityState.Added)
            db.Add(new M02AuditEvent
            {
                TenantId = request.RequestId,
                EventId = Guid.NewGuid(),
                ActorSubject = "m01-provisioning",
                Action = "First-admin invitation queued",
                EntityType = "User",
                EntityId = membership.UserId,
                Before = "{}",
                After = JsonSerializer.Serialize(new { email, deliveryId }),
                At = now
            });
        await db.SaveChangesAsync(ct);
    }

    private static string AccessPermission(string? path, string method)
    {
        if (path == "/api/v1/onboarding/usage")
            return "usage.view";
        if (path == "/api/v1/onboarding/export")
            return "onboarding.export";
        if (path?.StartsWith("/api/v1/openings", StringComparison.Ordinal) == true)
        {
            if (path.EndsWith("/excel", StringComparison.Ordinal))
                return "opening.edit";
            return HttpMethods.IsGet(method) || path.EndsWith("/preview", StringComparison.Ordinal)
                ? "opening.view"
                : "opening.edit";
        }
        return HttpMethods.IsGet(method) || path?.EndsWith("/number-preview", StringComparison.Ordinal) == true
            ? "onboarding.view"
            : "onboarding.edit";
    }

    private static object Metric(string name, long used, long limit) => new
    {
        name,
        used,
        limit,
        warning = SetupRules.Warning(used, limit),
        blocked = used >= limit
    };
}
