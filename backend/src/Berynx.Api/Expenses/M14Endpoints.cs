using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Accounting;
using Berynx.Application.Expenses;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.Accounting;
using Berynx.Domain.Expenses;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Expenses;

public static class M14Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapM14(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m14")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M14",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/categories", Categories);
        api.MapPost("/categories", CreateCategory);
        api.MapGet("/expenses", List);
        api.MapPost("/expenses", Create);
        api.MapPost("/expenses/{expenseId:guid}/sync-approval", SyncApproval);
        api.MapPost("/expenses/{expenseId:guid}/post", Post);
    }

    private static async Task<IResult> Categories(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M14", "m14.expenses.view", ct);
        var categories = await db.Set<M14ExpenseCategory>().AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.CategoryId, x.Name, x.Type, x.ApprovalThreshold })
            .ToListAsync(ct);
        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateCategory(
        M14CategoryRequest request, TenantAuthorizationService authorization,
        ICurrentTenantContext tenantContext, BerynxDbContext db, CancellationToken ct)
    {
        await authorization.CurrentUser(ct);
        await authorization.Require("M14", "m14.categories.manage", ct);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 120 ||
            !M14Rules.IsCategoryType(request.Type) ||
            request.ApprovalThreshold is < 0 ||
            string.IsNullOrWhiteSpace(request.ExpenseLedgerCode))
            throw new System.ComponentModel.DataAnnotations.ValidationException("Category details are invalid.");
        var tenant = await db.Set<Tenant>().SingleAsync(x => x.TenantId == tenantContext.TenantId, ct);
        var setup = OnboardingStore.Decode(tenant);
        if (!(setup.Ledgers ?? []).Any(x =>
                x.Code.Equals(request.ExpenseLedgerCode.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict(new { detail = "Expense ledger code is not configured in tenant setup." });
        var name = request.Name.Trim();
        if (await db.Set<M14ExpenseCategory>().AnyAsync(x => x.Name == name, ct))
            return Results.Conflict(new { detail = "An expense category with this name already exists." });
        var category = new M14ExpenseCategory
        {
            TenantId = tenantContext.TenantId, CategoryId = Guid.NewGuid(),
            Name = name, Type = request.Type, ApprovalThreshold = request.ApprovalThreshold,
            ExpenseLedgerCode = request.ExpenseLedgerCode.Trim()
        };
        db.Add(category);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m14/categories/{category.CategoryId}",
            new { category.CategoryId, category.Name, category.Type, category.ApprovalThreshold });
    }

    private static async Task<IResult> List(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M14", "m14.expenses.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m14.expenses.view", ct);
        var query = from expense in db.Set<M14Expense>().AsNoTracking()
                    join category in db.Set<M14ExpenseCategory>().AsNoTracking()
                        on new { expense.TenantId, expense.CategoryId } equals new { category.TenantId, category.CategoryId }
                    select new
                    {
                        expense.ExpenseId, expense.BranchId, expense.ExpenseDate, expense.Status,
                        expense.Amount, expense.Description, expense.SiteId, expense.SalesOrderId,
                        expense.TripId, expense.EmployeePaid, expense.SupplierBillNumber,
                        expense.VoucherId, category.Name, category.Type
                    };
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            query = query.Where(x => branches.Contains(x.BranchId));
        }
        return Results.Ok(await query.OrderByDescending(x => x.ExpenseDate).Take(500).ToListAsync(ct));
    }

    private static async Task<IResult> Create(
        M14CreateExpenseRequest request, TenantAuthorizationService authorization,
        ICurrentTenantContext tenant, BerynxDbContext db, IM02ApprovalEngine approvals,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M14", "m14.expenses.create", ct);
        if (request.BranchId == Guid.Empty || request.CategoryId == Guid.Empty ||
            request.Amount <= 0 || decimal.Round(request.Amount, 2) != request.Amount ||
            string.IsNullOrWhiteSpace(request.Description) ||
            string.IsNullOrWhiteSpace(request.PaidFromLedgerCode) ||
            string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Required expense fields are invalid.");
        var scopes = await authorization.Scopes(actor.UserId, "m14.expenses.create", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == request.BranchId))
            return Results.Forbid();
        var existing = await db.Set<M14Expense>().SingleOrDefaultAsync(
            x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.Amount != request.Amount || existing.CategoryId != request.CategoryId ||
                existing.BranchId != request.BranchId || existing.Description != request.Description.Trim())
                return Results.Conflict(new { detail = "Idempotency key was already used for another expense." });
            return Results.Ok(new { existing.ExpenseId, existing.Status, idempotent = true });
        }
        var category = await db.Set<M14ExpenseCategory>().SingleOrDefaultAsync(
            x => x.CategoryId == request.CategoryId && x.IsActive, ct);
        if (category is null) return Results.NotFound(new { detail = "Expense category is not configured." });
        if (category.Type == "Other income")
            return Results.Conflict(new { detail = "Other income/capital-introduced capture is not part of the expense workflow yet." });
        if (request.GstAmount < 0 || request.GstAmount > request.Amount)
            throw new System.ComponentModel.DataAnnotations.ValidationException("GST amount must be between zero and the gross amount.");
        if (request.ReverseCharge)
            return Results.Conflict(new { detail = "Reverse-charge expense posting is unavailable until GST and shared tax posting are integrated." });

        var expense = new M14Expense
        {
            TenantId = tenant.TenantId, ExpenseId = Guid.NewGuid(), BranchId = request.BranchId,
            CategoryId = request.CategoryId, VendorPartyId = request.VendorPartyId,
            SiteId = request.SiteId, SalesOrderId = request.SalesOrderId, TripId = request.TripId,
            ExpenseDate = request.ExpenseDate, Amount = request.Amount,
            Description = request.Description.Trim(), PaidFromLedgerCode = request.PaidFromLedgerCode.Trim(),
            EmployeePaid = request.EmployeePaid, SupplierGstin = request.SupplierGstin?.Trim() ?? "",
            SupplierBillNumber = request.SupplierBillNumber?.Trim() ?? "",
            GstAmount = request.GstAmount, ItcEligible = request.ItcEligible,
            ReverseCharge = request.ReverseCharge, AttachmentReference = request.AttachmentReference?.Trim() ?? "",
            IdempotencyKey = request.IdempotencyKey.Trim(), CreatedByUserId = actor.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Add(expense);
        await db.SaveChangesAsync(ct);
        if (category.ApprovalThreshold is { } threshold && expense.Amount > threshold)
        {
            using var snapshotDocument = JsonDocument.Parse(JsonSerializer.Serialize(expense, Json));
            expense.ApprovalRequestId = await approvals.Submit(new ApprovalSubmission(
                "M14Expense", expense.ExpenseId, expense.BranchId, null, actor.UserId,
                expense.Amount, null, null, null, snapshotDocument.RootElement.Clone()), ct);
            if (expense.ApprovalRequestId is null)
                return Results.Conflict(new { detail = "Expense exceeds the category threshold but no matching M02 approval policy is configured." });
            expense.Status = "Pending approval";
        }
        else
        {
            expense.Status = "Approved";
        }
        db.Add(new M14ExpenseEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ExpenseId = expense.ExpenseId,
            ActorUserId = actor.UserId, EventType = "Created",
            Detail = JsonSerializer.Serialize(new { expense.Status, expense.Amount, expense.CategoryId }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/m14/expenses/{expense.ExpenseId}",
            new { expense.ExpenseId, expense.Status, expense.ApprovalRequestId });
    }

    private static async Task<IResult> SyncApproval(
        Guid expenseId, TenantAuthorizationService authorization, ICurrentTenantContext tenant,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M14", "m14.expenses.view", ct);
        var expense = await db.Set<M14Expense>().SingleOrDefaultAsync(x => x.ExpenseId == expenseId, ct);
        if (expense is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m14.expenses.view", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == expense.BranchId))
            return Results.Forbid();
        if (expense.Status != "Pending approval" || expense.ApprovalRequestId is null)
            return Results.Conflict(new { detail = "Expense has no pending approval to synchronize." });
        var approval = await db.Set<M02ApprovalRequest>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.RequestId == expense.ApprovalRequestId, ct);
        if (approval is null) return Results.Conflict(new { detail = "Linked approval request was not found." });
        if (approval.Status is not ("Approved" or "Rejected"))
            return Results.Ok(new { expenseStatus = expense.Status, approvalStatus = approval.Status });
        expense.Status = approval.Status;
        expense.ApprovalComment = approval.Status;
        db.Add(new M14ExpenseEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ExpenseId = expense.ExpenseId,
            ActorUserId = actor.UserId, EventType = "ApprovalSynchronized",
            Detail = JsonSerializer.Serialize(new { approval.Status, approval.RequestId }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { expense.Status });
    }

    private static async Task<IResult> Post(
        Guid expenseId, TenantAuthorizationService authorization, ICurrentTenantContext tenant,
        BerynxDbContext db, IM12PostingEngine posting, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M14", "m14.expenses.post", ct);
        var expense = await db.Set<M14Expense>().SingleOrDefaultAsync(x => x.ExpenseId == expenseId, ct);
        if (expense is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m14.expenses.post", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == expense.BranchId))
            return Results.Forbid();
        M14Rules.RequirePostable(expense);
        var category = await db.Set<M14ExpenseCategory>().SingleAsync(x => x.CategoryId == expense.CategoryId, ct);
        var amountExGst = expense.Amount - expense.GstAmount;
        var lines = new List<M12LineDraft>
        {
            new(category.ExpenseLedgerCode, null, expense.BranchId, expense.SiteId,
                expense.SalesOrderId, expense.TripId, expense.Description, amountExGst, 0m),
            new(expense.PaidFromLedgerCode, expense.VendorPartyId, expense.BranchId, expense.SiteId,
                expense.SalesOrderId, expense.TripId, expense.Description, 0m, expense.Amount)
        };
        if (expense.GstAmount > 0 && expense.ItcEligible)
            return Results.Conflict(new { detail = "GST ITC posting is unavailable until M13 and the shared tax-account mapping are complete." });
        if (expense.GstAmount > 0)
            lines[0] = lines[0] with { Debit = expense.Amount };
        var posted = await posting.PostAsync(new M12PostCommand(
            tenant.TenantId, actor.UserId, expense.BranchId, expense.ExpenseDate,
            "Expense", "M14", expense.ExpenseId, $"M14-expense:{expense.ExpenseId}",
            expense.Description, lines, IsManual: false), ct);
        expense.Status = "Posted";
        expense.VoucherId = posted.VoucherId;
        expense.PostedAt = DateTimeOffset.UtcNow;
        db.Add(new M14ExpenseEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ExpenseId = expense.ExpenseId,
            ActorUserId = actor.UserId, EventType = "Posted",
            Detail = JsonSerializer.Serialize(new { posted.VoucherId, posted.Number, posted.TotalDebit }),
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { expense.ExpenseId, expense.Status, expense.VoucherId, voucherNumber = posted.Number });
    }
}
