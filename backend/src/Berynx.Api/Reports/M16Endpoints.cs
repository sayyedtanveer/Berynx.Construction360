using System.Text;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Reports;
using Berynx.Application.Tenancy;
using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;
using Berynx.Domain.Expenses;
using Berynx.Domain.Reports;
using Berynx.Domain.SalesOrders;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Reports;

public static class M16Endpoints
{
    private static readonly string[] Supported =
        ["R-S03", "R-D01", "R-D04", "R-X01"];

    public static void MapM16(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m16")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M16",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/catalog", Catalog);
        api.MapGet("/reports/{code}", GetReport);
        api.MapGet("/reports/{code}/export.csv", ExportCsv);
    }

    private static async Task<IResult> Catalog(TenantAuthorizationService authorization, CancellationToken ct)
    {
        await authorization.CurrentUser(ct);
        await authorization.Require("M16", "m16.reports.view", ct);
        return Results.Ok(new
        {
            available = Supported,
            pending = new[]
            {
                "Sales register, customer/product sales, quotation analytics, enquiry funnel",
                "Purchase register/rate history/GRN not billed",
                "Receivables/payables ageing, collections, retention",
                "Profitability and margin reconciliation",
                "Trial balance/P&L/balance sheet and cash books",
                "GST portal packs, audit/login, saved views, scheduled email, Excel/PDF export"
            }
        });
    }

    private static async Task<IResult> GetReport(
        string code, DateOnly? from, DateOnly? to, Guid? branchId, Guid? customerId,
        Guid? siteId, Guid? productId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var result = await BuildReport(code, from, to, branchId, customerId, siteId, productId, authorization, db, ct);
        return result is null ? Results.NotFound(new { detail = "Report is not implemented yet." }) : Results.Ok(result);
    }

    private static async Task<IResult> ExportCsv(
        string code, DateOnly? from, DateOnly? to, Guid? branchId, Guid? customerId,
        Guid? siteId, Guid? productId, TenantAuthorizationService authorization,
        ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        await authorization.Require("M16", "m16.reports.export", ct);
        var result = await BuildReport(code, from, to, branchId, customerId, siteId, productId, authorization, db, ct);
        if (result is null) return Results.NotFound(new { detail = "Report is not implemented yet." });
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", result.Columns.Select(Escape)));
        foreach (var row in result.Rows)
            builder.AppendLine(string.Join(",", result.Columns.Select(column =>
                Escape(row.TryGetValue(column, out var value) ? Format(value) : ""))));
        builder.AppendLine(string.Join(",", result.Columns.Select(column =>
            Escape(result.Totals.TryGetValue(column, out var value) ? Format(value) :
                column == result.Columns.FirstOrDefault() ? "TOTAL" : ""))));
        var actor = await authorization.CurrentUser(ct);
        db.Add(new M16ExportLog
        {
            TenantId = tenant.TenantId, ExportId = Guid.NewGuid(), UserId = actor.UserId,
            ReportCode = code, FiltersJson = JsonSerializer.Serialize(new { from = result.From, to = result.To, branchId, customerId, siteId, productId }),
            RowCount = result.Rows.Count, ExportedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return Results.File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv; charset=utf-8",
            $"{code.ToLowerInvariant()}-{result.From:yyyyMMdd}-{result.To:yyyyMMdd}.csv");
    }

    private static async Task<M16ReportResult?> BuildReport(
        string code, DateOnly? from, DateOnly? to, Guid? branchId, Guid? customerId,
        Guid? siteId, Guid? productId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M16", "m16.reports.view", ct);
        if (!Supported.Contains(code, StringComparer.OrdinalIgnoreCase)) return null;
        var start = from ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (start > end || end.DayNumber - start.DayNumber > 370)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Report period is invalid or exceeds 371 days.");
        var scopes = await authorization.Scopes(actor.UserId, "m16.reports.view", ct);
        var allScope = scopes.Any(x => x.ScopeType == "All");
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        var own = scopes.Any(x => x.ScopeType == "Own");
        if (branchId.HasValue && !allScope && !branches.Contains(branchId.Value))
            return null;
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        var totals = new Dictionary<string, object?>();
        var columns = new List<string>();

        if (code.Equals("R-S03", StringComparison.OrdinalIgnoreCase))
        {
            var query = from order in db.Set<M06SalesOrder>().AsNoTracking()
                        join line in db.Set<M06SalesOrderLine>().AsNoTracking()
                            on new { order.TenantId, order.SalesOrderId } equals new { line.TenantId, line.SalesOrderId }
                        where order.OrderDate >= start && order.OrderDate <= end &&
                              (order.Status == "Confirmed" || order.Status == "In Fulfilment" || order.Status == "On Hold")
                        select new { order, line };
            if (!allScope) query = query.Where(x =>
                x.order.BranchId.HasValue && branches.Contains(x.order.BranchId.Value) ||
                own && x.order.SalesOwnerUserId == actor.UserId);
            if (branchId.HasValue) query = query.Where(x => x.order.BranchId == branchId);
            if (customerId.HasValue) query = query.Where(x => x.order.CustomerPartyId == customerId);
            if (siteId.HasValue) query = query.Where(x => x.order.SiteId == siteId);
            if (productId.HasValue) query = query.Where(x => x.line.ProductId == productId);
            var source = await query.OrderBy(x => x.order.Number).ThenBy(x => x.line.ProductNameSnapshot)
                .Select(x => new
                {
                    x.order.SalesOrderId, x.order.Number, x.order.CustomerNameSnapshot,
                    x.order.SiteAddressSnapshot, x.order.Status,
                    x.line.ProductNameSnapshot, x.line.UnitCode, x.line.OrderedQuantity,
                    x.line.DeliveredQuantity, x.line.Rate, x.line.Total
                }).ToListAsync(ct);
            columns.AddRange(["Order", "Customer", "Site", "Status", "Product", "UOM", "Ordered", "Delivered", "Balance quantity", "Balance value"]);
            foreach (var x in source)
            {
                var remaining = Math.Max(0m, x.OrderedQuantity - x.DeliveredQuantity);
                var value = x.OrderedQuantity == 0 ? 0m : x.Total * remaining / x.OrderedQuantity;
                rows.Add(new Dictionary<string, object?>
                {
                    ["Order"] = x.Number, ["Customer"] = x.CustomerNameSnapshot, ["Site"] = x.SiteAddressSnapshot,
                    ["Status"] = x.Status, ["Product"] = x.ProductNameSnapshot, ["UOM"] = x.UnitCode,
                    ["Ordered"] = x.OrderedQuantity, ["Delivered"] = x.DeliveredQuantity,
                    ["Balance quantity"] = remaining, ["Balance value"] = decimal.Round(value, 2)
                });
            }
            totals["Balance quantity"] = source.Sum(x => Math.Max(0m, x.OrderedQuantity - x.DeliveredQuantity));
            totals["Balance value"] = rows.Sum(x => (decimal)x["Balance value"]!);
        }
        else if (code.Equals("R-D01", StringComparison.OrdinalIgnoreCase))
        {
            var query = db.Set<M08Trip>().AsNoTracking().Where(x =>
                x.CreatedAt >= new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) &&
                x.CreatedAt < new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
            if (!allScope) query = query.Where(x => branches.Contains(x.BranchId) || own && x.CreatedByUserId == actor.UserId);
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
            var source = await query.Select(x => new
            {
                x.Number, x.Status, x.PlannedQuantity, x.DeliveredQuantity,
                x.WaitingMinutes, x.ExceptionType
            }).ToListAsync(ct);
            columns.AddRange(["Trip", "Status", "Planned quantity", "Delivered quantity", "Waiting minutes", "Exception"]);
            foreach (var x in source)
                rows.Add(new Dictionary<string, object?>
                {
                    ["Trip"] = x.Number, ["Status"] = x.Status, ["Planned quantity"] = x.PlannedQuantity,
                    ["Delivered quantity"] = x.DeliveredQuantity, ["Waiting minutes"] = x.WaitingMinutes,
                    ["Exception"] = x.ExceptionType
                });
            totals["Trips"] = source.Count;
            totals["Delivered quantity"] = source.Sum(x => x.DeliveredQuantity);
        }
        else if (code.Equals("R-D04", StringComparison.OrdinalIgnoreCase))
        {
            var query = db.Set<M09DeliveryChallan>().AsNoTracking()
                .Where(x => x.InvoiceId == null && x.ChallanDate >= start && x.ChallanDate <= end);
            if (!allScope) query = query.Where(x => branches.Contains(x.BranchId) || own && x.CreatedByUserId == actor.UserId);
            if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
            if (customerId.HasValue) query = query.Where(x => x.CustomerPartyId == customerId);
            var source = await query.Select(x => new
            {
                x.Number, x.ChallanDate, x.CustomerNameSnapshot, x.OrderNumberSnapshot,
                x.DeliveredQuantity, x.DeliveredValueSnapshot
            }).ToListAsync(ct);
            columns.AddRange(["Challan", "Date", "Customer", "Order", "Quantity", "Value", "Age days"]);
            foreach (var x in source)
                rows.Add(new Dictionary<string, object?>
                {
                    ["Challan"] = x.Number, ["Date"] = x.ChallanDate, ["Customer"] = x.CustomerNameSnapshot,
                    ["Order"] = x.OrderNumberSnapshot, ["Quantity"] = x.DeliveredQuantity,
                    ["Value"] = x.DeliveredValueSnapshot,
                    ["Age days"] = Math.Max(0, DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - x.ChallanDate.DayNumber)
                });
            totals["Unbilled quantity"] = source.Sum(x => x.DeliveredQuantity);
            totals["Unbilled value"] = source.Sum(x => x.DeliveredValueSnapshot);
        }
        else if (code.Equals("R-X01", StringComparison.OrdinalIgnoreCase))
        {
            await authorization.Require("M16", "global.view_cost", ct);
            var costScopes = await authorization.Scopes(actor.UserId, "global.view_cost", ct);
            var costAll = costScopes.Any(x => x.ScopeType == "All");
            var costBranches = costScopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var costOwn = costScopes.Any(x => x.ScopeType == "Own");
            var query = from expense in db.Set<M14Expense>().AsNoTracking()
                        join category in db.Set<M14ExpenseCategory>().AsNoTracking()
                            on new { expense.TenantId, expense.CategoryId } equals new { category.TenantId, category.CategoryId }
                        where expense.Status == "Posted" && expense.ExpenseDate >= start && expense.ExpenseDate <= end
                        select new { expense, category };
            if (!allScope) query = query.Where(x => branches.Contains(x.expense.BranchId) ||
                                                    own && x.expense.CreatedByUserId == actor.UserId);
            if (!costAll) query = query.Where(x => costBranches.Contains(x.expense.BranchId) ||
                                                  costOwn && x.expense.CreatedByUserId == actor.UserId);
            if (branchId.HasValue) query = query.Where(x => x.expense.BranchId == branchId);
            if (siteId.HasValue) query = query.Where(x => x.expense.SiteId == siteId);
            var source = await query.GroupBy(x => new { x.category.Name, x.category.Type })
                .Select(x => new { x.Key.Name, x.Key.Type, Amount = x.Sum(y => y.expense.Amount), Count = x.Count() })
                .ToListAsync(ct);
            columns.AddRange(["Category", "Type", "Amount", "Count"]);
            foreach (var x in source)
                rows.Add(new Dictionary<string, object?> { ["Category"] = x.Name, ["Type"] = x.Type, ["Amount"] = x.Amount, ["Count"] = x.Count });
            totals["Amount"] = source.Sum(x => x.Amount);
            totals["Count"] = source.Sum(x => x.Count);
        }
        return new M16ReportResult(code.ToUpperInvariant(), start, end, DateTimeOffset.UtcNow, columns, rows, totals);
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        DateOnly date => date.ToString("yyyy-MM-dd"),
        decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? ""
    };

    private static string Escape(string value) =>
        "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
