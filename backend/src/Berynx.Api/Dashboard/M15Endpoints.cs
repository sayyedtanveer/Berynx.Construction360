using Berynx.Api.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.Accounting;
using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;
using Berynx.Domain.Expenses;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Invoicing;
using Berynx.Domain.Receivables;
using Berynx.Domain.SalesOrders;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Dashboard;

public static class M15Endpoints
{
    public static void MapM15(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m15")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M15", false,
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/home", Home);
    }

    private static async Task<IResult> Home(
        DateOnly? from, DateOnly? to, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M15", "m15.dashboard.view", ct);
        var start = from ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (start > end || end.DayNumber - start.DayNumber > 370)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Dashboard date range is invalid or exceeds 371 days.");
        var scopes = await authorization.Scopes(actor.UserId, "m15.dashboard.view", ct);
        var allBranches = scopes.Any(x => x.ScopeType == "All");
        var ownScope = scopes.Any(x => x.ScopeType == "Own");
        var branchIds = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        var costScopes = await authorization.Scopes(actor.UserId, "global.view_cost", ct);
        var costAllBranches = costScopes.Any(x => x.ScopeType == "All");
        var costOwn = costScopes.Any(x => x.ScopeType == "Own");
        var costBranchIds = costScopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        var costAccess = costAllBranches ||
                         costOwn && ownScope ||
                         (allBranches || branchIds.Intersect(costBranchIds).Any()) &&
                         (costBranchIds.Length > 0);

        var orders = db.Set<M06SalesOrder>().AsNoTracking()
            .Where(x => x.OrderDate >= start && x.OrderDate <= end);
        if (!allBranches) orders = orders.Where(x =>
            x.BranchId.HasValue && branchIds.Contains(x.BranchId.Value) ||
            ownScope && x.SalesOwnerUserId == actor.UserId);
        var orderRows = await orders.Select(x => new { x.Status, x.Total }).ToListAsync(ct);

        var invoices = db.Set<M10Invoice>().AsNoTracking()
            .Where(x => x.InvoiceDate >= start && x.InvoiceDate <= end);
        if (!allBranches) invoices = invoices.Where(x =>
            branchIds.Contains(x.BranchId) || ownScope && x.CreatedByUserId == actor.UserId);
        var invoiceRows = await invoices.Select(x => new
        {
            x.InvoiceId, x.Status, x.BranchId, x.TotalAmount
        }).ToListAsync(ct);

        var invoiceIds = invoiceRows.Where(x => x.Status == "Posted").Select(x => x.InvoiceId).ToArray();
        var postedAllocations = await (
            from allocation in db.Set<M11ReceiptAllocation>().AsNoTracking()
            join receipt in db.Set<M11Receipt>().AsNoTracking()
                on new { allocation.TenantId, allocation.ReceiptId } equals new { receipt.TenantId, receipt.ReceiptId }
            where invoiceIds.Contains(allocation.InvoiceId) && receipt.Status == "Posted"
            select allocation.Amount).SumAsync(ct);

        var trips = db.Set<M08Trip>().AsNoTracking()
            .Where(x => x.CreatedAt >= new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) &&
                        x.CreatedAt < new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (!allBranches) trips = trips.Where(x =>
            branchIds.Contains(x.BranchId) || ownScope && x.CreatedByUserId == actor.UserId);
        var tripRows = await trips.Select(x => new { x.Status, x.PlannedQuantity, x.DeliveredQuantity }).ToListAsync(ct);

        var challans = db.Set<M09DeliveryChallan>().AsNoTracking()
            .Where(x => x.ChallanDate >= start && x.ChallanDate <= end);
        if (!allBranches) challans = challans.Where(x =>
            branchIds.Contains(x.BranchId) || ownScope && x.CreatedByUserId == actor.UserId);
        var unbilledChallans = await challans.CountAsync(x => x.InvoiceId == null, ct);

        var expenses = db.Set<M14Expense>().AsNoTracking()
            .Where(x => x.ExpenseDate >= start && x.ExpenseDate <= end && x.Status == "Posted");
        if (!allBranches) expenses = expenses.Where(x =>
            branchIds.Contains(x.BranchId) || ownScope && x.CreatedByUserId == actor.UserId);
        if (!costAllBranches) expenses = expenses.Where(x =>
            costBranchIds.Contains(x.BranchId) || costOwn && x.CreatedByUserId == actor.UserId);
        var expenseTotal = costAccess ? await expenses.SumAsync(x => (decimal?)x.Amount, ct) ?? 0m : (decimal?)null;

        var pendingApprovals = db.Set<M02ApprovalRequest>().AsNoTracking()
            .Where(x => x.Status == "Pending" && x.CreatedAt >= new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) &&
                        x.CreatedAt < new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (!allBranches) pendingApprovals = pendingApprovals.Where(x =>
            x.BranchId.HasValue && branchIds.Contains(x.BranchId.Value) ||
            ownScope && x.RequesterUserId == actor.UserId);
        var approvalCount = await pendingApprovals.CountAsync(ct);

        var postedInvoices = invoiceRows.Where(x => x.Status == "Posted").ToArray();
        var invoiceTotal = postedInvoices.Sum(x => x.TotalAmount);
        var tripStatuses = tripRows.GroupBy(x => x.Status)
            .ToDictionary(x => x.Key, x => x.Count());
        var unpostedInvoices = invoiceRows.Count(x => x.Status == "Draft");
        var unallocatedReceipts = await db.Set<M11Receipt>().AsNoTracking()
            .Where(x => x.Status == "Draft" && x.ReceiptDate >= start && x.ReceiptDate <= end &&
                        (allBranches || branchIds.Contains(x.BranchId) || ownScope && x.CreatedByUserId == actor.UserId))
            .CountAsync(ct);

        return Results.Ok(new
        {
            period = new { from = start, to = end },
            lastRefreshedAt = DateTimeOffset.UtcNow,
            sales = new
            {
                orders = orderRows.Count(x => x.Status is not ("Cancelled" or "Draft")),
                orderValue = orderRows.Where(x => x.Status is not ("Cancelled" or "Draft")).Sum(x => x.Total)
            },
            invoices = new
            {
                postedCount = postedInvoices.Length, postedValue = invoiceTotal,
                pendingPostCount = unpostedInvoices,
                allocatedReceipts = postedAllocations,
                outstandingEstimate = Math.Max(0m, invoiceTotal - postedAllocations)
            },
            operations = new
            {
                trips = tripRows.Count, byStatus = tripStatuses,
                plannedQuantity = tripRows.Sum(x => x.PlannedQuantity),
                deliveredQuantity = tripRows.Sum(x => x.DeliveredQuantity),
                unbilledChallans
            },
            expenses = new { postedValue = expenseTotal, costVisible = costAccess },
            attention = new { pendingApprovals = approvalCount, pendingInvoicePost = unpostedInvoices, receiptsToAllocate = unallocatedReceipts },
            unavailable = new[]
            {
                "Cash and bank balances: M12 cash/bank reconciliation is not implemented.",
                "Payables and supplier due dates: M07 supplier invoice posting is not implemented.",
                "Profit/margin: purchase-cost and M12 source postings are unavailable.",
                "Trends, user card preferences and notification urgency ranking are not implemented."
            }
        });
    }
}
