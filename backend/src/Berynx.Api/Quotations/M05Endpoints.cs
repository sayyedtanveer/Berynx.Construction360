using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Berynx.Api.Crm;
using Berynx.Api.Masters;
using Berynx.Api.Onboarding;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.Crm;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Quotations;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Quotations;

public interface IM05CustomerOtpProvider
{
    bool IsConfigured { get; }
    Task<bool> Send(Guid tenantId, Guid quotationId, string mobile, CancellationToken cancellationToken);
    Task<bool> Verify(Guid tenantId, Guid quotationId, string mobile, string code, CancellationToken cancellationToken);
}

public sealed class UnconfiguredM05CustomerOtpProvider : IM05CustomerOtpProvider
{
    public bool IsConfigured => false;
    public Task<bool> Send(Guid tenantId, Guid quotationId, string mobile, CancellationToken cancellationToken) =>
        Task.FromResult(false);
    public Task<bool> Verify(Guid tenantId, Guid quotationId, string mobile, string code,
        CancellationToken cancellationToken) => Task.FromResult(false);
}

public static class M05Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed record PublicTenantScope(Guid TenantId) : ICurrentTenantContext;

    public sealed record LineWrite(
        Guid ProductId, decimal Quantity, string UnitCode, decimal? Rate, decimal DiscountPercent,
        string? RmcGrade = null, string? RmcSlumpBand = null,
        string? RmcAdmixtureCode = null, string? RmcPumpType = null);
    public sealed record ChargeWrite(Guid? ChargeTypeId, string Description, decimal Amount);
    public sealed record QuoteWrite(
        Guid CustomerPartyId, Guid? SiteId, Guid? ContactId, Guid? BranchId,
        string PriceBasis, DateOnly ValidUntil, Guid? PaymentTermId,
        string PlaceOfSupplyStateCode, string DeliveryTerms, string TermsAndConditions,
        string InternalNotes, LineWrite[] Lines, ChargeWrite[] Charges, string IdempotencyKey,
        long Version = 0);
    public sealed record QuoteFromEnquiryWrite(
        string PriceBasis, DateOnly ValidUntil, Guid? PaymentTermId,
        string PlaceOfSupplyStateCode, string DeliveryTerms, string TermsAndConditions,
        string InternalNotes, string IdempotencyKey, decimal? DiscountPercent = null);
    public sealed record PublicOtpRequest(string Token, string Mobile);
    public sealed record PublicDecisionRequest(
        string Token, string Mobile, string Otp, string Decision, string? RejectionReason);


    private sealed record BuiltLine(M05QuotationLine Line, bool BelowMinimum, decimal DiscountPercent);
    private sealed record BuiltQuote(
        M03Party Customer, M03Site? Site, M03Contact? Contact, Guid? BranchId,
        string PlaceOfSupply, string SellerGstin, string CompanyName,
        List<BuiltLine> Lines, List<M05QuotationCharge> Charges,
        decimal Subtotal, decimal DiscountTotal, decimal ChargesTotal, decimal TaxTotal, decimal Total,
        bool BelowMinimum, object RateComparisons);

    public static void MapM05(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m05")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M05",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/quotations", List);
        api.MapGet("/quotations/{quotationId:guid}", Detail);
        api.MapPost("/quotations", Create);
        api.MapPost("/quotations/from-enquiry/{enquiryId:guid}", CreateFromEnquiry);
        api.MapPut("/quotations/{quotationId:guid}", UpdateDraft);
        api.MapPost("/quotations/{quotationId:guid}/revise", Revise);
        api.MapPost("/quotations/{quotationId:guid}/clone", Clone);
        api.MapPost("/quotations/{quotationId:guid}/issue", Issue);
        api.MapPost("/quotations/{quotationId:guid}/cancel", Cancel);

        api.MapGet("/quotations/{quotationId:guid}/print", PrintView);
        api.MapGet("/customers/{customerId:guid}/quotations", CustomerHistory);

        var publicApi = app.MapGroup("/api/v1/m05/public/quotes").AllowAnonymous();
        publicApi.MapGet("/{token}", PublicView);
        publicApi.MapPost("/{token}/otp", SendOtp);
        publicApi.MapPost("/{token}/decision", PublicDecision);
    }

    private static async Task<IResult> List(
        string? q, string? status, Guid? customerId, Guid? ownerId, Guid? branchId,
        int page, int pageSize, TenantAuthorizationService authorization, BerynxDbContext db,
        CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.view", ct);
        var scopes = await authorization.Scopes(user.UserId, "m05.quotations.view", ct);
        var query = db.Set<M05Quotation>().AsNoTracking().Where(x => x.IsActiveRevision);
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x =>
                (own && x.SalespersonUserId == user.UserId) ||
                (x.BranchId != null && branches.Contains(x.BranchId.Value)));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x => (x.Number ?? "").Contains(search) ||
                                     x.CustomerNameSnapshot.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (customerId is not null) query = query.Where(x => x.CustomerPartyId == customerId);
        if (ownerId is not null) query = query.Where(x => x.SalespersonUserId == ownerId);
        if (branchId is not null) query = query.Where(x => x.BranchId == branchId);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
        var total = await query.LongCountAsync(ct);
        var rows = await query.OrderByDescending(x => x.UpdatedAt).Skip((page - 1) * pageSize)
            .Take(pageSize).Select(x => new
            {
                x.QuotationId, x.SeriesId, x.RevisionNumber, x.Number, x.CustomerPartyId,
                x.CustomerNameSnapshot, x.SiteId, x.BranchId, x.SalespersonUserId, x.Status,
                x.PriceBasis, x.ValidUntil, x.Total, x.Version, x.CreatedAt, x.UpdatedAt
            }).ToListAsync(ct);
        return Results.Ok(new { page, pageSize, total, rows });
    }

    private static async Task<IResult> Detail(
        Guid quotationId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.view", ct);
        var quotation = await db.Set<M05Quotation>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct);
        if (quotation is null || !await CanView(authorization, actor.UserId, quotation, ct))
            return Results.NotFound();
        var lines = await db.Set<M05QuotationLine>().AsNoTracking()
            .Where(x => x.QuotationId == quotationId).ToListAsync(ct);
        var charges = await db.Set<M05QuotationCharge>().AsNoTracking()
            .Where(x => x.QuotationId == quotationId).ToListAsync(ct);
        var activities = await db.Set<M05QuotationActivity>().AsNoTracking()
            .Where(x => x.QuotationId == quotationId).OrderByDescending(x => x.At).Take(200).ToListAsync(ct);
        var approval = quotation.ApprovalRequestId is { } approvalId
            ? await db.Set<M02ApprovalRequest>().AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == approvalId, ct)
            : null;
        var canViewCost = await authorization.CanAccess("M03", actor.UserId, "global.view_cost",
            quotation.BranchId, null, quotation.SalespersonUserId, ct, write: false);
        return Results.Ok(new
        {
            quotation.QuotationId, quotation.SeriesId, quotation.RevisionNumber, quotation.Number,
            quotation.SourceEnquiryId, quotation.CustomerPartyId, quotation.CustomerNameSnapshot,
            quotation.CustomerGstinSnapshot, quotation.CustomerAddressSnapshot, quotation.SiteId,
            quotation.SiteAddressSnapshot, quotation.BranchId, quotation.SalespersonUserId,
            quotation.Status, quotation.PriceBasis, quotation.ValidUntil, quotation.PaymentTermId,
            quotation.PlaceOfSupplyStateCode, quotation.DeliveryTerms, quotation.TermsAndConditions,
            quotation.InternalNotes, quotation.Subtotal, quotation.DiscountTotal, quotation.ChargesTotal,
            quotation.TaxTotal, quotation.Total, quotation.CostStatus,
            MarginPercent = canViewCost ? quotation.MarginPercent : null,
            RateComparison = canViewCost ? quotation.RateComparisonJson : null,
            quotation.ApprovalSnapshotJson, approvalStatus = approval?.Status,
            quotation.ApprovalRequestId, quotation.Version, quotation.CreatedAt, quotation.UpdatedAt,
            quotation.IssuedAt, quotation.AcceptedAt, quotation.RejectedAt, Lines = lines, Charges = charges, Activities = activities
        });
    }

    private static async Task<IResult> Create(
        QuoteWrite request, TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, M03MasterService masters, OnboardingStore onboarding,
        IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.create", ct);
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new ValidationException("An idempotency key is required.");
        var idempotencyKey = request.IdempotencyKey.Trim();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lockName = $"M05:{tenant.TenantId:N}:{idempotencyKey}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockName}, 0))", ct);
        var prior = await db.Set<M05Quotation>().AsNoTracking().SingleOrDefaultAsync(
            x => x.IdempotencyKey == idempotencyKey, ct);
        if (prior is not null)
        {
            await transaction.CommitAsync(ct);
            return Results.Ok(new { prior.QuotationId, prior.Number, prior.Status, prior.Version, duplicate = true });
        }
        var built = await Build(tenant.TenantId, actor, request, db, masters, onboarding, authorization, ct);
        if (!await CanWrite(authorization, actor.UserId, "m05.quotations.create", built.BranchId,
                actor.UserId, ct))
            throw new UnauthorizedAccessException("The quotation branch is outside your assignment scope.");
        if (request.Lines.Any(x => x.Rate is not null) &&
            !await authorization.CanAccess("M05", actor.UserId, "m05.quotations.price_override",
                built.BranchId, null, actor.UserId, ct, write: true))
            throw new UnauthorizedAccessException("Manual quotation rates require price-override permission.");
        var now = DateTimeOffset.UtcNow;
        var quote = NewQuotation(request, built, tenant.TenantId, actor.UserId, now);
        quote.QuotationId = Guid.NewGuid();
        quote.SeriesId = Guid.NewGuid();
        quote.SourceEnquiryId = null;
        quote.RevisionNumber = 1;
        quote.IdempotencyKey = idempotencyKey;
        quote.RateComparisonJson = JsonSerializer.Serialize(built.RateComparisons, Json);
        quote.ApprovalSnapshotJson = JsonSerializer.Serialize(new
        {
            costStatus = "Unavailable",
            belowMinimumRate = built.BelowMinimum,
            snapshot = "Approval thresholds evaluated by M02 at submission."
        }, Json);
        db.Add(quote);
        db.AddRange(built.Lines.Select(x => CopyLine(x.Line, quote)));
        db.AddRange(built.Charges.Select(x => CopyCharge(x, quote)));
        await db.SaveChangesAsync(ct);
        var approvalId = await SubmitApprovalIfRequired(quote, built, actor, approvals, ct);
        quote.ApprovalRequestId = approvalId;
        quote.Status = approvalId is null ? "Approved" : "Pending approval";
        AddActivity(db, quote, actor.UserId, "Created", new { quote.Status, quote.Total, quote.Version });
        AddAudit(db, quote, actor, "Quotation created", new { quote.Number, quote.Total, quote.Status });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/m05/quotations/{quote.QuotationId}",
            new { quote.QuotationId, quote.Status, quote.Total, quote.Version, duplicate = false });
    }

    private static async Task<IResult> CreateFromEnquiry(
        Guid enquiryId, QuoteFromEnquiryWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, M03MasterService masters,
        OnboardingStore onboarding, IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.create", ct);
        await authorization.Require("M04", "m04.enquiries.view", ct);
        var enquiry = await db.Set<M04Enquiry>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null || enquiry.Status is "Won" or "Lost" ||
            !await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.view",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: false))
            return Results.NotFound();
        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? throw new ValidationException("An idempotency key is required.")
            : request.IdempotencyKey.Trim();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lockName = $"M05:{tenant.TenantId:N}:{key}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockName}, 0))", ct);
        var prior = await db.Set<M05Quotation>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, ct);
        if (prior is not null)
        {
            await transaction.CommitAsync(ct);
            return Results.Ok(new { prior.QuotationId, prior.Number, prior.Status, prior.Version, duplicate = true });
        }

        var enquiryLines = await db.Set<M04EnquiryLine>().AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId).OrderBy(x => x.LineId).ToListAsync(ct);
        if (enquiryLines.Count == 0)
            throw new ValidationException("The enquiry has no product requirements to quote.");
        var write = new QuoteWrite(enquiry.CustomerPartyId ?? Guid.Empty, enquiry.SiteId, enquiry.ContactId,
            enquiry.BranchId, request.PriceBasis, request.ValidUntil, request.PaymentTermId,
            request.PlaceOfSupplyStateCode, request.DeliveryTerms, request.TermsAndConditions,
            request.InternalNotes,
            enquiryLines.Select(x => new LineWrite(x.ProductId, x.Quantity, x.UnitCode, null,
                request.DiscountPercent ?? 0m, x.RmcGrade, x.RmcSlumpBand,
                x.RmcAdmixtureCode, x.RmcPumpType)).ToArray(), [], key);
        var built = await Build(tenant.TenantId, actor, write, db, masters, onboarding, authorization, ct);
        if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            throw new UnauthorizedAccessException("You cannot create a quotation from this enquiry.");
        if (!await CanWrite(authorization, actor.UserId, "m05.quotations.create",
                built.BranchId, actor.UserId, ct))
            throw new UnauthorizedAccessException("The quotation branch is outside your assignment scope.");
        var now = DateTimeOffset.UtcNow;
        var quote = NewQuotation(write, built, tenant.TenantId, actor.UserId, now);
        quote.QuotationId = Guid.NewGuid();
        quote.SeriesId = Guid.NewGuid();
        quote.SourceEnquiryId = enquiryId;
        quote.IdempotencyKey = key;
        quote.RateComparisonJson = JsonSerializer.Serialize(built.RateComparisons, Json);
        quote.ApprovalSnapshotJson = JsonSerializer.Serialize(new
        {
            costStatus = "Unavailable",
            belowMinimumRate = built.BelowMinimum
        }, Json);
        db.Add(quote);
        db.AddRange(built.Lines.Select(x => CopyLine(x.Line, quote)));
        db.AddRange(built.Charges.Select(x => CopyCharge(x, quote)));
        await db.SaveChangesAsync(ct);
        quote.ApprovalRequestId = await SubmitApprovalIfRequired(quote, built, actor, approvals, ct);
        quote.Status = quote.ApprovalRequestId is null ? "Approved" : "Pending approval";
        AddActivity(db, quote, actor.UserId, "CreatedFromEnquiry",
            new { sourceEnquiryId = enquiryId, enquiry.Number, quote.Total, quote.Status });
        AddAudit(db, quote, actor, "Quotation created from enquiry",
            new { sourceEnquiryId = enquiryId, enquiry.Number, quote.Total });
        AddAudit(db, tenant.TenantId, actor, "Quotation created from enquiry", "M03Party",
            enquiry.CustomerPartyId, null, new { quotationId = quote.QuotationId, enquiryId, quote.Number });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/m05/quotations/{quote.QuotationId}",
            new { quote.QuotationId, quote.SourceEnquiryId, quote.Status, quote.Total, quote.Version });
    }

    private static async Task<IResult> UpdateDraft(
        Guid quotationId, QuoteWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, M03MasterService masters,
        OnboardingStore onboarding, IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.edit", ct);
        var quote = await db.Set<M05Quotation>().SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct);
        if (quote is null) return Results.NotFound();
        if (!await CanView(authorization, actor.UserId, quote, ct)) return Results.NotFound();
        if (!await CanWrite(authorization, actor.UserId, "m05.quotations.edit",
                quote.BranchId, quote.SalespersonUserId, ct))
            throw new UnauthorizedAccessException("The quotation is outside your edit scope.");
        if (quote.Status is not ("Draft" or "Approved") || quote.Version != request.Version)
            return Results.Conflict(new { detail = "Only a current draft can be edited in place. Create a revision for an issued quotation." });
        if (quote.Status == "Approved" && quote.ApprovalRequestId is not null)
            throw new ValidationException("An approved quotation must be revised to preserve its approval snapshot.");
        var built = await Build(tenant.TenantId, actor, request, db, masters, onboarding, authorization, ct);
        var before = new { quote.Status, quote.Total, quote.Version };
        ApplyQuote(quote, request, built);
        quote.Version++;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        quote.Status = "Draft";
        db.RemoveRange(await db.Set<M05QuotationLine>().Where(x => x.QuotationId == quotationId).ToListAsync(ct));
        db.RemoveRange(await db.Set<M05QuotationCharge>().Where(x => x.QuotationId == quotationId).ToListAsync(ct));
        db.AddRange(built.Lines.Select(x => CopyLine(x.Line, quote)));
        db.AddRange(built.Charges.Select(x => CopyCharge(x, quote)));
        quote.RateComparisonJson = JsonSerializer.Serialize(built.RateComparisons, Json);
        quote.ApprovalSnapshotJson = JsonSerializer.Serialize(new { costStatus = "Unavailable", belowMinimumRate = built.BelowMinimum }, Json);
        quote.ApprovalRequestId = await SubmitApprovalIfRequired(quote, built, actor, approvals, ct);
        quote.Status = quote.ApprovalRequestId is null ? "Approved" : "Pending approval";
        AddActivity(db, quote, actor.UserId, "Updated", new { before, quote.Status, quote.Total, quote.Version });
        AddAudit(db, quote, actor, "Quotation updated", new { before, quote.Status, quote.Total });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { quote.QuotationId, quote.Status, quote.Total, quote.Version });
    }

    private static async Task<IResult> Revise(
        Guid quotationId, QuoteWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, M03MasterService masters,
        OnboardingStore onboarding, IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.edit", ct);
        var original = await db.Set<M05Quotation>().SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct);
        if (original is null) return Results.NotFound();
        if (!await CanView(authorization, actor.UserId, original, ct)) return Results.NotFound();
        if (!await CanWrite(authorization, actor.UserId, "m05.quotations.edit",
                original.BranchId, original.SalespersonUserId, ct))
            throw new UnauthorizedAccessException("The quotation is outside your edit scope.");
        if (original.Version != request.Version ||
            original.Status is "Cancelled" or "Expired" or "Revised" or "Accepted" or "Rejected")
            return Results.Conflict(new { detail = "Reload the active quotation before creating a revision." });
        var built = await Build(tenant.TenantId, actor, request, db, masters, onboarding, authorization, ct);
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        original.Status = "Revised";
        original.IsActiveRevision = false;
        original.Version++;
        original.UpdatedAt = now;
        var oldLinks = await db.Set<M05QuotationLink>()
            .Where(x => x.QuotationId == original.QuotationId && x.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var link in oldLinks) link.RevokedAt = now;
        var revision = NewQuotation(request, built, tenant.TenantId, actor.UserId, now);
        revision.QuotationId = Guid.NewGuid();
        revision.SeriesId = original.SeriesId;
        revision.RevisionNumber = original.RevisionNumber + 1;
        revision.Number = original.Number is null ? null : $"{original.Number}-R{revision.RevisionNumber}";
        revision.SourceEnquiryId = original.SourceEnquiryId;
        revision.IdempotencyKey = $"revision:{original.SeriesId:N}:{revision.RevisionNumber}";
        revision.RateComparisonJson = JsonSerializer.Serialize(built.RateComparisons, Json);
        revision.ApprovalSnapshotJson = JsonSerializer.Serialize(new { costStatus = "Unavailable", belowMinimumRate = built.BelowMinimum }, Json);
        db.Add(revision);
        db.AddRange(built.Lines.Select(x => CopyLine(x.Line, revision)));
        db.AddRange(built.Charges.Select(x => CopyCharge(x, revision)));
        await db.SaveChangesAsync(ct);
        revision.ApprovalRequestId = await SubmitApprovalIfRequired(revision, built, actor, approvals, ct);
        revision.Status = revision.ApprovalRequestId is null ? "Approved" : "Pending approval";
        AddActivity(db, original, actor.UserId, "Superseded", new { revisionId = revision.QuotationId, revision.RevisionNumber });
        AddActivity(db, revision, actor.UserId, "RevisionCreated",
            new { previousQuotationId = original.QuotationId, revision.RevisionNumber, revision.Total });
        AddAudit(db, revision, actor, "Quotation revision created",
            new { previousQuotationId = original.QuotationId, revision.RevisionNumber, revision.Total });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/m05/quotations/{revision.QuotationId}",
            new { revision.QuotationId, revision.Number, revision.RevisionNumber, revision.Status, revision.Total });
    }

    private static async Task<IResult> Clone(
        Guid quotationId, QuoteWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, M03MasterService masters,
        OnboardingStore onboarding, IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.create", ct);
        var source = await db.Set<M05Quotation>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct);
        if (source is null || !await CanView(authorization, actor.UserId, source, ct))
            return Results.NotFound();
        if (request.CustomerPartyId != source.CustomerPartyId ||
            request.SiteId != source.SiteId || request.ContactId != source.ContactId)
            throw new ValidationException("A clone must preserve its source customer, site and contact.");
        var cloneRequest = request with { IdempotencyKey = request.IdempotencyKey.Trim() };
        var result = await Create(cloneRequest, authorization, db, tenant, masters, onboarding, approvals, ct);
        if (result is IStatusCodeHttpResult { StatusCode: >= 400 })
            return result;
        return result;
    }

    private static async Task<IResult> Issue(
        Guid quotationId, TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.issue", ct);
        var quote = await db.Set<M05Quotation>().SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct);
        if (quote is null || !await CanView(authorization, actor.UserId, quote, ct))
            return Results.NotFound();
        if (!await CanWrite(authorization, actor.UserId, "m05.quotations.issue",
                quote.BranchId, quote.SalespersonUserId, ct))
            throw new UnauthorizedAccessException("The quotation is outside your issue scope.");
        if (quote.Status == "Sent")
            return Results.Ok(new { quote.QuotationId, quote.Number, quote.Status, duplicate = true });
        if (quote.ApprovalRequestId is { } approvalId)
        {
            var approval = await db.Set<M02ApprovalRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestId == approvalId, ct);
            if (approval?.Status != "Approved")
                return Results.Conflict(new { detail = "The M02 approval must be completed before issue." });
            quote.Status = "Approved";
        }
        if (quote.Status != "Approved")
            return Results.Conflict(new { detail = "Only an approved quotation can be issued." });
        if (quote.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            quote.Status = "Expired";
            quote.UpdatedAt = DateTimeOffset.UtcNow;
            quote.Version++;
            AddActivity(db, quote, actor.UserId, "Expired", new { quote.ValidUntil });
            AddAudit(db, quote, actor, "Quotation expired", new { quote.ValidUntil });
            await db.SaveChangesAsync(ct);
            return Results.Conflict(new { detail = "The quotation validity date has passed. It was marked expired; create a revision to revalidate." });
        }

        var now = DateTimeOffset.UtcNow;
        var year = now.Year;
        var sequence = await db.Database.SqlQuery<long>($"""
            INSERT INTO m04_number_counter ("TenantId", "DocumentCode", "CalendarYear", "NextNumber")
            VALUES ({tenant.TenantId}, {"Quotation"}, {year}, 2)
            ON CONFLICT ("TenantId", "DocumentCode", "CalendarYear")
            DO UPDATE SET "NextNumber" = m04_number_counter."NextNumber" + 1
            RETURNING "NextNumber" - 1 AS "Value"
            """).SingleAsync(ct);
        quote.Number ??= $"QT-{year}-{sequence:D6}";
        quote.Status = "Sent";
        quote.IssuedAt = now;
        quote.UpdatedAt = now;
        quote.Version++;
        var token = NewToken();
        db.Add(new M05QuotationLink
        {
            TenantId = tenant.TenantId, LinkId = Guid.NewGuid(), QuotationId = quote.QuotationId,
            TokenHash = HashToken(token), CreatedAt = now, ExpiresAt = now.AddDays(30)
        });
        AddActivity(db, quote, actor.UserId, "Issued", new { quote.Number, quote.ValidUntil });
        AddAudit(db, quote, actor, "Quotation issued", new { quote.Number, quote.Total });
        if (quote.SourceEnquiryId is { } enquiryId)
        {
            await authorization.Require("M04", "m04.enquiries.edit", ct);
            var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct)
                ?? throw new ValidationException("The source enquiry is no longer available.");
            if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                    enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
                throw new UnauthorizedAccessException("You cannot update the source enquiry.");
            if (enquiry.Status is not ("Won" or "Lost"))
            {
                enquiry.Status = "Quotation Sent";
                enquiry.UpdatedAt = now;
                enquiry.Version++;
                db.Add(new M04EnquiryActivity
                {
                    TenantId = tenant.TenantId, ActivityId = Guid.NewGuid(), EnquiryId = enquiryId,
                    ActorUserId = actor.UserId, Kind = "QuotationIssued", Outcome = quote.Number ?? "",
                    Notes = $"Quotation {quote.Number} issued.", At = now
                });
                db.Add(new M02AuditEvent
                {
                    TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ActorUserId = actor.UserId,
                    ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString(),
                    Action = "Quotation issued", EntityType = "M03Party", EntityId = quote.CustomerPartyId,
                    Before = "{}", After = JsonSerializer.Serialize(new
                    {
                        quote.QuotationId, quote.Number, sourceEnquiryId = enquiryId
                    }, Json), At = now
                });
            }
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            quote.QuotationId, quote.Number, quote.Status,
            customerLink = $"/public/quotes/{token}",
            deliveryStatus = "link-created-not-delivered",
            quote.Version
        });
    }

    private static async Task<IResult> Cancel(
        Guid quotationId, TenantAuthorizationService authorization, BerynxDbContext db,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.cancel", ct);
        var quote = await db.Set<M05Quotation>().SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct);
        if (quote is null || !await CanView(authorization, actor.UserId, quote, ct)) return Results.NotFound();
        if (!await CanWrite(authorization, actor.UserId, "m05.quotations.cancel",
                quote.BranchId, quote.SalespersonUserId, ct))
            throw new UnauthorizedAccessException("The quotation is outside your cancel scope.");
        if (quote.Status is "Accepted" or "Rejected" or "Cancelled" or "Revised")
            return Results.Conflict(new { detail = "This quotation cannot be cancelled in its current state." });
        quote.Status = "Cancelled";
        quote.CancelledAt = DateTimeOffset.UtcNow;
        quote.UpdatedAt = DateTimeOffset.UtcNow;
        quote.Version++;
        var links = await db.Set<M05QuotationLink>().Where(x => x.QuotationId == quotationId && x.RevokedAt == null).ToListAsync(ct);
        foreach (var link in links) link.RevokedAt = quote.CancelledAt;
        AddActivity(db, quote, actor.UserId, "Cancelled", new { quote.Version });
        AddAudit(db, quote, actor, "Quotation cancelled", new { quote.Version });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { quote.QuotationId, quote.Status, quote.Version });
    }

    private static async Task<IResult> CustomerHistory(
        Guid customerId, int page, int pageSize, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.view", ct);
        var query = db.Set<M05Quotation>().AsNoTracking().Where(x =>
            x.CustomerPartyId == customerId && x.IsActiveRevision);
        var scopes = await authorization.Scopes(user.UserId, "m05.quotations.view", ct);
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x =>
                (own && x.SalespersonUserId == user.UserId) ||
                (x.BranchId != null && branches.Contains(x.BranchId.Value)));
        }
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
        return Results.Ok(await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize)
            .Take(pageSize).Select(x => new
            {
                x.QuotationId, x.Number, x.RevisionNumber, x.Status, x.Total, x.ValidUntil, x.CreatedAt
            }).ToListAsync(ct));
    }

    private static async Task<IResult> PrintView(
        Guid quotationId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M05", "m05.quotations.view", ct);
        var quote = await db.Set<M05Quotation>().AsNoTracking().SingleOrDefaultAsync(x => x.QuotationId == quotationId, ct);
        if (quote is null || !await CanView(authorization, actor.UserId, quote, ct)) return Results.NotFound();
        var lines = await db.Set<M05QuotationLine>().AsNoTracking().Where(x => x.QuotationId == quotationId).ToListAsync(ct);
        var charges = await db.Set<M05QuotationCharge>().AsNoTracking().Where(x => x.QuotationId == quotationId).ToListAsync(ct);
        var e = HtmlEncoder.Default;
        var content = new StringBuilder()
            .Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>")
            .Append(e.Encode($"Quotation {quote.Number ?? "Draft"}")).Append("</title>")
            .Append("<style>body{font:14px Arial;color:#242424;margin:32px}h1{color:#8d4a32}table{width:100%;border-collapse:collapse;margin:20px 0}th,td{border-bottom:1px solid #ddd;padding:8px;text-align:left}.total{text-align:right;font-size:18px;font-weight:bold}@media print{button{display:none}}</style></head><body>")
            .Append("<button onclick=\"window.print()\">Print / Save as PDF</button><h1>")
            .Append(e.Encode(quote.CompanyLegalNameSnapshot)).Append("</h1><h2>Quotation ")
            .Append(e.Encode(quote.Number ?? "Draft")).Append("</h2><p>Customer: ")
            .Append(e.Encode(quote.CustomerNameSnapshot)).Append("<br>Address: ")
            .Append(e.Encode(quote.CustomerAddressSnapshot)).Append("</p><table><thead><tr><th>Item</th><th>Qty</th><th>Rate</th><th>Discount</th><th>Tax</th><th>Total</th></tr></thead><tbody>");
        foreach (var line in lines)
            content.Append("<tr><td>").Append(e.Encode(line.Description)).Append("</td><td>")
                .Append(line.Quantity.ToString("0.###")).Append(' ').Append(e.Encode(line.UnitCode)).Append("</td><td>")
                .Append(line.Rate.ToString("0.0000")).Append("</td><td>").Append(line.DiscountPercent.ToString("0.####"))
                .Append("%</td><td>CGST ").Append(line.CgstAmount.ToString("0.00"))
                .Append(" / SGST ").Append(line.SgstAmount.ToString("0.00"))
                .Append(" / IGST ").Append(line.IgstAmount.ToString("0.00"))
                .Append("</td><td>").Append(line.Total.ToString("0.00")).Append("</td></tr>");
        foreach (var charge in charges)
            content.Append("<tr><td>").Append(e.Encode(charge.Description)).Append(" (charge)</td><td>1</td><td>")
                .Append(charge.Amount.ToString("0.00")).Append("</td><td>—</td><td>CGST ")
                .Append(charge.CgstAmount.ToString("0.00")).Append(" / SGST ")
                .Append(charge.SgstAmount.ToString("0.00")).Append(" / IGST ")
                .Append(charge.IgstAmount.ToString("0.00")).Append("</td><td>")
                .Append(charge.Total.ToString("0.00")).Append("</td></tr>");
        AddActivity(db, quote, actor.UserId, "Printed", new { quote.Number, quote.RevisionNumber });
        AddAudit(db, quote, actor, "Quotation printed", new { quote.Number, quote.RevisionNumber });
        await db.SaveChangesAsync(ct);
        content.Append("</tbody></table><p>Place of supply: ").Append(e.Encode(quote.PlaceOfSupplyStateCode))
            .Append("<br>Seller GSTIN: ").Append(e.Encode(quote.SellerGstinSnapshot))
            .Append("<br>Customer GSTIN: ").Append(e.Encode(quote.CustomerGstinSnapshot ?? "Not provided"))
            .Append("<br>Site: ").Append(e.Encode(quote.SiteAddressSnapshot))
            .Append("</p><p>Delivery: ").Append(e.Encode(quote.DeliveryTerms))
            .Append("</p><p>Terms: ").Append(e.Encode(quote.TermsAndConditions))
            .Append("</p><div class=\"total\">Total: ").Append(quote.Total.ToString("0.00"))
            .Append("</div><p>Valid until: ").Append(quote.ValidUntil.ToString("yyyy-MM-dd"))
            .Append("</p></body></html>");
        return Results.Content(content.ToString(), "text/html; charset=utf-8", Encoding.UTF8);
    }

    private static async Task<IResult> PublicView(
        string token, BerynxDbContext db, TenantEntitlementService entitlements, CancellationToken ct)
    {
        var hash = HashToken(token);
        var link = await db.Set<M05QuotationLink>().IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAt == null, ct);
        if (link is null || link.ExpiresAt <= DateTimeOffset.UtcNow) return Results.NotFound();
        await entitlements.RequirePublicModule(link.TenantId, "M05", cancellationToken: ct);
        var quote = await db.Set<M05Quotation>().IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == link.TenantId && x.QuotationId == link.QuotationId, ct);
        if (quote is null || quote.Status is not ("Sent" or "Accepted" or "Rejected"))
            return Results.NotFound();
        var lines = await db.Set<M05QuotationLine>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.TenantId == link.TenantId && x.QuotationId == quote.QuotationId)
            .Select(x => new { x.Description, x.Quantity, x.UnitCode, x.Rate, x.DiscountPercent, x.GstPercent, x.CgstAmount, x.SgstAmount, x.IgstAmount, x.TaxAmount, x.Total })
            .ToListAsync(ct);
        var charges = await db.Set<M05QuotationCharge>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.TenantId == link.TenantId && x.QuotationId == quote.QuotationId)
            .Select(x => new { x.Description, x.Amount, x.TaxRatePercent, x.CgstAmount, x.SgstAmount, x.IgstAmount, x.TaxAmount, x.Total })
            .ToListAsync(ct);
        return Results.Ok(new
        {
            quote.Number, quote.RevisionNumber, quote.CompanyLegalNameSnapshot,
            quote.CustomerNameSnapshot, quote.CustomerAddressSnapshot, quote.SiteAddressSnapshot,
            quote.PriceBasis, quote.ValidUntil, quote.DeliveryTerms, quote.TermsAndConditions,
            quote.Subtotal, quote.DiscountTotal, quote.ChargesTotal, quote.TaxTotal, quote.Total,
            Status = quote.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow) ? "Expired" : quote.Status,
            lines, charges
        });
    }

    private static async Task<IResult> SendOtp(
        string token, PublicOtpRequest request, BerynxDbContext db,
        TenantEntitlementService entitlements, IM05CustomerOtpProvider provider, CancellationToken ct)
    {
        if (!provider.IsConfigured)
            return Results.Problem("Customer OTP verification is not configured; no code was sent.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        var link = await PublicLink(token, db, ct);
        if (link is null) return Results.NotFound();
        await entitlements.RequirePublicModule(link.TenantId, "M05", write: true, cancellationToken: ct);
        var sent = await provider.Send(link.TenantId, link.QuotationId, request.Mobile.Trim(), ct);
        return sent ? Results.Accepted() : Results.Problem("OTP delivery was not confirmed.", statusCode: 502);
    }

    private static async Task<IResult> PublicDecision(
        string token, PublicDecisionRequest request, BerynxDbContext db,
        DbContextOptions<BerynxDbContext> dbOptions, TenantEntitlementService entitlements,
        IM05CustomerOtpProvider provider, CancellationToken ct)
    {
        if (!provider.IsConfigured)
            return Results.Problem("Customer OTP verification is not configured; no decision was recorded.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        var link = await PublicLink(token, db, ct);
        if (link is null) return Results.NotFound();
        await entitlements.RequirePublicModule(link.TenantId, "M05", write: true, cancellationToken: ct);
        if (!await provider.Verify(link.TenantId, link.QuotationId, request.Mobile.Trim(), request.Otp, ct))
            return Results.Unauthorized();
        await using var tenantDb = new BerynxDbContext(dbOptions, new PublicTenantScope(link.TenantId));
        var quote = await tenantDb.Set<M05Quotation>()
            .SingleOrDefaultAsync(x => x.QuotationId == link.QuotationId, ct);
        if (quote?.Status == request.Decision)
            return Results.Ok(new { quote.Number, quote.Status, duplicate = true });
        if (quote is null || quote.Status != "Sent" ||
            quote.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow))
            return Results.Conflict(new { detail = "This quotation is no longer open for a customer decision." });
        if (request.Decision is not ("Accepted" or "Rejected"))
            throw new ValidationException("Choose Accepted or Rejected.");
        if (request.Decision == "Rejected" && string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new ValidationException("A rejection reason is required.");
        var now = DateTimeOffset.UtcNow;
        quote.Status = request.Decision;
        quote.AcceptedAt = request.Decision == "Accepted" ? now : null;
        quote.RejectedAt = request.Decision == "Rejected" ? now : null;
        quote.Version++;
        quote.UpdatedAt = now;
        tenantDb.Add(new M05QuotationActivity
        {
            TenantId = quote.TenantId, ActivityId = Guid.NewGuid(), QuotationId = quote.QuotationId,
            Action = request.Decision, DetailJson = JsonSerializer.Serialize(new
            {
                mobile = request.Mobile.Trim(), reason = request.RejectionReason?.Trim(),
                verifiedByOtp = true
            }, Json), At = now
        });
        if (quote.SourceEnquiryId is { } enquiryId)
        {
            var enquiry = await tenantDb.Set<M04Enquiry>()
                .SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
            if (enquiry is not null && request.Decision == "Accepted" &&
                enquiry.Status == "Quotation Sent")
            {
                enquiry.Status = "Negotiation";
                enquiry.Version++;
                enquiry.UpdatedAt = now;
            }
            tenantDb.Add(new M04EnquiryActivity
            {
                TenantId = quote.TenantId, ActivityId = Guid.NewGuid(), EnquiryId = enquiryId,
                ActorUserId = null, Kind = $"Quotation{request.Decision}",
                Outcome = quote.Number ?? "", Notes = request.RejectionReason?.Trim() ?? "",
                At = now
            });
        }
        tenantDb.Add(new M02AuditEvent
        {
            TenantId = quote.TenantId, EventId = Guid.NewGuid(), ActorUserId = null,
            ActorSubject = "VerifiedCustomerOTP", Action = $"Customer {request.Decision} quotation",
            EntityType = "M05Quotation", EntityId = quote.QuotationId, Before = "{}",
            After = JsonSerializer.Serialize(new
            {
                quote.Number, request.Decision, mobile = request.Mobile.Trim(),
                reason = request.RejectionReason?.Trim(), verifiedAt = now
            }, Json), At = now
        });
        await tenantDb.SaveChangesAsync(ct);
        return Results.Ok(new { quote.Number, quote.Status, recordedAt = now });
    }

    private static async Task<M05QuotationLink?> PublicLink(string token, BerynxDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128) return null;
        return await db.Set<M05QuotationLink>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(
            x => x.TokenHash == HashToken(token) && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
    }

    private static async Task<BuiltQuote> Build(
        Guid tenantId, M02User actor, QuoteWrite request, BerynxDbContext db,
        M03MasterService masters, OnboardingStore onboarding,
        TenantAuthorizationService authorization, CancellationToken ct)
    {
        Validate(request);
        var customer = await db.Set<M03Party>().AsNoTracking().SingleOrDefaultAsync(
            x => x.PartyId == request.CustomerPartyId && x.IsActive, ct)
            ?? throw new ValidationException("Choose an active M03 customer.");
        if (!await db.Set<M03PartyRole>().AnyAsync(x => x.PartyId == customer.PartyId && x.RoleCode == "Customer", ct))
            throw new ValidationException("The selected M03 party does not have the Customer role.");
        var site = request.SiteId is { } siteId
            ? await db.Set<M03Site>().AsNoTracking().SingleOrDefaultAsync(
                x => x.SiteId == siteId && x.CustomerPartyId == customer.PartyId && x.Status == "Active", ct)
              ?? throw new ValidationException("Choose an active site belonging to the selected customer.")
            : null;
        var contact = request.ContactId is { } contactId
            ? await db.Set<M03Contact>().AsNoTracking().SingleOrDefaultAsync(
                x => x.ContactId == contactId && x.PartyId == customer.PartyId && x.IsActive, ct)
              ?? throw new ValidationException("Choose an active contact belonging to the selected customer.")
            : null;
        if (request.PaymentTermId is { } termId &&
            !await db.Set<M03PaymentTerm>().AnyAsync(x => x.PaymentTermId == termId && x.IsActive, ct))
            throw new ValidationException("Choose an active M03 payment term.");
        if (site?.BranchId is { } siteBranch && request.BranchId is { } requestedBranch && siteBranch != requestedBranch)
            throw new ValidationException("The selected site belongs to a different branch.");
        var branchId = site?.BranchId ?? request.BranchId;
        var setupTenant = await db.Set<Tenant>().AsNoTracking().SingleAsync(x => x.TenantId == tenantId, ct);
        var setup = OnboardingStore.Decode(setupTenant);
        var branch = branchId is { } branchKey ? setup.Branches.SingleOrDefault(x => x.Id == branchKey) : null;
        if (branchId is not null && branch is null)
            throw new ValidationException("Choose a branch configured for this tenant.");
        var sellerGstin = branch?.Gstin ?? setup.Registrations.FirstOrDefault() ?? "";
        var place = request.PlaceOfSupplyStateCode.Trim();
        if (place.Length != 2 || !place.All(char.IsDigit))
            throw new ValidationException("Place of supply must be a two-digit GST state code.");
        var effectiveOn = DateOnly.FromDateTime(DateTime.UtcNow);
        var lines = new List<BuiltLine>();
        foreach (var input in request.Lines)
        {
            var product = await db.Set<M03Product>().AsNoTracking().SingleOrDefaultAsync(
                x => x.ProductId == input.ProductId && x.IsActive, ct)
                ?? throw new ValidationException("Choose an active M03 product.");
            if (!string.Equals(product.BaseUnitCode, input.UnitCode.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Quotation UOM must match the product's configured base unit.");
            if (product.Category == "RMC" && string.IsNullOrWhiteSpace(input.RmcGrade))
                throw new ValidationException("RMC quotations require an approved grade rate-card selection.");
            if (product.Category != "RMC" && (input.RmcGrade is not null || input.RmcSlumpBand is not null ||
                input.RmcAdmixtureCode is not null || input.RmcPumpType is not null))
                throw new ValidationException("RMC rate-card fields are valid only for RMC products.");
            decimal listRate;
            Guid? priceListId = null;
            string rateSource;
            if (!string.IsNullOrWhiteSpace(input.RmcGrade))
            {
                if (string.IsNullOrWhiteSpace(input.RmcSlumpBand) ||
                    string.IsNullOrWhiteSpace(input.RmcAdmixtureCode) ||
                    string.IsNullOrWhiteSpace(input.RmcPumpType))
                    throw new ValidationException("RMC grade, slump band, admixture and pump type are required together.");
                var rmc = await masters.ResolveRmc(input.ProductId, input.RmcGrade,
                    input.RmcSlumpBand, input.RmcAdmixtureCode, input.RmcPumpType,
                    effectiveOn, ct, request.PriceBasis);
                listRate = rmc.Rate;
                rateSource = "ApprovedRmcRateCard";
            }
            else
            {
                var price = await masters.ResolvePrice(customer.PartyId, input.ProductId, effectiveOn,
                    ct, input.Rate, request.PriceBasis);
                listRate = price.Rate;
                priceListId = price.PriceListId;
                rateSource = price.Precedence;
            }
            var rate = input.Rate ?? listRate;
            if (input.Rate is not null && rate != listRate)
                rateSource = "ManualOverride";
            if (rate < 0 || decimal.Round(rate, 4) != rate)
                throw new ValidationException("Rates must be non-negative with up to four decimals.");
            var taxRate = await masters.ResolveTaxRate(input.ProductId, customer.CustomerCategory,
                effectiveOn, sellerGstin, customer.Gstin, place, ct);
            var intraState = sellerGstin.Length >= 2 && sellerGstin[..2] == place;
            var amounts = M05QuotationCalculator.CalculateLine(input.Quantity, rate,
                input.DiscountPercent, taxRate.RatePercent, intraState);
            var taxSnapshot = Berynx.Application.Masters.M03TaxSnapshot.Create(
                input.ProductId, taxRate.HsnCode, taxRate.SellerGstin, taxRate.BuyerGstin,
                taxRate.PlaceOfSupplyStateCode ?? place, effectiveOn, taxRate.Source,
                amounts.TaxableAmount, taxRate.RatePercent,
                intraState ? taxRate.RatePercent / 2m : 0m,
                intraState ? taxRate.RatePercent / 2m : 0m,
                intraState ? 0m : taxRate.RatePercent, taxRate.SourceRateId);
            lines.Add(new BuiltLine(new M05QuotationLine
            {
                TenantId = tenantId, LineId = Guid.NewGuid(), ProductId = product.ProductId,
                Description = product.Name, Quantity = input.Quantity, UnitCode = product.BaseUnitCode,
                Rate = rate, DiscountPercent = input.DiscountPercent, GstPercent = taxRate.RatePercent,
                TaxableAmount = amounts.TaxableAmount, CgstAmount = amounts.CgstAmount,
                SgstAmount = amounts.SgstAmount, IgstAmount = amounts.IgstAmount,
                TaxAmount = amounts.TaxAmount, Total = amounts.Total, PriceListId = priceListId,
                RateSource = rateSource, RmcGrade = input.RmcGrade, RmcSlumpBand = input.RmcSlumpBand,
                RmcAdmixtureCode = input.RmcAdmixtureCode, RmcPumpType = input.RmcPumpType,
                TaxSnapshotJson = JsonSerializer.Serialize(taxSnapshot, Json)
            }, false, input.DiscountPercent));
            if (!string.IsNullOrWhiteSpace(input.RmcGrade))
            {
                var rmcRate = await db.Set<M03RmcRateCard>().AsNoTracking().SingleAsync(x =>
                    x.ProductId == input.ProductId && x.Grade == input.RmcGrade &&
                    x.SlumpBand == input.RmcSlumpBand && x.AdmixtureCode == input.RmcAdmixtureCode &&
                    x.PumpType == input.RmcPumpType && x.PriceBasis == request.PriceBasis &&
                    x.IsActive && x.IsApproved &&
                    x.EffectiveFrom <= effectiveOn && (x.EffectiveTo == null || x.EffectiveTo >= effectiveOn), ct);
                lines[^1] = lines[^1] with { BelowMinimum = input.Rate is not null && input.Rate < rmcRate.Rate };
            }
            else
            {
                var priceCheck = await masters.ResolvePrice(customer.PartyId, input.ProductId,
                    effectiveOn, ct, rate, request.PriceBasis);
                lines[^1] = lines[^1] with { BelowMinimum = priceCheck.ProposedRateBelowMinimum };
            }
        }

        var charges = new List<M05QuotationCharge>();
        foreach (var input in request.Charges)
        {
            if (input.Amount < 0 || decimal.Round(input.Amount, 2) != input.Amount ||
                string.IsNullOrWhiteSpace(input.Description) || input.Description.Length > 200)
                throw new ValidationException("Charges require a description and non-negative two-decimal amount.");
            var type = input.ChargeTypeId is { } chargeTypeId
                ? await db.Set<M03ChargeType>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.ChargeTypeId == chargeTypeId && x.IsActive, ct)
                  ?? throw new ValidationException("Choose an active M03 charge type.")
                : null;
            var taxRate = type?.DefaultTaxRatePercent ?? 0m;
            var intraState = sellerGstin.Length >= 2 && sellerGstin[..2] == place;
            var amounts = M05QuotationCalculator.CalculateLine(1m, input.Amount, 0m, taxRate, intraState);
            charges.Add(new M05QuotationCharge
            {
                TenantId = tenantId, ChargeId = Guid.NewGuid(), ChargeTypeId = type?.ChargeTypeId,
                Description = input.Description.Trim(), Amount = input.Amount, TaxRatePercent = taxRate,
                CgstAmount = amounts.CgstAmount, SgstAmount = amounts.SgstAmount,
                IgstAmount = amounts.IgstAmount, TaxAmount = amounts.TaxAmount, Total = amounts.Total,
                TaxTreatmentSnapshotJson = JsonSerializer.Serialize(new
                {
                    source = "M03ChargeType.DefaultTaxRatePercent", hsnCode = type?.HsnCode,
                    effectiveOn, placeOfSupply = place, sellerGstin
                }, Json)
            });
        }
        var subtotal = lines.Sum(x => x.Line.GrossAmount());
        var discountTotal = lines.Sum(x => x.Line.DiscountAmount());
        var chargesTotal = charges.Sum(x => x.Amount);
        var taxTotal = lines.Sum(x => x.Line.TaxAmount) + charges.Sum(x => x.TaxAmount);
        var total = lines.Sum(x => x.Line.Total) + chargesTotal + charges.Sum(x => x.TaxAmount);
        var rateComparisons = lines.Select(x => new
        {
            productId = x.Line.ProductId, x.Line.Rate, x.Line.RateSource,
            belowMinimum = x.BelowMinimum, costStatus = "Unavailable"
        }).ToArray();
        return new(customer, site, contact, branchId, place, sellerGstin,
            setup.Company?.LegalName ?? setupTenant.Name, lines, charges, subtotal,
            discountTotal, chargesTotal, taxTotal, total, lines.Any(x => x.BelowMinimum), rateComparisons);
    }

    private static M05Quotation NewQuotation(
        QuoteWrite request, BuiltQuote built, Guid tenantId, Guid actorUserId, DateTimeOffset now)
    {
        var contact = built.Contact;
        var siteAddress = built.Site is null ? "" :
            string.Join(", ", new[] { built.Site.Address, built.Site.City, built.Site.StateCode }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new M05Quotation
        {
            TenantId = tenantId, QuotationId = Guid.NewGuid(), SeriesId = Guid.NewGuid(),
            SourceEnquiryId = null, CustomerPartyId = built.Customer.PartyId, SiteId = built.Site?.SiteId,
            ContactId = contact?.ContactId, BranchId = built.BranchId, SalespersonUserId = actorUserId,
            Status = "Draft", PriceBasis = request.PriceBasis, ValidUntil = request.ValidUntil,
            PaymentTermId = request.PaymentTermId, PlaceOfSupplyStateCode = built.PlaceOfSupply,
            CompanyLegalNameSnapshot = built.CompanyName, SellerGstinSnapshot = built.SellerGstin,
            CustomerNameSnapshot = built.Customer.TradeName ?? built.Customer.LegalName,
            CustomerGstinSnapshot = built.Customer.Gstin,
            CustomerAddressSnapshot = built.Customer.BillingAddress ?? "",
            SiteAddressSnapshot = siteAddress, DeliveryTerms = request.DeliveryTerms.Trim(),
            TermsAndConditions = request.TermsAndConditions.Trim(), InternalNotes = request.InternalNotes.Trim(),
            Subtotal = built.Subtotal, DiscountTotal = built.DiscountTotal, ChargesTotal = built.ChargesTotal,
            TaxTotal = built.TaxTotal, Total = built.Total, CostStatus = "Unavailable",
            CreditStatus = built.Customer.CreditStatus, CreditSnapshotJson = JsonSerializer.Serialize(new
            {
                built.Customer.CreditStatus, built.Customer.CreditLimit, built.Customer.CreditDays,
                creditPolicy = built.Customer.CreditPolicy
            }, Json), CreatedAt = now, UpdatedAt = now
        };
    }

    private static void ApplyQuote(M05Quotation quote, QuoteWrite request, BuiltQuote built)
    {
        quote.CustomerPartyId = built.Customer.PartyId;
        quote.SiteId = built.Site?.SiteId;
        quote.ContactId = built.Contact?.ContactId;
        quote.BranchId = built.BranchId;
        quote.PriceBasis = request.PriceBasis;
        quote.ValidUntil = request.ValidUntil;
        quote.PaymentTermId = request.PaymentTermId;
        quote.PlaceOfSupplyStateCode = built.PlaceOfSupply;
        quote.CompanyLegalNameSnapshot = built.CompanyName;
        quote.SellerGstinSnapshot = built.SellerGstin;
        quote.CustomerNameSnapshot = built.Customer.TradeName ?? built.Customer.LegalName;
        quote.CustomerGstinSnapshot = built.Customer.Gstin;
        quote.CustomerAddressSnapshot = built.Customer.BillingAddress ?? "";
        quote.SiteAddressSnapshot = built.Site is null ? "" :
            string.Join(", ", new[] { built.Site.Address, built.Site.City, built.Site.StateCode }.Where(x => !string.IsNullOrWhiteSpace(x)));
        quote.DeliveryTerms = request.DeliveryTerms.Trim();
        quote.TermsAndConditions = request.TermsAndConditions.Trim();
        quote.InternalNotes = request.InternalNotes.Trim();
        quote.Subtotal = built.Subtotal;
        quote.DiscountTotal = built.DiscountTotal;
        quote.ChargesTotal = built.ChargesTotal;
        quote.TaxTotal = built.TaxTotal;
        quote.Total = built.Total;
    }

    private static M05QuotationLine CopyLine(M05QuotationLine source, M05Quotation quote)
    {
        source.TenantId = quote.TenantId;
        source.LineId = Guid.NewGuid();
        source.QuotationId = quote.QuotationId;
        return source;
    }

    private static M05QuotationCharge CopyCharge(M05QuotationCharge source, M05Quotation quote)
    {
        source.TenantId = quote.TenantId;
        source.ChargeId = Guid.NewGuid();
        source.QuotationId = quote.QuotationId;
        return source;
    }

    private static async Task<Guid?> SubmitApprovalIfRequired(
        M05Quotation quote, BuiltQuote built, M02User actor, IM02ApprovalEngine approvals, CancellationToken ct)
    {
        if (!built.BelowMinimum && built.Lines.All(x => x.DiscountPercent == 0m))
            return null;
        var snapshot = JsonSerializer.SerializeToElement(new
        {
            quote.QuotationId, quote.Total, quote.DiscountTotal, quote.PriceBasis,
            lines = built.Lines.Select(x => new
            {
                x.Line.ProductId, x.Line.Quantity, x.Line.UnitCode, x.Line.Rate,
                x.Line.DiscountPercent, x.Line.GstPercent, x.Line.Total, x.BelowMinimum
            }),
            costStatus = "Unavailable",
            belowMinimumRate = built.BelowMinimum
        }, Json);
        var discount = built.Lines.Count == 0 ? 0m : built.Lines.Max(x => x.DiscountPercent);
        var requestId = await approvals.Submit(new ApprovalSubmission(
            "M05Quotation", quote.QuotationId, quote.BranchId, null, actor.UserId,
            quote.Total, discount, null, built.Customer.CreditStatus, snapshot), ct);
        if (built.BelowMinimum && requestId is null)
            throw new ValidationException("A rate is below the M03 minimum. Configure a matching M02 quotation approval rule before saving it.");
        return requestId;
    }

    private static async Task<bool> CanView(
        TenantAuthorizationService authorization, Guid userId, M05Quotation quote, CancellationToken ct) =>
        await authorization.CanAccess("M05", userId, "m05.quotations.view",
            quote.BranchId, null, quote.SalespersonUserId, ct, write: false);

    private static Task<bool> CanWrite(
        TenantAuthorizationService authorization, Guid userId, string permission,
        Guid? branchId, Guid? ownerId, CancellationToken ct) =>
        authorization.CanAccess("M05", userId, permission, branchId, null, ownerId, ct, write: true);

    private static void Validate(QuoteWrite request)
    {
        if (request.CustomerPartyId == Guid.Empty || request.Lines is null || request.Lines.Length is 0 or > 100)
            throw new ValidationException("Choose a customer and add between one and 100 product lines.");
        if (request.Charges is null || request.Charges.Length > 100)
            throw new ValidationException("Quotation charges are invalid.");
        if (request.PriceBasis is not ("ExPlant" or "Delivered" or "Site"))
            throw new ValidationException("Choose ExPlant, Delivered or Site price basis.");
        if (request.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow) ||
            request.ValidUntil > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)))
            throw new ValidationException("Validity date must be between today and one year from today.");
        if (string.IsNullOrWhiteSpace(request.DeliveryTerms) || request.DeliveryTerms.Length > 2000 ||
            string.IsNullOrWhiteSpace(request.TermsAndConditions) || request.TermsAndConditions.Length > 8000 ||
            request.InternalNotes?.Length > 4000)
            throw new ValidationException("Delivery terms, quotation terms and internal notes are invalid.");
        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0 || decimal.Round(line.Quantity, 3) != line.Quantity ||
                line.DiscountPercent is < 0 or > 100 || decimal.Round(line.DiscountPercent, 4) != line.DiscountPercent ||
                string.IsNullOrWhiteSpace(line.UnitCode) || line.UnitCode.Length > 16)
                throw new ValidationException("Line quantity, UOM or discount is invalid.");
        }
    }

    private static void AddActivity(
        BerynxDbContext db, M05Quotation quote, Guid? actorUserId, string action, object detail) =>
        db.Add(new M05QuotationActivity
        {
            TenantId = quote.TenantId, ActivityId = Guid.NewGuid(), QuotationId = quote.QuotationId,
            ActorUserId = actorUserId, Action = action,
            DetailJson = JsonSerializer.Serialize(detail, Json), At = DateTimeOffset.UtcNow
        });

    private static void AddAudit(
        BerynxDbContext db, M05Quotation quote, M02User actor, string action, object after) =>
        AddAudit(db, quote.TenantId, actor, action, "M05Quotation", quote.QuotationId, null, after);

    private static void AddAudit(
        BerynxDbContext db, Guid tenantId, M02User actor, string action,
        string entityType, Guid? entityId, object? before, object? after) =>
        db.Add(new M02AuditEvent
        {
            TenantId = tenantId, EventId = Guid.NewGuid(), ActorUserId = actor.UserId,
            ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString(), Action = action,
            EntityType = entityType, EntityId = entityId,
            Before = JsonSerializer.Serialize(before ?? new { }, Json),
            After = JsonSerializer.Serialize(after ?? new { }, Json), At = DateTimeOffset.UtcNow
        });

    private static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static decimal GrossAmount(this M05QuotationLine line) => line.TaxableAmount + line.DiscountAmount();
    private static decimal DiscountAmount(this M05QuotationLine line) =>
        decimal.Round(line.Quantity * line.Rate * line.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
}
