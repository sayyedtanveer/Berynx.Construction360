using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.Accounting;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Accounting;

public static class M12LedgerMappingEndpoints
{
    public sealed record CreateRequest(string MappingCode, Guid BranchId, DateOnly EffectiveFrom,
        DateOnly? EffectiveTo, string LedgerCode);
    public sealed record DeactivateRequest(long Version, string Reason);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapLedgerMappings(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m12")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M12",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/ledger-mappings", List);
        api.MapPost("/ledger-mappings", Create);
        api.MapPost("/ledger-mappings/{mappingId:guid}/deactivate", Deactivate);
    }

    private static async Task<IResult> List(
        TenantAuthorizationService authorization, OnboardingStore onboarding,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.accounts.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m12.accounts.view", ct);
        var setup = OnboardingStore.Decode(await onboarding.Current());
        var query = db.Set<M12LedgerMapping>().AsNoTracking();
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            query = query.Where(x => branches.Contains(x.BranchId));
        }
        return Results.Ok(new
        {
            items = await query.OrderBy(x => x.BranchId).ThenBy(x => x.MappingCode)
                .ThenByDescending(x => x.EffectiveFrom).ToListAsync(ct),
            codes = M12MappingCodes.All,
            branches = setup.Branches.Where(x => scopes.Any(scope => scope.ScopeType == "All" || scope.ScopeType == "Branch" && scope.ScopeId == x.Id))
                .Select(x => new { x.Id, x.Name, x.Gstin }),
            accounts = (setup.Ledgers ?? []).Where(x => x.IsActive).Select(x => new { x.Code, x.Name, x.Group, isActive = x.IsActive })
        });
    }

    private static async Task<IResult> Create(
        CreateRequest request, TenantAuthorizationService authorization,
        OnboardingStore onboarding, BerynxDbContext db, ICurrentTenantContext tenant,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.mappings.manage", ct);
        var code = request.MappingCode?.Trim() ?? "";
        var ledgerCode = request.LedgerCode?.Trim().ToLowerInvariant() ?? "";
        try { M12LedgerMappingRules.RequireValidRange(code, request.EffectiveFrom, request.EffectiveTo); }
        catch (InvalidOperationException ex) { throw new ValidationException(ex.Message); }
        if (request.BranchId == Guid.Empty || string.IsNullOrWhiteSpace(ledgerCode))
            throw new ValidationException("Choose a mapping, branch and active account.");
        var scopes = await authorization.Scopes(actor.UserId, "m12.mappings.manage", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == request.BranchId))
            return Results.Forbid();
        var setup = OnboardingStore.Decode(await onboarding.Current());
        var branch = setup.Branches.SingleOrDefault(x => x.Id == request.BranchId);
        if (branch is null || string.IsNullOrWhiteSpace(branch.Gstin))
            return Results.Conflict(new { detail = "Select a configured branch with a GSTIN." });
        var ledger = (setup.Ledgers ?? []).SingleOrDefault(x => x.IsActive &&
            x.Code.Equals(ledgerCode, StringComparison.OrdinalIgnoreCase));
        if (ledger is null)
            return Results.Conflict(new { detail = "The ledger must be an active account in this tenant's chart of accounts." });
        try { M12LedgerMappingRules.RequireLedgerGroup(code, ledger.Group); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { detail = ex.Message }); }
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var overlaps = await db.Set<M12LedgerMapping>()
            .Where(x => x.BranchId == request.BranchId && x.Gstin == branch.Gstin &&
                        x.MappingCode == code && x.IsActive &&
                        (x.EffectiveTo == null || x.EffectiveTo >= request.EffectiveFrom) &&
                        (request.EffectiveTo == null || x.EffectiveFrom <= request.EffectiveTo))
            .ToListAsync(ct);
        if (overlaps.Any(x => x.EffectiveFrom >= request.EffectiveFrom))
            return Results.Conflict(new { detail = "The effective range overlaps a later mapping. End or deactivate that mapping first." });
        var now = DateTimeOffset.UtcNow;
        foreach (var prior in overlaps)
        {
            prior.EffectiveTo = request.EffectiveFrom.AddDays(-1);
            prior.Version++;
            db.Add(new M12LedgerMappingEvent
            {
                TenantId = tenant.TenantId, EventId = Guid.NewGuid(), MappingId = prior.MappingId,
                ActorUserId = actor.UserId, EventType = "Superseded by effective-dated mapping",
                DetailJson = JsonSerializer.Serialize(new { prior.EffectiveTo, newCode = code }, Json),
                OccurredAt = now
            });
        }
        var mapping = new M12LedgerMapping
        {
            TenantId = tenant.TenantId, MappingId = Guid.NewGuid(), MappingCode = code,
            BranchId = branch.Id, Gstin = branch.Gstin, LedgerCode = ledgerCode,
            EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo,
            IsActive = true, CreatedByUserId = actor.UserId, CreatedAt = now
        };
        db.Add(mapping);
        db.Add(new M12LedgerMappingEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), MappingId = mapping.MappingId,
            ActorUserId = actor.UserId, EventType = "Ledger mapping created",
            DetailJson = JsonSerializer.Serialize(new
            {
                mapping.MappingCode, mapping.BranchId, mapping.Gstin, mapping.LedgerCode,
                mapping.EffectiveFrom, mapping.EffectiveTo
            }, Json), OccurredAt = now
        });
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "A mapping changed concurrently; reload and retry." });
        }
        return Results.Created($"/api/v1/m12/ledger-mappings/{mapping.MappingId}", mapping);
    }

    private static async Task<IResult> Deactivate(
        Guid mappingId, DeactivateRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.mappings.manage", ct);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A reason is required to deactivate a mapping.");
        var mapping = await db.Set<M12LedgerMapping>().SingleOrDefaultAsync(x => x.MappingId == mappingId, ct);
        if (mapping is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m12.mappings.manage", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == mapping.BranchId))
            return Results.Forbid();
        if (mapping.Version != request.Version || !mapping.IsActive)
            return Results.Conflict(new { detail = "The mapping changed or is already inactive." });
        mapping.IsActive = false;
        mapping.Version++;
        db.Add(new M12LedgerMappingEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), MappingId = mapping.MappingId,
            ActorUserId = actor.UserId, EventType = "Ledger mapping deactivated",
            DetailJson = JsonSerializer.Serialize(new { reason = request.Reason.Trim(), mapping.LedgerCode }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { mapping.MappingId, mapping.IsActive, mapping.Version });
    }
}