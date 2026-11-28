using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Gst;
using Berynx.Application.Tenancy;
using Berynx.Domain.Gst;
using Berynx.Domain.Invoicing;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Gst;

public static class M13Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed record ExceptionRow(Guid InvoiceId, string Code, string Severity);

    public static void MapM13(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m13")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M13",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/workspace", Workspace);
        api.MapGet("/gstr1/export", ExportGstr1);
        api.MapPost("/exceptions/review", ReviewException);
        api.MapPost("/periods/filed", MarkFiled);
    }

    private static async Task<IResult> Workspace(
        DateOnly periodStart, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M13", "m13.gst.view", ct);
        var periodEnd = M13Rules.MonthEnd(periodStart);
        var scopes = await authorization.Scopes(actor.UserId, "m13.gst.view", ct);
        var query = db.Set<M10Invoice>().AsNoTracking()
            .Where(x => x.Status == "Posted" && x.InvoiceDate >= periodStart && x.InvoiceDate <= periodEnd);
        query = ApplyBranchScope(query, scopes);
        var invoices = await query
            .OrderBy(x => x.InvoiceDate)
            .Select(x => new
            {
                x.InvoiceId, x.Number, x.InvoiceDate, x.CustomerNameSnapshot,
                x.CustomerGstinSnapshot, x.PlaceOfSupplyStateCode, x.BranchGstinSnapshot,
                x.TaxableAmount, x.CgstAmount, x.SgstAmount, x.IgstAmount,
                x.EInvoiceStatus, x.Irn
            }).ToListAsync(ct);
        var invoiceIds = invoices.Select(x => x.InvoiceId).ToArray();
        var lines = await db.Set<M10InvoiceLine>().AsNoTracking()
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .Select(x => new
            {
                x.InvoiceId, x.ProductNameSnapshot, x.HsnCodeSnapshot,
                x.Quantity, x.TaxableAmount, x.TaxPercent,
                x.CgstAmount, x.SgstAmount, x.IgstAmount
            }).ToListAsync(ct);
        var exceptionRows = new List<ExceptionRow>();
        foreach (var invoice in invoices)
        {
            if (string.IsNullOrWhiteSpace(invoice.CustomerGstinSnapshot))
                exceptionRows.Add(new(invoice.InvoiceId, "Missing customer GSTIN", "Error"));
            if (string.IsNullOrWhiteSpace(invoice.PlaceOfSupplyStateCode))
                exceptionRows.Add(new(invoice.InvoiceId, "Missing place of supply", "Error"));
            if (invoice.Number.Length > 16 || string.IsNullOrWhiteSpace(invoice.Number))
                exceptionRows.Add(new(invoice.InvoiceId, "Invoice number invalid", "Error"));
            if (invoice.EInvoiceStatus == "Required" && string.IsNullOrWhiteSpace(invoice.Irn))
                exceptionRows.Add(new(invoice.InvoiceId, "Required IRN missing", "Error"));
        }
        foreach (var line in lines.Where(x => string.IsNullOrWhiteSpace(x.HsnCodeSnapshot)))
            exceptionRows.Add(new(line.InvoiceId, $"Missing HSN: {line.ProductNameSnapshot}", "Error"));

        var reviewed = await db.Set<M13ExceptionReview>().AsNoTracking()
            .Where(x => x.PeriodStart == periodStart)
            .Select(x => new { x.InvoiceId, x.ExceptionCode, x.ReviewNote, x.ReviewedAt })
            .ToListAsync(ct);
        var totalTaxable = invoices.Sum(x => x.TaxableAmount);
        var cgst = invoices.Sum(x => x.CgstAmount);
        var sgst = invoices.Sum(x => x.SgstAmount);
        var igst = invoices.Sum(x => x.IgstAmount);
        var period = await db.Set<M13GstPeriod>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.PeriodStart == periodStart, ct);
        var totalChecks = invoices.Count * 4 + lines.Count;
        var blocking = exceptionRows.Count(x => x.Severity == "Error");
        var readiness = M13Rules.ReadinessPercent(totalChecks, Math.Max(0, totalChecks - blocking));
        return Results.Ok(new
        {
            periodStart, periodEnd,
            status = period?.Status ?? "Open",
            readinessPercent = readiness,
            blockingErrors = blocking,
            invoices,
            lines,
            exceptions = exceptionRows,
            reviewedExceptions = reviewed,
            outwardTax = new { taxable = totalTaxable, cgst, sgst, igst, total = cgst + sgst + igst },
            itc = new { eligible = 0m, ineligible = 0m, reverseCharge = 0m, status = "Pending M07 supplier invoice and M12 posting integration" },
            gstr3b = new { outputTax = cgst + sgst + igst, inputTaxCredit = 0m, netPayable = cgst + sgst + igst },
            externalPortalFiling = false
        });
    }

    private static async Task<IResult> ExportGstr1(
        DateOnly periodStart, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M13", "m13.gst.export", ct);
        var periodEnd = M13Rules.MonthEnd(periodStart);
        var scopes = await authorization.Scopes(actor.UserId, "m13.gst.export", ct);
        var invoiceQuery = db.Set<M10Invoice>().AsNoTracking()
            .Where(x => x.Status == "Posted" && x.InvoiceDate >= periodStart && x.InvoiceDate <= periodEnd);
        invoiceQuery = ApplyBranchScope(invoiceQuery, scopes);
        var invoices = await invoiceQuery
            .ToListAsync(ct);
        var invoiceIds = invoices.Select(x => x.InvoiceId).ToArray();
        var lines = await db.Set<M10InvoiceLine>().AsNoTracking()
            .Where(x => invoiceIds.Contains(x.InvoiceId)).ToListAsync(ct);
        var payload = new
        {
            format = "Berynx GSTR-1 extract; not validated against the current GST Offline Tool schema",
            periodStart,
            periodEnd,
            b2b = invoices.Where(x => !string.IsNullOrWhiteSpace(x.CustomerGstinSnapshot)).Select(invoice => new
            {
                invoiceNumber = invoice.Number,
                invoiceDate = invoice.InvoiceDate,
                recipientGstin = invoice.CustomerGstinSnapshot,
                placeOfSupply = invoice.PlaceOfSupplyStateCode,
                taxableValue = invoice.TaxableAmount,
                cgst = invoice.CgstAmount,
                sgst = invoice.SgstAmount,
                igst = invoice.IgstAmount,
                lines = lines.Where(x => x.InvoiceId == invoice.InvoiceId).Select(x => new
                {
                    x.HsnCodeSnapshot, x.ProductNameSnapshot, x.Quantity,
                    x.TaxableAmount, x.TaxPercent, x.CgstAmount, x.SgstAmount, x.IgstAmount
                })
            }),
            warnings = new[] { "B2CL/B2CS, exempt, credit/debit note, document-series, and portal schema validation are not implemented." }
        };
        return Results.Json(payload, Json);
    }

    private static async Task<IResult> ReviewException(
        M13ExceptionReviewRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M13", "m13.exceptions.review", ct);
        if (string.IsNullOrWhiteSpace(request.ExceptionCode) || string.IsNullOrWhiteSpace(request.ReviewNote))
            throw new System.ComponentModel.DataAnnotations.ValidationException("Exception code and review note are required.");
        var period = M13Rules.MonthEnd(request.PeriodStart);
        var filing = await db.Set<M13GstPeriod>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.PeriodStart == request.PeriodStart, ct);
        if (filing?.Status == "Filed")
            return Results.Conflict(new { detail = "Filed GST periods are locked; correct source documents in a later period." });
        var invoice = await db.Set<M10Invoice>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.InvoiceId == request.InvoiceId &&
                                      x.InvoiceDate >= request.PeriodStart && x.InvoiceDate <= period, ct);
        if (invoice is null)
            return Results.NotFound(new { detail = "Invoice is outside the selected GST period." });
        var scopes = await authorization.Scopes(actor.UserId, "m13.exceptions.review", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == invoice.BranchId))
            return Results.Forbid();
        var review = await db.Set<M13ExceptionReview>().SingleOrDefaultAsync(x =>
            x.PeriodStart == request.PeriodStart && x.InvoiceId == request.InvoiceId &&
            x.ExceptionCode == request.ExceptionCode, ct);
        if (review is null)
        {
            review = new M13ExceptionReview
            {
                TenantId = tenant.TenantId, ReviewId = Guid.NewGuid(),
                PeriodStart = request.PeriodStart, InvoiceId = request.InvoiceId,
                ExceptionCode = request.ExceptionCode
            };
            db.Add(review);
        }
        review.ReviewNote = request.ReviewNote.Trim();
        review.ReviewedByUserId = actor.UserId;
        review.ReviewedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { review.ReviewId, review.ReviewedAt });
    }

    private static async Task<IResult> MarkFiled(
        M13MarkFiledRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M13", "m13.periods.file", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m13.periods.file", ct);
        if (!scopes.Any(x => x.ScopeType == "All"))
            return Results.Forbid();
        var end = M13Rules.MonthEnd(request.PeriodStart);
        var invoices = await db.Set<M10Invoice>().AsNoTracking()
            .Where(x => x.Status == "Posted" && x.InvoiceDate >= request.PeriodStart && x.InvoiceDate <= end)
            .ToListAsync(ct);
        var invoiceIds = invoices.Select(x => x.InvoiceId).ToArray();
        var lines = await db.Set<M10InvoiceLine>().AsNoTracking()
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .Select(x => new { x.InvoiceId, x.HsnCodeSnapshot, x.ProductNameSnapshot }).ToListAsync(ct);
        var blocking = invoices.Count(x =>
                string.IsNullOrWhiteSpace(x.CustomerGstinSnapshot) ||
                string.IsNullOrWhiteSpace(x.PlaceOfSupplyStateCode) ||
                string.IsNullOrWhiteSpace(x.Number) || x.Number.Length > 16 ||
                x.EInvoiceStatus == "Required" && string.IsNullOrWhiteSpace(x.Irn)) +
            lines.Count(x => string.IsNullOrWhiteSpace(x.HsnCodeSnapshot));
        M13Rules.RequireReadyToFile(blocking);
        var row = await db.Set<M13GstPeriod>().SingleOrDefaultAsync(x => x.PeriodStart == request.PeriodStart, ct);
        if (row?.Status == "Filed")
            return Results.Conflict(new { detail = "GST period is already filed." });
        if (row is null)
        {
            row = new M13GstPeriod
            {
                TenantId = tenant.TenantId, PeriodStart = request.PeriodStart, PeriodEnd = end
            };
            db.Add(row);
        }
        row.Status = "Filed";
        row.FiledAt = DateTimeOffset.UtcNow;
        row.FiledByUserId = actor.UserId;
        row.ExternalAcknowledgement = request.ExternalAcknowledgement?.Trim() ?? "";
        row.FilingSnapshotJson = JsonSerializer.Serialize(new
        {
            invoiceCount = invoiceIds.Length, export = "Internal extract; portal filing remains manual"
        }, Json);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { row.PeriodStart, row.PeriodEnd, row.Status, row.FiledAt, externalPortalFiling = false });
    }

    private static IQueryable<M10Invoice> ApplyBranchScope(
        IQueryable<M10Invoice> query, IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        return query.Where(x => branches.Contains(x.BranchId));
    }
}
