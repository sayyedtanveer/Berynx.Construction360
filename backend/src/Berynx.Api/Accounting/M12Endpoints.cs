using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Berynx.Api.Onboarding;
using Berynx.Application.Accounting;
using Berynx.Application.Tenancy;
using Berynx.Domain.Accounting;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Accounting;

public static class M12Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapM12(this WebApplication app)
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
        api.MapGet("/accounts", Accounts);
        api.MapPost("/accounts", CreateAccount);
        api.MapPut("/accounts/{code}", RenameAccount);
        api.MapPost("/accounts/{code}/deactivate", DeactivateAccount);
        app.MapLedgerMappings();
        api.MapPost("/vouchers/manual", PostManualVoucher);
        api.MapGet("/vouchers", Vouchers);
        api.MapGet("/trial-balance", TrialBalance);
        api.MapGet("/periods", Periods);
        api.MapPost("/periods/lock", LockPeriod);
    }

    private sealed record AccountWrite(long Version, string Code, string Name, string Group);
    private sealed record AccountDeactivate(long Version, string Reason);
    private sealed record AccountRename(long Version, string Name);
    private static readonly HashSet<string> SystemLedgerCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "cash", "bank", "receivables", "payables", "opening-equity", "sales-revenue",
        "output-cgst", "output-sgst", "output-igst", "round-off", "tds-receivable",
        "tcs-payable", "bank-charges", "customer-advances"
    };
    private static readonly string[] AccountGroups = ["Assets", "Liabilities", "Equity", "Income", "Expenses"];

    private static async Task<IResult> Accounts(
        OnboardingStore onboarding, TenantAuthorizationService authorization, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.accounts.view", ct);
        var tenant = await onboarding.Current();
        var setup = OnboardingStore.Decode(tenant);
        return Results.Ok(new
        {
            version = tenant.Version,
            items = (setup.Ledgers ?? []).Select(x => new { x.Code, x.Name, x.Group, isSystem = SystemLedgerCodes.Contains(x.Code), x.IsActive })
        });
    }

    private static async Task<IResult> CreateAccount(
        AccountWrite request, OnboardingStore onboarding, TenantAuthorizationService authorization,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.accounts.manage", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m12.accounts.manage", ct);
        if (!scopes.Any(x => x.ScopeType == "All")) return Results.Forbid();
        var code = request.Code?.Trim().ToLowerInvariant() ?? "";
        var name = request.Name?.Trim() ?? "";
        var group = request.Group?.Trim() ?? "";
        if (!Regex.IsMatch(code, "^[A-Za-z0-9][A-Za-z0-9_-]{1,63}$") || name.Length is < 2 or > 120 ||
            !AccountGroups.Contains(group, StringComparer.Ordinal))
            throw new ValidationException("Account code, name and group are invalid.");
        var current = OnboardingStore.Decode(await onboarding.Current());
        if ((current.Ledgers ?? []).Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict(new { detail = "Ledger code already exists in this tenant." });
        if ((current.Ledgers ?? []).Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict(new { detail = "Account name already exists in this tenant." });
        try
        {
            await onboarding.Mutate(request.Version, "M12 ledger account created", actor.UserId.ToString(),
                (_, setup) => setup.Ledgers = (setup.Ledgers ?? []).Append(new LedgerDefinition(code, name, group)).ToArray());
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "Tenant chart changed concurrently; reload before retrying." });
        }
        return Results.Created($"/api/v1/m12/accounts/{code}", new { code, name, group, isActive = true });
    }

    private static async Task<IResult> RenameAccount(
        string code, AccountRename request, OnboardingStore onboarding,
        TenantAuthorizationService authorization, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.accounts.manage", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m12.accounts.manage", ct);
        if (!scopes.Any(x => x.ScopeType == "All")) return Results.Forbid();
        var ledgerCode = Uri.UnescapeDataString(code);
        var name = request.Name?.Trim() ?? "";
        if (name.Length is < 2 or > 120) throw new ValidationException("Account name must contain 2-120 characters.");
        var current = OnboardingStore.Decode(await onboarding.Current());
        var existing = (current.Ledgers ?? []).SingleOrDefault(x => x.Code.Equals(ledgerCode, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return Results.NotFound();
        if (SystemLedgerCodes.Contains(existing.Code))
            return Results.Conflict(new { detail = "System chart accounts cannot be renamed." });
        if ((current.Ledgers ?? []).Any(x => !x.Code.Equals(ledgerCode, StringComparison.OrdinalIgnoreCase) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict(new { detail = "Account name already exists in this tenant." });
        try
        {
            await onboarding.Mutate(request.Version, "M12 ledger account renamed", actor.UserId.ToString(),
                (_, setup) => setup.Ledgers = (setup.Ledgers ?? []).Select(x => x.Code.Equals(ledgerCode, StringComparison.OrdinalIgnoreCase)
                    ? x with { Name = name } : x).ToArray());
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "Tenant chart changed concurrently; reload before retrying." });
        }
        return Results.Ok(new { code = ledgerCode, name });
    }
    private static async Task<IResult> DeactivateAccount(
        string code, AccountDeactivate request, OnboardingStore onboarding,
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.accounts.manage", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m12.accounts.manage", ct);
        if (!scopes.Any(x => x.ScopeType == "All")) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("A reason is required to deactivate an account.");
        var ledgerCode = Uri.UnescapeDataString(code);
        if (SystemLedgerCodes.Contains(ledgerCode))
            return Results.Conflict(new { detail = "System chart accounts cannot be deactivated." });
        if (await db.Set<M12VoucherLine>().AnyAsync(x => x.LedgerCode.ToLower() == ledgerCode.ToLower(), ct))
            return Results.Conflict(new { detail = "A ledger used by a posted voucher cannot be deactivated; use an authorized reversal/correction instead." });
        if (await db.Set<M12LedgerMapping>().AnyAsync(x => x.LedgerCode.ToLower() == ledgerCode.ToLower() && x.IsActive, ct))
            return Results.Conflict(new { detail = "Deactivate all active accounting mappings before deactivating this ledger." });
        var tenant = await onboarding.Current();
        try
        {
            await onboarding.Mutate(request.Version, "M12 ledger account deactivated", actor.UserId.ToString(),
                (_, setup) =>
                {
                    var ledger = (setup.Ledgers ?? []).SingleOrDefault(x => x.Code.Equals(ledgerCode, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ValidationException("Ledger account not found.");
                    if (!ledger.IsActive) throw new ValidationException("Ledger account is already inactive.");
                    setup.Ledgers = (setup.Ledgers ?? []).Select(x => x.Code.Equals(ledgerCode, StringComparison.OrdinalIgnoreCase)
                        ? x with { IsActive = false } : x).ToArray();
                });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "Tenant chart changed concurrently; reload before retrying." });
        }
        return Results.Ok(new { code = ledgerCode, isActive = false, version = tenant.Version + 1 });
    }
    private static async Task<IResult> PostManualVoucher(
        M12PostVoucherRequest request, TenantAuthorizationService authorization,
        IM12PostingEngine postingEngine, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.vouchers.post", ct);
        if (request.VoucherType is not ("Journal" or "Contra"))
            throw new ValidationException("Manual vouchers are limited to Journal and Contra.");
        if (string.IsNullOrWhiteSpace(request.Narration) || request.Narration.Length > 1000)
            throw new ValidationException("Voucher narration is required and cannot exceed 1,000 characters.");
        var scopes = await authorization.Scopes(actor.UserId, "m12.vouchers.post", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == request.BranchId))
            return Results.Forbid();
        try
        {
            var posted = await postingEngine.PostAsync(new M12PostCommand(
                tenant.TenantId, actor.UserId, request.BranchId, request.VoucherDate,
                request.VoucherType, "Manual", null, request.IdempotencyKey,
                request.Narration.Trim(), request.Lines, IsManual: true), ct);
            return Results.Created($"/api/v1/m12/vouchers/{posted.VoucherId}", posted);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { detail = ex.Message });
        }
    }

    private static async Task<IResult> Vouchers(
        Guid? branchId, DateOnly? from, DateOnly? to, int? page, int? pageSize,
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.vouchers.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m12.vouchers.view", ct);
        var query = db.Set<M12Voucher>().AsNoTracking().AsQueryable();
        if (branchId is { } branch) query = query.Where(x => x.BranchId == branch);
        if (from is { } start) query = query.Where(x => x.VoucherDate >= start);
        if (to is { } end) query = query.Where(x => x.VoucherDate <= end);
        query = ApplyBranchScope(query, scopes);
        var safePage = Math.Max(1, page ?? 1);
        var safeSize = Math.Clamp(pageSize ?? 50, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.VoucherDate).ThenByDescending(x => x.Number)
            .Skip((safePage - 1) * safeSize).Take(safeSize)
            .Select(x => new
            {
                x.VoucherId, x.Number, x.VoucherType, x.Status, x.VoucherDate,
                x.SourceModule, x.SourceEntityId, x.Narration, x.TotalDebit, x.TotalCredit
            }).ToListAsync(ct);
        return Results.Ok(new { items, total, page = safePage, pageSize = safeSize });
    }

    private static async Task<IResult> TrialBalance(
        DateOnly? asOf, Guid? branchId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.reports.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m12.reports.view", ct);
        var lines = from line in db.Set<M12VoucherLine>().AsNoTracking()
                    join voucher in db.Set<M12Voucher>().AsNoTracking()
                        on new { line.TenantId, line.VoucherId } equals new { voucher.TenantId, voucher.VoucherId }
                    where voucher.Status == "Posted" && (asOf == null || voucher.VoucherDate <= asOf)
                    select new { line, voucher };
        if (branchId is { } branch)
            lines = lines.Where(x => x.line.BranchId == branch || x.voucher.BranchId == branch);
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            lines = lines.Where(x => branches.Contains(x.voucher.BranchId) ||
                                     x.line.BranchId != null && branches.Contains(x.line.BranchId.Value));
        }
        var balances = await lines.GroupBy(x => x.line.LedgerCode)
            .Select(g => new
            {
                ledgerCode = g.Key,
                debit = g.Sum(x => x.line.Debit),
                credit = g.Sum(x => x.line.Credit)
            }).OrderBy(x => x.ledgerCode).ToListAsync(ct);
        return Results.Ok(new
        {
            asOf,
            balances,
            totalDebit = balances.Sum(x => x.debit),
            totalCredit = balances.Sum(x => x.credit)
        });
    }

    private static async Task<IResult> Periods(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.periods.manage", ct);
        return Results.Ok(await db.Set<M12FinancialPeriod>().AsNoTracking()
            .OrderBy(x => x.PeriodStart)
            .Select(x => new { x.PeriodStart, x.PeriodEnd, x.IsLocked, x.LockReason, x.LockedAt })
            .ToListAsync(ct));
    }

    private static async Task<IResult> LockPeriod(
        M12FinancialPeriod request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M12", "m12.periods.manage", ct);
        if (!request.IsLocked || request.PeriodStart.Day != 1 ||
            request.PeriodEnd != request.PeriodStart.AddMonths(1).AddDays(-1) ||
            string.IsNullOrWhiteSpace(request.LockReason))
            throw new ValidationException("Lock a complete calendar month with a reason.");
        var period = await db.Set<M12FinancialPeriod>()
            .SingleOrDefaultAsync(x => x.PeriodStart == request.PeriodStart, ct);
        if (period?.IsLocked == true)
            return Results.Conflict(new { detail = "This accounting period is already locked." });
        if (period is null)
        {
            period = new M12FinancialPeriod
            {
                TenantId = tenant.TenantId,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd
            };
            db.Add(period);
        }
        period.IsLocked = true;
        period.LockReason = request.LockReason.Trim();
        period.LockedByUserId = actor.UserId;
        period.LockedAt = DateTimeOffset.UtcNow;
        db.Add(new M12PeriodEvent
        {
            TenantId = tenant.TenantId,
            EventId = Guid.NewGuid(),
            PeriodStart = request.PeriodStart,
            ActorUserId = actor.UserId,
            EventType = "Period locked",
            Reason = request.LockReason.Trim(),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { period.PeriodStart, period.PeriodEnd, period.IsLocked, period.LockReason });
    }

    private static IQueryable<M12Voucher> ApplyBranchScope(
        IQueryable<M12Voucher> query, IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        return query.Where(x => branches.Contains(x.BranchId));
    }
}
