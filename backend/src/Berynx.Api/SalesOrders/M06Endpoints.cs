using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Berynx.Api.Masters;
using Berynx.Api.Onboarding;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Masters;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Crm;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Quotations;
using Berynx.Domain.SalesOrders;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.SalesOrders;

public static class M06Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public sealed record DirectLineWrite(Guid ProductId, decimal Quantity, string UnitCode,
        decimal? Rate, decimal DiscountPercent, string? RmcGrade = null, string? RmcSlumpBand = null,
        string? RmcAdmixtureCode = null, string? RmcPumpType = null);
    public sealed record ChargeWrite(Guid? ChargeTypeId, string Description, decimal Amount);
    public sealed record DirectOrderWrite(Guid CustomerPartyId, Guid? SiteId, Guid? BranchId,
        DateOnly OrderDate, string OrderType, string PriceBasis, DateOnly? RateValidityUntil,
        Guid? PaymentTermId, string PaymentTerms, string DeliveryTerms, string CustomerPoNumber,
        DateOnly? CustomerPoDate, string? CustomerPoAttachmentReference, string Notes,
        string PlaceOfSupplyStateCode, DirectLineWrite[] Lines, ChargeWrite[] Charges,
        string IdempotencyKey);
    public sealed record QuoteOrderLine(Guid QuotationLineId, decimal? Quantity);
    public sealed record QuoteOrderWrite(QuoteOrderLine[]? Lines, string IdempotencyKey,
        string? CustomerPoNumber = null, DateOnly? CustomerPoDate = null,
        string? CustomerPoAttachmentReference = null, DateOnly? RateValidityUntil = null,
        string? Notes = null);
    public sealed record ConfirmWrite(long Version, string? OverrideReason);
    public sealed record ScheduleLineWrite(Guid SalesOrderLineId, decimal Quantity,
        DateOnly DeliveryDate, string? TimeWindow, Guid? SiteId, Guid? PreferredPlantId,
        string? OperationalNotes);
    public sealed record ScheduleWrite(long Version, ScheduleLineWrite[] Rows);
    public sealed record StatusWrite(long Version, string Reason);
    public sealed record AmendmentLine(Guid LineId, decimal? OrderedQuantity, decimal? Rate);
    public sealed record AmendmentWrite(long Version, string Reason, Guid? SiteId,
        AmendmentLine[] Lines);
    public sealed record ApplyAmendmentWrite(long Version);
    public sealed record ToleranceWrite(long Version, decimal RmcPercent, decimal SteelPercent);

    public static void MapM06(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m06")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices
                .GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M06",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });

        api.MapGet("/orders", List);
        api.MapGet("/orders/{orderId:guid}", Detail);
        api.MapPost("/orders", CreateDirect);
        api.MapPost("/orders/from-quotation/{quotationId:guid}", CreateFromQuotation);
        api.MapPost("/orders/{orderId:guid}/confirm", Confirm);
        api.MapPost("/orders/{orderId:guid}/schedules", AddSchedules);
        api.MapPost("/orders/{orderId:guid}/schedules/{scheduleId:guid}/apply", ApplySchedule);
        api.MapPost("/orders/{orderId:guid}/hold", Hold);
        api.MapPost("/orders/{orderId:guid}/cancel", Cancel);
        api.MapPost("/orders/{orderId:guid}/short-close", ShortClose);
        api.MapPost("/orders/{orderId:guid}/reverse-short-close", ReverseShortClose);
        api.MapPost("/orders/{orderId:guid}/amendments", ProposeAmendment);
        api.MapPost("/orders/{orderId:guid}/amendments/{amendmentId:guid}/apply", ApplyAmendment);
        api.MapGet("/orders/{orderId:guid}/confirmation", Confirmation);
        api.MapGet("/reports/pending-orders", PendingReport);
        api.MapGet("/settings/delivery-tolerances", GetDeliveryTolerances);
        api.MapPut("/settings/delivery-tolerances", UpdateDeliveryTolerances);
    }

    private static async Task<IResult> GetDeliveryTolerances(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        await authorization.Require("M06", "m06.orders.view", ct);
        var tenant = await db.Set<Tenant>().AsNoTracking().SingleAsync(ct);
        var setup = OnboardingStore.Decode(tenant);
        return Results.Ok(new
        {
            version = tenant.Version,
            rmcPercent = setup.M06RmcOverdeliveryTolerancePercent,
            steelPercent = setup.M06SteelOverdeliveryTolerancePercent
        });
    }

    private static async Task<IResult> UpdateDeliveryTolerances(
        ToleranceWrite request, TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenantContext, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.configure", ct);
        if (request.RmcPercent is < 0 or > 100 || request.SteelPercent is < 0 or > 100 ||
            decimal.Round(request.RmcPercent, 4) != request.RmcPercent ||
            decimal.Round(request.SteelPercent, 4) != request.SteelPercent)
            throw new ValidationException("Delivery tolerances must be between 0 and 100 with up to four decimals.");
        var tenant = await db.Set<Tenant>().SingleAsync(ct);
        if (tenant.Version != request.Version)
            return Results.Conflict(new { detail = "Tenant settings changed. Reload before updating tolerances.", currentVersion = tenant.Version });
        var setup = OnboardingStore.Decode(tenant);
        var before = new
        {
            rmcPercent = setup.M06RmcOverdeliveryTolerancePercent,
            steelPercent = setup.M06SteelOverdeliveryTolerancePercent
        };
        setup.M06RmcOverdeliveryTolerancePercent = request.RmcPercent;
        setup.M06SteelOverdeliveryTolerancePercent = request.SteelPercent;
        tenant.Configuration = OnboardingStore.Encode(setup);
        tenant.Version++;
        var after = new
        {
            rmcPercent = setup.M06RmcOverdeliveryTolerancePercent,
            steelPercent = setup.M06SteelOverdeliveryTolerancePercent
        };
        db.Add(new M02AuditEvent
        {
            TenantId = tenantContext.TenantId, EventId = Guid.NewGuid(),
            ActorUserId = actor.UserId, ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString(),
            Action = "M06 delivery tolerances updated", EntityType = "Tenant",
            EntityId = tenantContext.TenantId,
            Before = JsonSerializer.Serialize(before, Json), After = JsonSerializer.Serialize(after, Json),
            At = DateTimeOffset.UtcNow
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "Tenant settings changed concurrently. Reload and retry." });
        }
        return Results.Ok(new { version = tenant.Version, after.rmcPercent, after.steelPercent });
    }

    private static async Task<IResult> List(
        string? q, string? status, Guid? customerId, Guid? siteId, Guid? productId,
        Guid? branchId, DateOnly? from, DateOnly? to, int page, int pageSize,
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.view", ct);
        var scopes = await authorization.Scopes(user.UserId, "m06.orders.view", ct);
        var query = db.Set<M06SalesOrder>().AsNoTracking();
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId != null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId != null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x => (own && x.SalesOwnerUserId == user.UserId) ||
                                     (x.BranchId != null && branches.Contains(x.BranchId.Value)) ||
                                     (plants.Length > 0 && db.Set<M06DeliverySchedule>().Any(schedule =>
                                         schedule.SalesOrderId == x.SalesOrderId &&
                                         schedule.PreferredPlantId != null &&
                                         plants.Contains(schedule.PreferredPlantId.Value))));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Number.Contains(term) ||
                                     x.CustomerNameSnapshot.Contains(term) ||
                                     (x.CustomerPoNumber ?? "").Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (customerId.HasValue) query = query.Where(x => x.CustomerPartyId == customerId);
        if (siteId.HasValue) query = query.Where(x => x.SiteId == siteId);
        if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
        if (from.HasValue) query = query.Where(x => x.OrderDate >= from);
        if (to.HasValue) query = query.Where(x => x.OrderDate <= to);
        if (productId.HasValue)
            query = query.Where(x => db.Set<M06SalesOrderLine>().Any(line =>
                line.SalesOrderId == x.SalesOrderId && line.ProductId == productId));
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize == 0 ? 25 : pageSize, 1, 100);
        var total = await query.LongCountAsync(ct);
        var rows = await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.SalesOrderId, x.Number, x.OrderDate, x.CustomerPartyId,
                x.CustomerNameSnapshot, x.SiteId, x.SourceQuotationId,
                x.SourceQuotationNumber, x.OrderType, x.Status, x.Total,
                x.BranchId, x.SalesOwnerUserId, x.Version, x.UpdatedAt
            }).ToListAsync(ct);
        return Results.Ok(new { page, pageSize, total, rows });
    }

    private static async Task<IResult> Detail(
        Guid orderId, TenantAuthorizationService authorization, BerynxDbContext db,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.view", ct);
        var order = await db.Set<M06SalesOrder>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        if (order is null || !await InScope(authorization, db, actor.UserId, "m06.orders.view", order, ct))
            return Results.NotFound();
        var lines = await db.Set<M06SalesOrderLine>().AsNoTracking()
            .Where(x => x.SalesOrderId == orderId).OrderBy(x => x.ProductNameSnapshot).ToListAsync(ct);
        var charges = await db.Set<M06SalesOrderCharge>().AsNoTracking()
            .Where(x => x.SalesOrderId == orderId).ToListAsync(ct);
        var schedules = await db.Set<M06DeliverySchedule>().AsNoTracking()
            .Where(x => x.SalesOrderId == orderId).OrderBy(x => x.DeliveryDate).ToListAsync(ct);
        var scheduleApprovalIds = schedules.Where(x => x.ApprovalRequestId != null)
            .Select(x => x.ApprovalRequestId!.Value).ToArray();
        var scheduleApprovalStatuses = await db.Set<M02ApprovalRequest>().AsNoTracking()
            .Where(x => scheduleApprovalIds.Contains(x.RequestId))
            .ToDictionaryAsync(x => x.RequestId, x => x.Status, ct);
        var documents = await db.Set<M06LinkedDocument>().AsNoTracking()
            .Where(x => x.SalesOrderId == orderId).OrderBy(x => x.LinkedAt).ToListAsync(ct);
        var activities = await db.Set<M06Activity>().AsNoTracking()
            .Where(x => x.SalesOrderId == orderId).OrderByDescending(x => x.At).Take(200).ToListAsync(ct);
        var amendments = await db.Set<M06Amendment>().AsNoTracking()
            .Where(x => x.SalesOrderId == orderId).OrderByDescending(x => x.RevisionNumber).ToListAsync(ct);
        var amendmentApprovalIds = amendments.Where(x => x.ApprovalRequestId != null)
            .Select(x => x.ApprovalRequestId!.Value).ToArray();
        var amendmentApprovalStatuses = await db.Set<M02ApprovalRequest>().AsNoTracking()
            .Where(x => amendmentApprovalIds.Contains(x.RequestId))
            .ToDictionaryAsync(x => x.RequestId, x => x.Status, ct);
        var canViewCost = await authorization.CanAccess("M06", actor.UserId, "global.view_cost",
            order.BranchId, null, order.SalesOwnerUserId, ct, write: false);
        var unitCodes = lines.Select(x => x.UnitCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var oneUnit = unitCodes.Length == 1;
        return Results.Ok(new
        {
            order.SalesOrderId, order.Number, order.OrderDate, order.BranchId,
            order.CustomerPartyId, order.CustomerNameSnapshot, order.SiteId,
            order.SiteAddressSnapshot, order.CustomerPoNumber, order.CustomerPoDate,
            order.CustomerPoAttachmentReference, order.SourceQuotationId,
            order.SourceQuotationNumber, order.OrderType, order.Status,
            order.SalesOwnerUserId, order.RateValidityUntil, order.PaymentTermId,
            order.PaymentTermsSnapshot, order.DeliveryTermsSnapshot, order.Notes,
            order.Subtotal, order.DiscountTotal, order.ChargesTotal, order.TaxTotal, order.Total,
            order.CreditStatus, CreditSnapshot = ParseJson(order.CreditSnapshotJson),
            order.CostStatus,
            EstimatedMarginPercent = canViewCost ? order.EstimatedMarginPercent : null,
            ApprovalSnapshot = ParseJson(order.ApprovalSnapshotJson), order.ApprovalRequestId,
            order.Version, order.ConfirmedAt, order.CompletedAt, order.ClosureReason,
            lines, charges,
            schedules = schedules.Select(x => new
            {
                x.ScheduleId, x.SalesOrderId, x.SalesOrderLineId, x.ProductId, x.Quantity,
                x.DeliveryDate, x.TimeWindow, x.SiteId, x.PreferredPlantId,
                x.OperationalNotes, x.Status, x.ApprovalRequestId, x.CreatedAt,
                approvalStatus = x.ApprovalRequestId is { } id
                    ? scheduleApprovalStatuses.GetValueOrDefault(id) : null
            }),
            amendments = amendments.Select(x => new
            {
                x.AmendmentId, x.SalesOrderId, x.RevisionNumber, x.Reason,
                x.BeforeSnapshotJson, x.ProposedSnapshotJson,
                x.Status, x.ApprovalRequestId, x.RequestedByUserId, x.CreatedAt,
                approvalStatus = x.ApprovalRequestId is { } id
                    ? amendmentApprovalStatuses.GetValueOrDefault(id) : null
            }),
            activities, documents,
            linkedQuotation = order.SourceQuotationId is null
                ? new { status = "not-applicable", message = "No source quotation." }
                : new { status = "available", message = order.SourceQuotationNumber ?? order.SourceQuotationId.Value.ToString() },
            purchaseOrders = ModulePending(documents, "M07", "PurchaseOrder"),
            dispatches = ModulePending(documents, "M08", "Dispatch"),
            invoices = ModulePending(documents, "M10", "Invoice"),
            receipts = ModulePending(documents, "M11", "Receipt"),
            fulfilment = new
            {
                orderedQuantity = oneUnit ? lines.Sum(x => x.OrderedQuantity) : (decimal?)null,
                scheduledQuantity = oneUnit
                    ? schedules.Where(x => x.Status == "Planned" || x.Status == "Released")
                        .Sum(x => x.Quantity)
                    : (decimal?)null,
                quantityUnitCode = oneUnit ? unitCodes[0] : null,
                mixedUnits = !oneUnit,
                lineProgress = lines.Select(line => new
                {
                    line.LineId, line.ProductNameSnapshot, line.UnitCode,
                    line.OrderedQuantity,
                    scheduledQuantity = schedules.Where(x => x.SalesOrderLineId == line.LineId &&
                        x.Status is "Planned" or "Released").Sum(x => x.Quantity),
                    deliveredQuantity = (decimal?)null,
                    invoicedQuantity = (decimal?)null,
                    paidAmount = (decimal?)null
                }),
                deliveredQuantity = (decimal?)null,
                invoicedQuantity = (decimal?)null,
                paidAmount = (decimal?)null,
                downstreamState = "Pending modules M08, M10 and M11"
            }
        });
    }

    private static async Task<IResult> CreateDirect(
        DirectOrderWrite request, TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, M03MasterService masters, OnboardingStore onboarding,
        IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.create", ct);
        ValidateIdempotency(request.IdempotencyKey);
        var duplicate = await db.Set<M06SalesOrder>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (duplicate is not null)
            return duplicate.SourceQuotationId is null
                ? Results.Ok(new { duplicate.SalesOrderId, duplicate.Number, duplicate.Status, duplicate = true })
                : Results.Conflict(new { detail = "The idempotency key was already used for a quotation conversion." });
        ValidateDirect(request);

        var customer = await db.Set<M03Party>().AsNoTracking().SingleOrDefaultAsync(
            x => x.PartyId == request.CustomerPartyId && x.IsActive, ct)
            ?? throw new ValidationException("Choose an active M03 customer.");
        if (!await db.Set<M03PartyRole>().AnyAsync(x =>
                x.PartyId == customer.PartyId && x.RoleCode == "Customer", ct))
            throw new ValidationException("The selected party is not an M03 customer.");
        var site = request.SiteId is { } siteId
            ? await db.Set<M03Site>().AsNoTracking().SingleOrDefaultAsync(
                x => x.SiteId == siteId && x.CustomerPartyId == customer.PartyId && x.Status == "Active", ct)
              ?? throw new ValidationException("Choose an active site belonging to the customer.")
            : null;
        if (site?.BranchId is { } siteBranch && request.BranchId is { } requestedBranch &&
            siteBranch != requestedBranch)
            throw new ValidationException("The site belongs to a different branch.");
        var branchId = site?.BranchId ?? request.BranchId;
        if (!await authorization.CanAccess("M06", actor.UserId, "m06.orders.create",
                branchId, null, actor.UserId, ct))
            throw new UnauthorizedAccessException("The order branch is outside your assignment scope.");
        if (request.PaymentTermId.HasValue && !await db.Set<M03PaymentTerm>()
                .AnyAsync(x => x.PaymentTermId == request.PaymentTermId && x.IsActive, ct))
            throw new ValidationException("Choose an active M03 payment term.");

        var setupTenant = await db.Set<Tenant>().AsNoTracking().SingleAsync(ct);
        var setup = OnboardingStore.Decode(setupTenant);
        var branch = branchId is { } b ? setup.Branches.SingleOrDefault(x => x.Id == b) : null;
        if (branchId is not null && branch is null)
            throw new ValidationException("Choose a branch configured for the tenant.");
        var sellerGstin = branch?.Gstin ?? setup.Registrations.FirstOrDefault() ?? "";
        var place = request.PlaceOfSupplyStateCode.Trim();
        if (place.Length != 2 || place.Any(c => !char.IsAsciiDigit(c)))
            throw new ValidationException("Place of supply must be a two-digit state code.");
        var effectiveOn = request.OrderDate;
        var canOverridePrice = await authorization.CanAccess("M06", actor.UserId,
            "m06.orders.price_override", branchId, null, actor.UserId, ct);
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var order = new M06SalesOrder
        {
            TenantId = tenant.TenantId, SalesOrderId = orderId,
            Number = "",
            OrderDate = request.OrderDate, BranchId = branchId,
            CustomerPartyId = customer.PartyId,
            CustomerNameSnapshot = customer.TradeName ?? customer.LegalName,
            SiteId = site?.SiteId,
            SiteAddressSnapshot = site is null ? "" : string.Join(", ",
                new[] { site.Address, site.City, site.StateCode }.Where(x => !string.IsNullOrWhiteSpace(x))),
            CustomerPoNumber = EmptyToNull(request.CustomerPoNumber),
            CustomerPoDate = request.CustomerPoDate,
            CustomerPoAttachmentReference = EmptyToNull(request.CustomerPoAttachmentReference),
            OrderType = request.OrderType, Status = "Draft", SalesOwnerUserId = actor.UserId,
            RateValidityUntil = request.RateValidityUntil, PaymentTermId = request.PaymentTermId,
            PaymentTermsSnapshot = request.PaymentTerms.Trim(),
            DeliveryTermsSnapshot = request.DeliveryTerms.Trim(), Notes = request.Notes.Trim(),
            CreditStatus = customer.CreditStatus,
            CreditSnapshotJson = JsonSerializer.Serialize(new
            {
                status = "NotChecked", source = "M03 credit evaluation runs at confirmation",
                customer.CreditLimit, customer.CreditDays, customer.CreditPolicy, customer.CreditStatus
            }, Json),
            CostStatus = "Unavailable", IdempotencyKey = request.IdempotencyKey.Trim(),
            CreatedAt = now, UpdatedAt = now
        };
        var orderLines = new List<M06SalesOrderLine>();
        var minRateLines = new List<(Guid ProductId, decimal Rate, bool BelowMinimum, decimal Discount)>();
        foreach (var input in request.Lines)
        {
            var product = await db.Set<M03Product>().AsNoTracking().SingleOrDefaultAsync(
                x => x.ProductId == input.ProductId && x.IsActive, ct)
                ?? throw new ValidationException("Choose an active M03 product.");
            if (!string.Equals(product.BaseUnitCode, input.UnitCode.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Order UOM must match the product base unit.");
            if (product.Category == "RMC" &&
                (string.IsNullOrWhiteSpace(input.RmcGrade) ||
                 string.IsNullOrWhiteSpace(input.RmcSlumpBand) ||
                 string.IsNullOrWhiteSpace(input.RmcAdmixtureCode) ||
                 string.IsNullOrWhiteSpace(input.RmcPumpType)))
                throw new ValidationException("RMC lines require a full approved rate-card selection.");
            if (product.Category != "RMC" &&
                new[] { input.RmcGrade, input.RmcSlumpBand, input.RmcAdmixtureCode, input.RmcPumpType }
                    .Any(value => !string.IsNullOrWhiteSpace(value)))
                throw new ValidationException("RMC rate-card attributes may only be supplied for RMC products.");
            decimal listRate;
            Guid? priceListId = null;
            var belowMinimum = false;
            if (!string.IsNullOrWhiteSpace(input.RmcGrade))
            {
                if (product.Category != "RMC" || string.IsNullOrWhiteSpace(input.RmcSlumpBand) ||
                    string.IsNullOrWhiteSpace(input.RmcAdmixtureCode) ||
                    string.IsNullOrWhiteSpace(input.RmcPumpType))
                    throw new ValidationException("RMC lines require a full approved rate-card selection.");
                var card = await masters.ResolveRmc(input.ProductId, input.RmcGrade,
                    input.RmcSlumpBand, input.RmcAdmixtureCode, input.RmcPumpType,
                    effectiveOn, ct, request.PriceBasis);
                listRate = card.Rate;
                belowMinimum = input.Rate is { } rmcRate && rmcRate < card.Rate;
            }
            else
            {
                var price = await masters.ResolvePrice(customer.PartyId, input.ProductId,
                    effectiveOn, ct, input.Rate, request.PriceBasis);
                listRate = price.Rate;
                priceListId = price.PriceListId;
                belowMinimum = price.ProposedRateBelowMinimum;
            }
            if (input.Rate is not null && input.Rate != listRate && !canOverridePrice)
                throw new UnauthorizedAccessException("Manual order rates require m06.orders.price_override.");
            var rate = input.Rate ?? listRate;
            var tax = await masters.ResolveTaxRate(product.ProductId, customer.CustomerCategory,
                effectiveOn, sellerGstin, customer.Gstin, place, ct);
            var intraState = sellerGstin.Length >= 2 && sellerGstin[..2] == place;
            var amounts = M05QuotationCalculator.CalculateLine(
                input.Quantity, rate, input.DiscountPercent, tax.RatePercent, intraState);
            var lineId = Guid.NewGuid();
            var snapshot = M03TaxSnapshot.Create(product.ProductId, tax.HsnCode, tax.SellerGstin,
                tax.BuyerGstin, tax.PlaceOfSupplyStateCode ?? place, effectiveOn, tax.Source,
                amounts.TaxableAmount, tax.RatePercent, intraState ? tax.RatePercent / 2m : 0m,
                intraState ? tax.RatePercent / 2m : 0m, intraState ? 0m : tax.RatePercent,
                tax.SourceRateId);
            orderLines.Add(new M06SalesOrderLine
            {
                TenantId = tenant.TenantId, LineId = lineId, SalesOrderId = orderId,
                ProductId = product.ProductId, ProductNameSnapshot = product.Name,
                ProductCategorySnapshot = product.Category, UnitCode = product.BaseUnitCode,
                OrderedQuantity = input.Quantity, Rate = rate,
                DiscountPercent = input.DiscountPercent, GstPercent = tax.RatePercent,
                TaxableAmount = amounts.TaxableAmount, CgstAmount = amounts.CgstAmount,
                SgstAmount = amounts.SgstAmount, IgstAmount = amounts.IgstAmount,
                TaxAmount = amounts.TaxAmount, Total = amounts.Total,
                TaxSnapshotJson = JsonSerializer.Serialize(snapshot, Json)
            });
            minRateLines.Add((product.ProductId, rate, belowMinimum, input.DiscountPercent));
        }
        var charges = new List<M06SalesOrderCharge>();
        foreach (var input in request.Charges)
        {
            var type = input.ChargeTypeId is { } chargeId
                ? await db.Set<M03ChargeType>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.ChargeTypeId == chargeId && x.IsActive, ct)
                  ?? throw new ValidationException("Choose an active M03 charge type.")
                : null;
            var taxRate = type?.DefaultTaxRatePercent ?? 0m;
            var isIntra = sellerGstin.Length >= 2 && sellerGstin[..2] == place;
            var amounts = M05QuotationCalculator.CalculateLine(1m, input.Amount, 0m, taxRate, isIntra);
            charges.Add(new M06SalesOrderCharge
            {
                TenantId = tenant.TenantId, ChargeId = Guid.NewGuid(), SalesOrderId = orderId,
                Description = input.Description.Trim(), Amount = input.Amount,
                TaxRatePercent = taxRate, CgstAmount = amounts.CgstAmount,
                SgstAmount = amounts.SgstAmount, IgstAmount = amounts.IgstAmount,
                TaxAmount = amounts.TaxAmount, Total = amounts.Total,
                TaxSnapshotJson = JsonSerializer.Serialize(new
                {
                    source = "M03ChargeType", chargeTypeId = type?.ChargeTypeId, taxRate,
                    effectiveOn, placeOfSupply = place, sellerGstin
                }, Json)
            });
        }
        order.Subtotal = orderLines.Sum(x => x.TaxableAmount + x.DiscountAmount());
        order.DiscountTotal = orderLines.Sum(x => x.GrossAmount() - x.TaxableAmount);
        order.ChargesTotal = charges.Sum(x => x.Amount);
        order.TaxTotal = orderLines.Sum(x => x.TaxAmount) + charges.Sum(x => x.TaxAmount);
        order.Total = orderLines.Sum(x => x.Total) + charges.Sum(x => x.Total);
        order.OrderType = DeriveOrderType(orderLines);
        if (request.OrderType != order.OrderType)
            throw new ValidationException($"Order type must match the product lines ({order.OrderType}).");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        order.Number = await AllocateOrderNumber(db, tenant.TenantId, request.OrderDate.Year, ct);
        db.Add(order);
        db.AddRange(orderLines);
        db.AddRange(charges);
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Direct order created",
            new { order.Total, order.OrderType }, now));
        await db.SaveChangesAsync(ct);
        var needsPricingApproval = minRateLines.Any(x => x.BelowMinimum || x.Discount > 0m);
        if (needsPricingApproval)
        {
            var snapshot = JsonSerializer.SerializeToElement(new
            {
                order.SalesOrderId, order.Total, order.DiscountTotal,
                lines = minRateLines.Select(x => new { x.ProductId, x.Rate, x.BelowMinimum, x.Discount }),
                costStatus = "Unavailable"
            }, Json);
            var approvalId = await approvals.Submit(new ApprovalSubmission(
                "M06SalesOrderPricing", orderId, branchId, null, actor.UserId,
                order.Total, minRateLines.Max(x => x.Discount), null, customer.CreditStatus,
                snapshot), ct);
            if (approvalId is null)
                throw new ValidationException("Pricing requires an M02 approval rule; the order was not saved.");
            order.ApprovalRequestId = approvalId;
            order.ApprovalSnapshotJson = snapshot.GetRawText();
            order.Status = "Pending approval";
            order.Version++;
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/m06/orders/{orderId}",
            new { order.SalesOrderId, order.Number, order.Status, order.Total, order.Version });
    }

    private static async Task<IResult> CreateFromQuotation(
        Guid quotationId, QuoteOrderWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.create", ct);
        await authorization.Require("M05", "m05.quotations.view", ct);
        ValidateIdempotency(request.IdempotencyKey);
        var existing = await db.Set<M06SalesOrder>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null)
            return existing.SourceQuotationId == quotationId
                ? Results.Ok(new { existing.SalesOrderId, existing.Number, existing.Status, duplicate = true })
                : Results.Conflict(new { detail = "The idempotency key was already used for a different action or quotation." });

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var quote = await db.Set<M05Quotation>().SingleOrDefaultAsync(
            x => x.QuotationId == quotationId, ct);
        if (quote is null) return Results.NotFound();
        if (!await authorization.CanAccess("M05", actor.UserId, "m05.quotations.view",
                quote.BranchId, null, quote.SalespersonUserId, ct, write: false))
            return Results.NotFound();
        if (!await authorization.CanAccess("M06", actor.UserId, "m06.orders.create",
                quote.BranchId, null, actor.UserId, ct, write: true))
            throw new UnauthorizedAccessException("The quotation branch is outside your sales-order creation scope.");
        if (quote.Status != "Accepted")
            throw new ValidationException("Only an accepted quotation can be converted.");
        if (quote.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ValidationException("The accepted quotation is expired; revise and revalidate it first.");
        var quoteLines = await db.Set<M05QuotationLine>().Where(x => x.QuotationId == quotationId)
            .OrderBy(x => x.LineId).ToListAsync(ct);
        var allocations = await db.Set<M06QuotationAllocation>().Where(x => x.QuotationId == quotationId)
            .GroupBy(x => x.QuotationLineId)
            .Select(x => new { LineId = x.Key, Quantity = x.Sum(row => row.Quantity) })
            .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);
        if (request.Lines is { } requestedLines &&
            requestedLines.Select(x => x.QuotationLineId).Distinct().Count() != requestedLines.Length)
            throw new ValidationException("A quotation line may appear only once in a conversion request.");
        var requested = request.Lines?.ToDictionary(x => x.QuotationLineId, x => x.Quantity);
        if (requested is not null && requested.Keys.Any(id => quoteLines.All(line => line.LineId != id)))
            throw new ValidationException("An order line does not belong to this quotation.");

        var now = DateTimeOffset.UtcNow;
        var orderId = Guid.NewGuid();
        var number = await AllocateOrderNumber(db, tenant.TenantId, DateTime.UtcNow.Year, ct);
        var order = new M06SalesOrder
        {
            TenantId = tenant.TenantId, SalesOrderId = orderId,
            Number = number,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            BranchId = quote.BranchId, CustomerPartyId = quote.CustomerPartyId,
            CustomerNameSnapshot = quote.CustomerNameSnapshot, SiteId = quote.SiteId,
            SiteAddressSnapshot = quote.SiteAddressSnapshot,
            CustomerPoNumber = EmptyToNull(request.CustomerPoNumber),
            CustomerPoDate = request.CustomerPoDate,
            CustomerPoAttachmentReference = EmptyToNull(request.CustomerPoAttachmentReference),
            SourceQuotationId = quote.QuotationId, SourceQuotationNumber = quote.Number,
            SourceEnquiryId = quote.SourceEnquiryId,
            OrderType = "", Status = "Draft", SalesOwnerUserId = actor.UserId,
            RateValidityUntil = request.RateValidityUntil ?? quote.ValidUntil,
            PaymentTermId = quote.PaymentTermId,
            PaymentTermsSnapshot = quote.TermsAndConditions,
            DeliveryTermsSnapshot = quote.DeliveryTerms, Notes = request.Notes?.Trim() ?? "",
            CreditStatus = quote.CreditStatus, CreditSnapshotJson = quote.CreditSnapshotJson,
            CostStatus = quote.CostStatus, EstimatedMarginPercent = quote.MarginPercent,
            IdempotencyKey = request.IdempotencyKey.Trim(), CreatedAt = now, UpdatedAt = now
        };
        var orderLines = new List<M06SalesOrderLine>();
        var quoteAllocations = new List<M06QuotationAllocation>();
        var remainingQuotationValue = quoteLines.Sum(x =>
            Math.Max(0m, x.Quantity - allocations.GetValueOrDefault(x.LineId)) * x.Rate);
        foreach (var source in quoteLines)
        {
            var remaining = source.Quantity - allocations.GetValueOrDefault(source.LineId);
            if (remaining <= 0m) continue;
            var quantity = requested is null
                ? remaining
                : requested.TryGetValue(source.LineId, out var requestedQuantity)
                    ? requestedQuantity ?? 0m
                    : 0m;
            if (quantity == 0m && requested?.ContainsKey(source.LineId) != true) continue;
            if (quantity <= 0 || quantity > remaining)
                throw new ValidationException("Order quantity must be positive and no greater than the quotation's remaining quantity.");
            var amounts = M05QuotationCalculator.CalculateLine(quantity, source.Rate,
                source.DiscountPercent, source.GstPercent,
                source.CgstAmount > 0 || source.SgstAmount > 0);
            var lineId = Guid.NewGuid();
            orderLines.Add(new M06SalesOrderLine
            {
                TenantId = tenant.TenantId, LineId = lineId, SalesOrderId = orderId,
                SourceQuotationLineId = source.LineId, ProductId = source.ProductId,
                ProductNameSnapshot = source.Description, ProductCategorySnapshot =
                    await db.Set<M03Product>().Where(x => x.ProductId == source.ProductId)
                        .Select(x => x.Category).SingleAsync(ct),
                UnitCode = source.UnitCode, OrderedQuantity = quantity, Rate = source.Rate,
                DiscountPercent = source.DiscountPercent, GstPercent = source.GstPercent,
                TaxableAmount = amounts.TaxableAmount, CgstAmount = amounts.CgstAmount,
                SgstAmount = amounts.SgstAmount, IgstAmount = amounts.IgstAmount,
                TaxAmount = amounts.TaxAmount, Total = amounts.Total,
                TaxSnapshotJson = JsonSerializer.Serialize(new
                {
                    sourceQuotationSnapshot = JsonDocument.Parse(source.TaxSnapshotJson).RootElement,
                    sourceQuotationLineId = source.LineId, orderQuantity = quantity,
                    taxableAmount = amounts.TaxableAmount, taxAmount = amounts.TaxAmount
                }, Json)
            });
            quoteAllocations.Add(new M06QuotationAllocation
            {
                TenantId = tenant.TenantId, AllocationId = Guid.NewGuid(),
                QuotationId = quote.QuotationId, QuotationLineId = source.LineId,
                SalesOrderId = orderId, SalesOrderLineId = lineId, Quantity = quantity,
                CreatedAt = now
            });
        }
        if (orderLines.Count == 0)
            throw new ValidationException("This quotation has no remaining quantity to convert.");
        order.OrderType = DeriveOrderType(orderLines);
        order.Subtotal = orderLines.Sum(x => x.TaxableAmount + x.GrossAmount() - x.TaxableAmount);
        order.DiscountTotal = orderLines.Sum(x => x.GrossAmount() - x.TaxableAmount);
        order.TaxTotal = orderLines.Sum(x => x.TaxAmount);
        order.Total = orderLines.Sum(x => x.Total);

        var selectedQuotationValue = orderLines.Sum(x => x.OrderedQuantity * x.Rate);
        var allocationRatio = remainingQuotationValue <= 0m
            ? 0m
            : Math.Min(1m, selectedQuotationValue / remainingQuotationValue);
        var quoteCharges = await db.Set<M05QuotationCharge>().Where(x => x.QuotationId == quotationId)
            .ToListAsync(ct);
        var previousChargeAllocations = await db.Set<M06QuotationChargeAllocation>()
            .Where(x => x.QuotationId == quotationId)
            .GroupBy(x => x.QuotationChargeId)
            .Select(x => new
            {
                ChargeId = x.Key, Amount = x.Sum(row => row.Amount),
                Cgst = x.Sum(row => row.CgstAmount), Sgst = x.Sum(row => row.SgstAmount),
                Igst = x.Sum(row => row.IgstAmount), Tax = x.Sum(row => row.TaxAmount)
            }).ToDictionaryAsync(x => x.ChargeId, ct);
        var orderCharges = new List<M06SalesOrderCharge>();
        var chargeAllocations = new List<M06QuotationChargeAllocation>();
        foreach (var source in quoteCharges)
        {
            var used = previousChargeAllocations.GetValueOrDefault(source.ChargeId);
            var remainingAmount = source.Amount - (used?.Amount ?? 0m);
            var remainingCgst = source.CgstAmount - (used?.Cgst ?? 0m);
            var remainingSgst = source.SgstAmount - (used?.Sgst ?? 0m);
            var remainingIgst = source.IgstAmount - (used?.Igst ?? 0m);
            var remainingTax = source.TaxAmount - (used?.Tax ?? 0m);
            decimal Allocate(decimal value) => allocationRatio >= 1m
                ? value
                : decimal.Round(value * allocationRatio, 2, MidpointRounding.AwayFromZero);
            var amount = Allocate(remainingAmount);
            var cgst = Allocate(remainingCgst);
            var sgst = Allocate(remainingSgst);
            var igst = Allocate(remainingIgst);
            var tax = cgst + sgst + igst;
            if (remainingTax == 0m) tax = 0m;
            var charge = new M06SalesOrderCharge
            {
                TenantId = tenant.TenantId, ChargeId = Guid.NewGuid(), SalesOrderId = orderId,
                SourceQuotationChargeId = source.ChargeId, Description = source.Description,
                Amount = amount, TaxRatePercent = source.TaxRatePercent,
                CgstAmount = cgst, SgstAmount = sgst, IgstAmount = igst,
                TaxAmount = tax, Total = amount + tax,
                TaxSnapshotJson = source.TaxTreatmentSnapshotJson
            };
            orderCharges.Add(charge);
            chargeAllocations.Add(new M06QuotationChargeAllocation
            {
                TenantId = tenant.TenantId, AllocationId = Guid.NewGuid(),
                QuotationId = quote.QuotationId, QuotationChargeId = source.ChargeId,
                SalesOrderId = orderId, SalesOrderChargeId = charge.ChargeId,
                Amount = amount, CgstAmount = cgst, SgstAmount = sgst, IgstAmount = igst,
                TaxAmount = tax, CreatedAt = now
            });
        }
        order.ChargesTotal = orderCharges.Sum(x => x.Amount);
        order.TaxTotal += orderCharges.Sum(x => x.TaxAmount);
        order.Total += orderCharges.Sum(x => x.Total);

        db.Add(order);
        db.AddRange(orderLines);
        db.AddRange(orderCharges);
        db.AddRange(quoteAllocations);
        db.AddRange(chargeAllocations);
        quote.Version++;
        quote.UpdatedAt = now;
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Converted from accepted quotation",
            new { quote.QuotationId, quote.Number, lineCount = orderLines.Count }, now));
        if (quote.SourceEnquiryId is { } enquiryId)
        {
            await authorization.Require("M04", "m04.enquiries.edit", ct);
            var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(
                x => x.EnquiryId == enquiryId, ct);
            if (enquiry is null || enquiry.CustomerPartyId != quote.CustomerPartyId ||
                enquiry.Status == "Lost" ||
                !await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                    enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
                throw new ValidationException("The source enquiry cannot be linked to this order.");
            var existingEnquiryOrder = enquiry.LinkedSalesOrderId is { } linkedId
                ? await db.Set<M06SalesOrder>().AsNoTracking()
                    .SingleOrDefaultAsync(x => x.SalesOrderId == linkedId, ct)
                : null;
            var additionalCallOff = enquiry.Status == "Won" &&
                existingEnquiryOrder is { } previousOrder &&
                previousOrder.SourceQuotationId == quote.QuotationId &&
                previousOrder.SourceEnquiryId == enquiryId;
            if (enquiry.Status == "Won" && !additionalCallOff)
                throw new ValidationException("The Won enquiry is linked to a different order or quotation.");
            if (!additionalCallOff && enquiry.LinkedSalesOrderId is not null)
                throw new ValidationException("The enquiry is already linked to a different sales order.");
            var beforeStatus = enquiry.Status;
            enquiry.Status = "Won";
            if (!additionalCallOff)
                enquiry.LinkedSalesOrderId = orderId;
            enquiry.NextActionAt = null;
            enquiry.UpdatedAt = now;
            enquiry.Version++;
            db.Add(new M04EnquiryActivity
            {
                TenantId = tenant.TenantId, ActivityId = Guid.NewGuid(), EnquiryId = enquiryId,
                ActorUserId = actor.UserId, Kind = "Won", Channel = "",
                Outcome = order.Number,
                Notes = additionalCallOff
                    ? $"Additional order call-off from accepted quotation {quote.Number}; primary order link remains {existingEnquiryOrder!.Number}."
                    : $"Converted accepted quotation {quote.Number}.",
                At = now
            });
            db.Add(new M02AuditEvent
            {
                TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ActorUserId = actor.UserId,
                ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString(),
                Action = "Enquiry linked to sales order", EntityType = "M04Enquiry",
                EntityId = enquiryId, Before = JsonSerializer.Serialize(new { status = beforeStatus }, Json),
                After = JsonSerializer.Serialize(new
                {
                    status = enquiry.Status, salesOrderId = orderId,
                    primarySalesOrderId = enquiry.LinkedSalesOrderId
                }, Json),
                At = now
            });
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Results.Created($"/api/v1/m06/orders/{orderId}",
            new { order.SalesOrderId, order.Number, order.Status, order.Total, order.Version,
                sourceQuotationId = quote.QuotationId, convertedLineCount = orderLines.Count });
    }

    private static async Task<IResult> Confirm(
        Guid orderId, ConfirmWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, M03CreditEvaluationService credit, IM02ApprovalEngine approvals,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.confirm", ct);
        var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        if (order is null || !await InScope(authorization, db, actor.UserId, "m06.orders.confirm", order, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The order changed. Reload before confirming.", currentVersion = order.Version });
        if (order.Status is not ("Draft" or "Pending approval"))
            return Results.Conflict(new { detail = $"An order in {order.Status} cannot be confirmed." });
        if (order.RateValidityUntil is { } rateExpiry &&
            rateExpiry < DateOnly.FromDateTime(DateTime.UtcNow))
            return Results.Conflict(new
            {
                detail = "The frozen rate-validity date has passed. Create an approved amendment with revalidated pricing."
            });
        var priorApproval = order.ApprovalRequestId is { } approvalId
            ? await db.Set<M02ApprovalRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestId == approvalId, ct)
            : null;
        if (priorApproval?.Status == "Pending")
            return Results.Conflict(new { detail = "The order is awaiting approval.", order.ApprovalRequestId });
        if (priorApproval?.Status == "Rejected")
            return Results.Conflict(new { detail = "The order approval was rejected.", order.ApprovalRequestId });
        if (priorApproval?.SourceType == "M06SalesOrderPricing" && priorApproval.Status != "Approved")
            return Results.Conflict(new { detail = "Pricing approval must be completed before confirmation.", order.ApprovalRequestId });

        var canOverride = await authorization.CanAccess("M03", actor.UserId, "credit.override",
            order.BranchId, null, order.SalesOwnerUserId, ct);
        var assessment = await credit.Evaluate(order.CustomerPartyId, order.Total, canOverride, ct);
        var creditSnapshot = JsonSerializer.Serialize(new
        {
            assessment.Profile.CreditLimit,
            assessment.Profile.CreditDays, assessment.Profile.CreditPolicy,
            assessment.Profile.CreditStatus, assessment.Facts, assessment.Decision
        }, Json);
        order.CreditStatus = assessment.Decision.Status;
        order.CreditSnapshotJson = creditSnapshot;
        var approvalForCredit = priorApproval?.SourceType == "M06CreditOverride" &&
                                priorApproval.Status == "Approved";
        if ((assessment.Decision.Status is "Blocked" or "ApprovalRequired") && !approvalForCredit)
        {
            if (!assessment.Decision.CanRequestOverride)
            {
                order.UpdatedAt = DateTimeOffset.UtcNow;
                order.Version++;
                db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Credit check blocked",
                    new { assessment.Decision.Status, assessment.Decision.Reasons, assessment.Facts.Available },
                    order.UpdatedAt));
                await db.SaveChangesAsync(ct);
                return Results.Conflict(new
                {
                    detail = "Credit policy blocks confirmation and transaction-backed credit facts do not permit an override.",
                    creditStatus = assessment.Decision.Status, assessment.Decision.Reasons,
                    factsAvailable = assessment.Facts.Available
                });
            }
            if (string.IsNullOrWhiteSpace(request.OverrideReason))
                throw new ValidationException("A reason is required for a credit override request.");
            if (!canOverride)
                throw new UnauthorizedAccessException("The credit.override permission is required.");
            var snapshot = JsonSerializer.SerializeToElement(new
            {
                order.SalesOrderId, order.Number, order.Total, request.OverrideReason,
                credit = assessment
            }, Json);
            var submitted = await approvals.Submit(new ApprovalSubmission(
                "M06CreditOverride", order.SalesOrderId, order.BranchId, null, actor.UserId,
                order.Total, null, null, assessment.Decision.Status, snapshot), ct);
            if (submitted is null)
                return Results.Conflict(new { detail = "No matching M02 credit-override approval rule is configured." });
            order.ApprovalRequestId = submitted;
            order.ApprovalSnapshotJson = snapshot.GetRawText();
            var latest = await db.Set<M02ApprovalRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestId == submitted, ct);
            if (latest?.Status != "Approved")
            {
                order.Status = "Pending approval";
                order.Version++;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Credit override requested",
                    new { submitted, request.OverrideReason, assessment.Decision.Reasons }, order.UpdatedAt));
                await db.SaveChangesAsync(ct);
                return Results.Accepted($"/api/v1/m02/approvals/{submitted}",
                    new { order.SalesOrderId, order.Status, approvalRequestId = submitted,
                        creditStatus = assessment.Decision.Status });
            }
            approvalForCredit = true;
        }
        if (order.Status == "Pending approval" && !approvalForCredit &&
            priorApproval?.SourceType != "M06SalesOrderPricing" &&
            order.ApprovalRequestId is not null)
            return Results.Conflict(new { detail = "Pricing approval must be approved before confirmation." });

        M06Rules.RequireTransition(order.Status, "Confirmed");
        var before = order.Status;
        order.Status = "Confirmed";
        order.ConfirmedAt = DateTimeOffset.UtcNow;
        order.UpdatedAt = order.ConfirmedAt.Value;
        order.Version++;
        order.ApprovalSnapshotJson = JsonSerializer.Serialize(new
        {
            credit = assessment, creditOverrideApproved = approvalForCredit,
            approvalRequestId = order.ApprovalRequestId,
            estimatedMarginStatus = "Unavailable until authorized M07 purchase rates exist"
        }, Json);
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Order confirmed",
            new { before, order.Status, order.CreditStatus, order.ApprovalRequestId }, order.UpdatedAt));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The order was changed by another request. Reload and retry." });
        }
        return Results.Ok(new
        {
            order.SalesOrderId, order.Number, order.Status, order.Version,
            credit = assessment.Decision, order.CostStatus,
            estimatedMarginPercent = (decimal?)null
        });
    }

    private static async Task<IResult> AddSchedules(
        Guid orderId, ScheduleWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, OnboardingStore onboarding,
        IM02ApprovalEngine approvals, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.schedule", ct);
        var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        if (order is null || !await InScope(authorization, db, actor.UserId, "m06.orders.schedule", order, ct))
            return Results.NotFound();
        if (order.Status is not ("Confirmed" or "In fulfilment"))
            return Results.Conflict(new { detail = "Only confirmed orders can be scheduled." });
        if (request.Version != order.Version)
            return Results.Conflict(new { detail = "The order changed. Reload before scheduling.", currentVersion = order.Version });
        if (request.Rows is null || request.Rows.Length is < 1 or > 100)
            throw new ValidationException("Add between 1 and 100 schedule rows.");
        var lines = await db.Set<M06SalesOrderLine>().Where(x => x.SalesOrderId == orderId).ToDictionaryAsync(x => x.LineId, ct);
        var existing = await db.Set<M06DeliverySchedule>().Where(x =>
            x.SalesOrderId == orderId && (x.Status == "Planned" || x.Status == "Released" || x.Status == "Pending approval"))
            .GroupBy(x => x.SalesOrderLineId).Select(g => new { LineId = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);
        var setup = OnboardingStore.Decode(await onboarding.Current());
        var plannedRows = new List<M06DeliverySchedule>();
        foreach (var row in request.Rows)
        {
            if (!lines.TryGetValue(row.SalesOrderLineId, out var line))
                throw new ValidationException("A schedule row references a line from another order.");
            if (row.Quantity <= 0 || decimal.Round(row.Quantity, 3) != row.Quantity)
                throw new ValidationException("Schedule quantity must be positive with up to three decimals.");
            if (row.SiteId is { } siteId && !await db.Set<M03Site>().AnyAsync(x =>
                    x.SiteId == siteId && x.CustomerPartyId == order.CustomerPartyId && x.Status == "Active", ct))
                throw new ValidationException("A schedule site must belong to the order customer.");
            if (row.PreferredPlantId is { } plantId)
            {
                var plant = await db.Set<M03Plant>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.PlantId == plantId && x.IsActive, ct);
                if (plant is null)
                    throw new ValidationException("Choose an active M03 plant.");
                if (plant.BranchId is { } plantBranch && order.BranchId is { } orderBranch &&
                    plantBranch != orderBranch)
                    throw new ValidationException("The preferred plant belongs to a different branch.");
                if (!await authorization.CanAccess("M06", actor.UserId, "m06.orders.schedule",
                        order.BranchId, plant.PlantId, order.SalesOwnerUserId, ct))
                    throw new UnauthorizedAccessException("The preferred plant is outside your assignment scope.");
            }
            var tolerance = ToleranceFor(line.ProductCategorySnapshot, setup);
            var total = existing.GetValueOrDefault(line.LineId) + plannedRows
                .Where(x => x.SalesOrderLineId == line.LineId).Sum(x => x.Quantity) + row.Quantity;
            var exceedsTolerance = total > M06Rules.MaximumScheduleQuantity(line.OrderedQuantity, tolerance);
            var requiresApproval = exceedsTolerance;
            Guid? approvalId = null;
            var status = "Planned";
            if (requiresApproval)
            {
                var reason = exceedsTolerance
                    ? $"Schedule total {total} exceeds allowed tolerance {tolerance}%."
                    : $"Schedule total {total} uses configured overdelivery tolerance {tolerance}%.";
                var snapshot = JsonSerializer.SerializeToElement(new
                {
                    orderId, row.SalesOrderLineId, line.OrderedQuantity, scheduledTotal = total,
                    tolerancePercent = tolerance, reason
                }, Json);
                approvalId = await approvals.Submit(new ApprovalSubmission(
                    "M06DeliveryTolerance", orderId, order.BranchId, row.PreferredPlantId,
                    order.SalesOwnerUserId, order.Total, null, null, order.CreditStatus, snapshot), ct);
                if (approvalId is null)
                    throw new ValidationException("Delivery quantity requires an M02 approval rule; no schedule row was saved.");
                var approval = await db.Set<M02ApprovalRequest>().AsNoTracking()
                    .SingleOrDefaultAsync(x => x.RequestId == approvalId, ct);
                if (approval?.Status != "Approved") status = "Pending approval";
            }
            plannedRows.Add(new M06DeliverySchedule
            {
                TenantId = tenant.TenantId, ScheduleId = Guid.NewGuid(), SalesOrderId = orderId,
                SalesOrderLineId = line.LineId, ProductId = line.ProductId,
                Quantity = row.Quantity, DeliveryDate = row.DeliveryDate,
                TimeWindow = EmptyToNull(row.TimeWindow), SiteId = row.SiteId ?? order.SiteId,
                PreferredPlantId = row.PreferredPlantId,
                OperationalNotes = row.OperationalNotes?.Trim() ?? "",
                Status = status, ApprovalRequestId = approvalId, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        db.AddRange(plannedRows);
        if (order.Status == "Confirmed" && plannedRows.Any(x => x.Status == "Planned"))
            order.Status = "In fulfilment";
        order.Version++;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Delivery schedule added",
            new { rows = plannedRows.Select(x => new { x.ScheduleId, x.SalesOrderLineId, x.Quantity, x.Status }) },
            order.UpdatedAt));
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m06/orders/{orderId}",
            new { order.SalesOrderId, order.Version, schedules = plannedRows,
                dispatchState = "M08 is not implemented; schedule rows are persisted for future handoff." });
    }

    private static async Task<IResult> ApplySchedule(
        Guid orderId, Guid scheduleId, ApplyAmendmentWrite request,
        TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.schedule", ct);
        var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        var schedule = await db.Set<M06DeliverySchedule>().SingleOrDefaultAsync(
            x => x.ScheduleId == scheduleId && x.SalesOrderId == orderId, ct);
        if (order is null || schedule is null ||
            !await InScope(authorization, db, actor.UserId, "m06.orders.schedule", order, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The order changed. Reload before applying the approved schedule." });
        if (schedule.Status != "Pending approval" || schedule.ApprovalRequestId is not { } approvalId)
            return Results.Conflict(new { detail = "Only a pending schedule with an approval request can be applied." });
        var approval = await db.Set<M02ApprovalRequest>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.RequestId == approvalId, ct);
        if (approval?.Status != "Approved")
            return Results.Conflict(new { detail = "The M02 delivery-tolerance approval is not approved." });
        schedule.Status = "Planned";
        if (order.Status == "Confirmed") order.Status = "In fulfilment";
        order.Version++;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Approved delivery schedule applied",
            new { scheduleId, approvalId, schedule.Quantity }, order.UpdatedAt));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The schedule changed concurrently. Reload and retry." });
        }
        return Results.Ok(new { order.SalesOrderId, schedule.ScheduleId, schedule.Status, order.Version });
    }

    private static async Task<IResult> Hold(
        Guid orderId, StatusWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A reason is required to put an order on hold.");
        return await Transition(orderId, request, "On hold", "m06.orders.hold",
            authorization, db, tenant, ct);
    }

    private static async Task<IResult> Cancel(
        Guid orderId, StatusWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A cancellation reason is required.");
        var delivered = await db.Set<M06SalesOrderLine>().Where(x => x.SalesOrderId == orderId)
            .SumAsync(x => x.DeliveredQuantity, ct);
        if (delivered > 0)
            return Results.Conflict(new { detail = "Delivery exists; use short-close instead of cancellation." });
        if (await db.Set<M06LinkedDocument>().AnyAsync(x => x.SalesOrderId == orderId, ct) ||
            await db.Set<M06DeliverySchedule>().AnyAsync(x =>
                x.SalesOrderId == orderId && x.Status == "Released", ct))
            return Results.Conflict(new { detail = "Downstream commitments exist; cancel or release them in their source module first." });
        return await Transition(orderId, request, "Cancelled", "m06.orders.cancel",
            authorization, db, tenant, ct);
    }

    private static async Task<IResult> ShortClose(
        Guid orderId, StatusWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A short-close reason is required.");
        var delivered = await db.Set<M06SalesOrderLine>().Where(x => x.SalesOrderId == orderId)
            .SumAsync(x => x.DeliveredQuantity, ct);
        if (delivered <= 0)
            return Results.Conflict(new { detail = "No delivery exists; use cancellation to close an undelivered order." });
        if (await db.Set<M06LinkedDocument>().AnyAsync(x => x.SalesOrderId == orderId, ct) ||
            await db.Set<M06DeliverySchedule>().AnyAsync(x =>
                x.SalesOrderId == orderId && x.Status == "Released", ct))
            return Results.Conflict(new { detail = "Released downstream commitments must be resolved before short-close." });
        return await Transition(orderId, request, "Short-closed", "m06.orders.short_close",
            authorization, db, tenant, ct);
    }

    private static async Task<IResult> ReverseShortClose(
        Guid orderId, StatusWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, IM02ApprovalEngine approvals, ICurrentTenantContext tenant,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.short_close.reverse", ct);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("An approver reason is required to reverse a short-close.");
        var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        if (order is null || !await InScope(authorization, db, actor.UserId,
                "m06.orders.short_close.reverse", order, ct))
            return Results.NotFound();
        if (order.Status != "Short-closed" || order.Version != request.Version)
            return Results.Conflict(new { detail = "Reload a short-closed order before requesting reversal." });
        if (await db.Set<M06LinkedDocument>().AnyAsync(x => x.SalesOrderId == orderId, ct) ||
            await db.Set<M06DeliverySchedule>().AnyAsync(x =>
                x.SalesOrderId == orderId && x.Status == "Released", ct))
            return Results.Conflict(new { detail = "Downstream commitments exist; short-close cannot be reversed safely." });
        var priorReversal = order.ApprovalRequestId is { } previousId
            ? await db.Set<M02ApprovalRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestId == previousId &&
                    x.SourceType == "M06ShortCloseReversal" && x.SourceId == orderId, ct)
            : null;
        if (priorReversal?.Status == "Pending")
            return Results.Accepted($"/api/v1/m02/approvals/{priorReversal.RequestId}",
                new { order.SalesOrderId, approvalRequestId = priorReversal.RequestId, status = "Pending approval" });
        if (priorReversal?.Status == "Rejected")
            return Results.Conflict(new { detail = "The short-close reversal was rejected; submit a separately reviewed request." });
        var snapshot = JsonSerializer.SerializeToElement(new
        {
            order.SalesOrderId, order.Number, order.Status, order.ClosureReason,
            reversalReason = request.Reason.Trim(), targetStatus = "Confirmed"
        }, Json);
        var approvalId = priorReversal?.Status == "Approved"
            ? priorReversal.RequestId
            : await approvals.Submit(new ApprovalSubmission(
                "M06ShortCloseReversal", orderId, order.BranchId, null, actor.UserId,
                order.Total, null, null, order.CreditStatus, snapshot), ct);
        if (approvalId is null)
            return Results.Conflict(new { detail = "No M02 approval rule matched short-close reversal." });
        var approval = await db.Set<M02ApprovalRequest>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.RequestId == approvalId, ct);
        if (approval?.Status != "Approved")
        {
            order.ApprovalRequestId = approvalId;
            order.ApprovalSnapshotJson = snapshot.GetRawText();
            order.UpdatedAt = DateTimeOffset.UtcNow;
            order.Version++;
            db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Short-close reversal requested",
                new { approvalId, request.Reason }, order.UpdatedAt));
            await db.SaveChangesAsync(ct);
            return Results.Accepted($"/api/v1/m02/approvals/{approvalId}",
                new { order.SalesOrderId, status = "Pending approval", approvalRequestId = approvalId });
        }
        M06Rules.RequireTransition(order.Status, "Confirmed");
        order.Status = "Confirmed";
        order.ClosureReason = null;
        order.ShortClosedAt = null;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        order.ApprovalRequestId = approvalId;
        order.ApprovalSnapshotJson = snapshot.GetRawText();
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Short-close reversed",
            new { approvalId, request.Reason }, order.UpdatedAt));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { order.SalesOrderId, order.Status, order.Version });
    }

    private static async Task<IResult> Transition(
        Guid orderId, StatusWrite request, string target, string permission,
        TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", permission, ct);
        var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        if (order is null || !await InScope(authorization, db, actor.UserId, permission, order, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The order changed. Reload and retry.", currentVersion = order.Version });
        try { M06Rules.RequireTransition(order.Status, target); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { detail = ex.Message }); }
        var before = order.Status;
        order.Status = target;
        order.ClosureReason = request.Reason.Trim();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        if (target == "Cancelled") order.CancelledAt = order.UpdatedAt;
        if (target == "Short-closed") order.ShortClosedAt = order.UpdatedAt;
        if (target is "Cancelled" or "Short-closed")
        {
            var uncommittedSchedules = await db.Set<M06DeliverySchedule>()
                .Where(x => x.SalesOrderId == orderId &&
                    (x.Status == "Planned" || x.Status == "Pending approval"))
                .ToListAsync(ct);
            foreach (var schedule in uncommittedSchedules)
                schedule.Status = "Cancelled";
        }
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, target, new { before, reason = order.ClosureReason }, order.UpdatedAt));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The order was changed concurrently. Reload and retry." });
        }
        return Results.Ok(new { order.SalesOrderId, order.Number, order.Status, order.Version });
    }

    private static async Task<IResult> ProposeAmendment(
        Guid orderId, AmendmentWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, IM02ApprovalEngine approvals, ICurrentTenantContext tenant,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.amend", ct);
        var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        if (order is null || !await InScope(authorization, db, actor.UserId, "m06.orders.amend", order, ct))
            return Results.NotFound();
        if (order.Status is not ("Confirmed" or "In fulfilment" or "On hold"))
            return Results.Conflict(new { detail = "Only confirmed orders can be amended." });
        if (request.Version != order.Version)
            return Results.Conflict(new { detail = "The order changed. Reload before amending.", currentVersion = order.Version });
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("An amendment reason is required.");
        if (request.Lines is null || request.Lines.Length > 100 ||
            request.Lines.Select(x => x.LineId).Distinct().Count() != request.Lines.Length)
            throw new ValidationException("An amendment may contain up to 100 distinct order lines.");
        var lines = await db.Set<M06SalesOrderLine>().Where(x => x.SalesOrderId == orderId).ToListAsync(ct);
        var scheduledByLine = await db.Set<M06DeliverySchedule>().Where(x =>
                x.SalesOrderId == orderId &&
                (x.Status == "Pending approval" || x.Status == "Planned" || x.Status == "Released"))
            .GroupBy(x => x.SalesOrderLineId)
            .Select(x => new { LineId = x.Key, Quantity = x.Sum(row => row.Quantity) })
            .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);
        foreach (var change in request.Lines)
        {
            var line = lines.SingleOrDefault(x => x.LineId == change.LineId)
                ?? throw new ValidationException("An amendment line does not belong to this order.");
            var changesCommercialTerms =
                (change.OrderedQuantity is { } requestedQuantity && requestedQuantity != line.OrderedQuantity) ||
                (change.Rate is { } requestedRate && requestedRate != line.Rate);
            if (changesCommercialTerms && (line.DeliveredQuantity > 0 || line.InvoicedQuantity > 0))
                throw new ValidationException("A line with delivered or invoiced quantity cannot be commercially amended in place.");
            if (change.OrderedQuantity is { } proposedQuantity &&
                proposedQuantity != line.OrderedQuantity &&
                proposedQuantity < scheduledByLine.GetValueOrDefault(line.LineId))
                throw new ValidationException("Amended quantity cannot be less than an existing delivery schedule commitment.");
            if (change.OrderedQuantity is <= 0 || change.OrderedQuantity is { } quantity &&
                (decimal.Round(quantity, 3) != quantity || quantity < line.DeliveredQuantity || quantity < line.InvoicedQuantity))
                throw new ValidationException("Amended quantity must be positive, correctly rounded and not below fulfilled quantities.");
            if (change.Rate is < 0 || change.Rate is { } rate && decimal.Round(rate, 4) != rate)
                throw new ValidationException("Amended rate must be non-negative with up to four decimals.");
        }
        if (request.SiteId is { } siteId && !await db.Set<M03Site>().AnyAsync(x =>
                x.SiteId == siteId && x.CustomerPartyId == order.CustomerPartyId && x.Status == "Active", ct))
            throw new ValidationException("Choose an active site belonging to the order customer.");
        var before = JsonSerializer.Serialize(new
        {
            order.SiteId, order.SiteAddressSnapshot,
            lines = lines.Select(x => new { x.LineId, x.OrderedQuantity, x.Rate, x.Total })
        }, Json);
        var proposed = JsonSerializer.Serialize(new
        {
            siteId = request.SiteId ?? order.SiteId,
            lines = lines.Select(x =>
            {
                var change = request.Lines.SingleOrDefault(c => c.LineId == x.LineId);
                return new
                {
                    x.LineId, orderedQuantity = change?.OrderedQuantity ?? x.OrderedQuantity,
                    rate = change?.Rate ?? x.Rate
                };
            })
        }, Json);
        var amendmentId = Guid.NewGuid();
        var revision = await db.Set<M06Amendment>().Where(x => x.SalesOrderId == orderId)
            .Select(x => (int?)x.RevisionNumber).MaxAsync(ct) ?? 0;
        var snapshot = JsonSerializer.SerializeToElement(new
        {
            amendmentId, orderId, reason = request.Reason.Trim(),
            before = JsonDocument.Parse(before).RootElement,
            proposed = JsonDocument.Parse(proposed).RootElement
        }, Json);
        var approvalId = await approvals.Submit(new ApprovalSubmission(
            "M06SalesOrderAmendment", amendmentId, order.BranchId, null, actor.UserId,
            order.Total, null, null, order.CreditStatus, snapshot), ct);
        if (approvalId is null)
            return Results.Conflict(new { detail = "No M02 approval rule matched the amendment. No amendment was created." });
        var amendment = new M06Amendment
        {
            TenantId = tenant.TenantId, AmendmentId = amendmentId, SalesOrderId = orderId,
            RevisionNumber = revision + 1, Reason = request.Reason.Trim(),
            BeforeSnapshotJson = before, ProposedSnapshotJson = proposed,
            ApprovalRequestId = approvalId, RequestedByUserId = actor.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Add(amendment);
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Amendment proposed",
            new { amendmentId, amendment.RevisionNumber, amendment.Reason, approvalId }, amendment.CreatedAt));
        await db.SaveChangesAsync(ct);
        return Results.Accepted($"/api/v1/m02/approvals/{approvalId}",
            new { amendment.AmendmentId, amendment.RevisionNumber, amendment.Status,
                amendment.ApprovalRequestId, before = JsonDocument.Parse(before).RootElement,
                proposed = JsonDocument.Parse(proposed).RootElement });
    }

    private static async Task<IResult> ApplyAmendment(
        Guid orderId, Guid amendmentId, ApplyAmendmentWrite request,
        TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.amend", ct);
        var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        var amendment = await db.Set<M06Amendment>().SingleOrDefaultAsync(x =>
            x.AmendmentId == amendmentId && x.SalesOrderId == orderId, ct);
        if (order is null || amendment is null ||
            !await InScope(authorization, db, actor.UserId, "m06.orders.amend", order, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The order changed. Reload before applying the amendment." });
        var approval = amendment.ApprovalRequestId is { } id
            ? await db.Set<M02ApprovalRequest>().AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == id, ct)
            : null;
        if (approval?.Status != "Approved")
            return Results.Conflict(new { detail = "Only an approved amendment can be applied." });
        if (amendment.Status != "Pending approval")
            return Results.Conflict(new { detail = "The amendment is not pending application." });

        using var proposal = JsonDocument.Parse(amendment.ProposedSnapshotJson);
        var root = proposal.RootElement;
        var siteId = root.GetProperty("siteId").ValueKind == JsonValueKind.Null
            ? (Guid?)null : root.GetProperty("siteId").GetGuid();
        if (siteId != order.SiteId)
        {
            var site = siteId is { } newSite
                ? await db.Set<M03Site>().SingleAsync(x =>
                    x.SiteId == newSite && x.CustomerPartyId == order.CustomerPartyId, ct)
                : null;
            order.SiteId = site?.SiteId;
            order.SiteAddressSnapshot = site is null ? "" : string.Join(", ",
                new[] { site.Address, site.City, site.StateCode }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var uncommittedSchedules = await db.Set<M06DeliverySchedule>().Where(x =>
                    x.SalesOrderId == orderId &&
                    (x.Status == "Pending approval" || x.Status == "Planned"))
                .ToListAsync(ct);
            foreach (var schedule in uncommittedSchedules)
                schedule.SiteId = siteId;
        }
        var lines = await db.Set<M06SalesOrderLine>().Where(x => x.SalesOrderId == orderId).ToListAsync(ct);
        var scheduledByLine = await db.Set<M06DeliverySchedule>().Where(x =>
                x.SalesOrderId == orderId &&
                (x.Status == "Pending approval" || x.Status == "Planned" || x.Status == "Released"))
            .GroupBy(x => x.SalesOrderLineId)
            .Select(x => new { LineId = x.Key, Quantity = x.Sum(row => row.Quantity) })
            .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);
        foreach (var proposed in root.GetProperty("lines").EnumerateArray())
        {
            var line = lines.Single(x => x.LineId == proposed.GetProperty("lineId").GetGuid());
            var quantity = proposed.GetProperty("orderedQuantity").GetDecimal();
            var rate = proposed.GetProperty("rate").GetDecimal();
            if (quantity == line.OrderedQuantity && rate == line.Rate)
                continue;
            if (line.DeliveredQuantity > 0 || line.InvoicedQuantity > 0)
                return Results.Conflict(new { detail = "Order facts changed after proposal; create a new amendment." });
            if (quantity != line.OrderedQuantity &&
                quantity < scheduledByLine.GetValueOrDefault(line.LineId))
                return Results.Conflict(new { detail = "The approved quantity is below an existing schedule commitment." });
            using var priorTaxSnapshot = JsonDocument.Parse(line.TaxSnapshotJson);
            line.OrderedQuantity = quantity;
            line.Rate = rate;
            var amounts = M05QuotationCalculator.CalculateLine(quantity, rate,
                line.DiscountPercent, line.GstPercent, line.CgstAmount > 0 || line.SgstAmount > 0);
            line.TaxableAmount = amounts.TaxableAmount;
            line.CgstAmount = amounts.CgstAmount;
            line.SgstAmount = amounts.SgstAmount;
            line.IgstAmount = amounts.IgstAmount;
            line.TaxAmount = amounts.TaxAmount;
            line.Total = amounts.Total;
            line.TaxSnapshotJson = JsonSerializer.Serialize(new
            {
                priorSnapshot = priorTaxSnapshot.RootElement,
                amendmentId, rate, quantity, amounts.TaxableAmount,
                amounts.CgstAmount, amounts.SgstAmount, amounts.IgstAmount, amounts.TaxAmount
            }, Json);
        }
        order.Subtotal = lines.Sum(x => x.TaxableAmount + x.GrossAmount() - x.TaxableAmount);
        order.DiscountTotal = lines.Sum(x => x.GrossAmount() - x.TaxableAmount);
        var charges = await db.Set<M06SalesOrderCharge>().Where(x => x.SalesOrderId == orderId).ToListAsync(ct);
        order.ChargesTotal = charges.Sum(x => x.Amount);
        order.TaxTotal = lines.Sum(x => x.TaxAmount) + charges.Sum(x => x.TaxAmount);
        order.Total = lines.Sum(x => x.Total) + charges.Sum(x => x.Total);
        order.Version++;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        amendment.Status = "Approved";
        db.Add(Activity(tenant.TenantId, actor.UserId, orderId, "Approved amendment applied",
            new { amendmentId, amendment.RevisionNumber }, order.UpdatedAt));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The order was changed concurrently. Reload and retry." });
        }
        return Results.Ok(new { order.SalesOrderId, order.Number, order.Version, amendmentId, order.Total });
    }

    private static async Task<IResult> Confirmation(
        Guid orderId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M06", "m06.orders.view", ct);
        var order = await db.Set<M06SalesOrder>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct);
        if (order is null || !await InScope(authorization, db, actor.UserId, "m06.orders.view", order, ct))
            return Results.NotFound();
        return Results.Content(ConfirmationHtml(order), "text/html; charset=utf-8", Encoding.UTF8);
    }

    private static async Task<IResult> PendingReport(
        Guid? customerId, Guid? siteId, Guid? productId, Guid? branchId, string? format,
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        var export = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);
        var permission = export ? "m06.orders.export" : "m06.orders.report";
        await authorization.Require("M06", permission, ct);
        var scopes = await authorization.Scopes(actor.UserId, permission, ct);
        var query = db.Set<M06SalesOrder>().AsNoTracking()
            .Where(x => (x.Status == "Confirmed" || x.Status == "In fulfilment" || x.Status == "On hold"));
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId != null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId != null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x => own && x.SalesOwnerUserId == actor.UserId ||
                                     x.BranchId != null && branches.Contains(x.BranchId.Value) ||
                                     plants.Length > 0 && db.Set<M06DeliverySchedule>().Any(schedule =>
                                         schedule.SalesOrderId == x.SalesOrderId &&
                                         schedule.PreferredPlantId != null &&
                                         plants.Contains(schedule.PreferredPlantId.Value)));
        }
        if (customerId.HasValue) query = query.Where(x => x.CustomerPartyId == customerId);
        if (siteId.HasValue) query = query.Where(x => x.SiteId == siteId);
        if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId);
        var orders = await query.OrderBy(x => x.OrderDate).ToListAsync(ct);
        var lineQuery = db.Set<M06SalesOrderLine>().AsNoTracking()
            .Where(line => orders.Select(x => x.SalesOrderId).Contains(line.SalesOrderId));
        if (productId.HasValue) lineQuery = lineQuery.Where(x => x.ProductId == productId);
        var lines = await lineQuery.ToListAsync(ct);
        var lineIds = lines.Select(x => x.LineId).ToArray();
        var scheduledByLine = await db.Set<M06DeliverySchedule>().AsNoTracking()
            .Where(schedule => lineIds.Contains(schedule.SalesOrderLineId) &&
                (schedule.Status == "Planned" || schedule.Status == "Released"))
            .GroupBy(schedule => schedule.SalesOrderLineId)
            .Select(group => new { LineId = group.Key, Quantity = group.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);
        var rows = orders.SelectMany(order => lines.Where(line => line.SalesOrderId == order.SalesOrderId)
            .Select(line => new
            {
                order.SalesOrderId, order.Number, order.OrderDate, order.CustomerPartyId,
                order.CustomerNameSnapshot, order.SiteId, line.ProductId,
                line.ProductNameSnapshot, line.UnitCode, line.OrderedQuantity,
                scheduledQuantity = scheduledByLine.GetValueOrDefault(line.LineId),
                pendingQuantity = Math.Max(0m, line.OrderedQuantity - line.DeliveredQuantity),
                pendingValue = line.OrderedQuantity <= 0
                    ? 0m
                    : decimal.Round(line.Total * Math.Max(0m, line.OrderedQuantity - line.DeliveredQuantity) /
                        line.OrderedQuantity, 2, MidpointRounding.AwayFromZero)
            })).ToArray();
        var canViewCost = await authorization.CanAccess("M06", actor.UserId, "global.view_cost",
            null, null, null, ct, write: false);
        if (export)
        {
            var csv = new StringBuilder("Order,Date,Customer,Site,Product,Unit,Ordered,Scheduled,Remaining,Value\r\n");
            foreach (var row in rows)
                csv.Append(Csv(row.Number)).Append(',').Append(row.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    .Append(',').Append(Csv(row.CustomerNameSnapshot)).Append(',').Append(row.SiteId?.ToString() ?? "")
                    .Append(',').Append(Csv(row.ProductNameSnapshot)).Append(',').Append(Csv(row.UnitCode))
                    .Append(',').Append(row.OrderedQuantity.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(row.scheduledQuantity.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(row.pendingQuantity.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(row.pendingValue.ToString("0.00", CultureInfo.InvariantCulture)).Append("\r\n");
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8",
                "m06-pending-orders.csv");
        }
        return Results.Ok(new
        {
            rows, total = rows.Length,
            valueVisibility = canViewCost ? "Cost permission present; only order sales values are included." :
                "Sales values only; cost and margin are hidden.",
            deliveredQuantityState = "Unavailable until M08 is implemented.",
            export = "/api/v1/m06/reports/pending-orders?format=csv"
        });
    }

    private static string ConfirmationHtml(M06SalesOrder order)
    {
        static string H(string? value) => HtmlEncoder.Default.Encode(value ?? "");
        return $"""
            <!doctype html><html lang="en"><meta charset="utf-8"><title>Sales order {H(order.Number)}</title>
            <body><main><h1>Sales Order {H(order.Number)}</h1>
            <p>Status: {H(order.Status)} · Date: {order.OrderDate:yyyy-MM-dd}</p>
            <p>Customer: {H(order.CustomerNameSnapshot)}</p>
            <p>Customer PO: {H(order.CustomerPoNumber)}</p>
            <p>Site: {H(order.SiteAddressSnapshot)}</p>
            <p>Source quotation: {H(order.SourceQuotationNumber)}</p>
            <p>Payment terms: {H(order.PaymentTermsSnapshot)}</p>
            <p>Delivery terms: {H(order.DeliveryTermsSnapshot)}</p>
            <p>Total: INR {order.Total.ToString("N2", CultureInfo.InvariantCulture)}</p>
            <p>This confirmation uses the stored order snapshot. It is not a digitally signed document.</p>
            </main></body></html>
            """;
    }

    private static object ModulePending(IReadOnlyList<M06LinkedDocument> documents,
        string module, string type)
    {
        var found = documents.Where(x => x.SourceModule == module && x.DocumentType == type).ToArray();
        if (found.Length == 0)
            return new
            {
                state = "unavailable",
                reason = $"{module} is not implemented; no {type} records are available."
            };
        return new { state = "available", reason = (string?)null, documents = found };
    }

    private static async Task<bool> InScope(
        TenantAuthorizationService authorization, BerynxDbContext db, Guid userId, string permission,
        M06SalesOrder order, CancellationToken ct)
    {
        var write = !(permission.EndsWith(".view", StringComparison.Ordinal) ||
                      permission.EndsWith(".report", StringComparison.Ordinal));
        if (await authorization.CanAccess("M06", userId, permission, order.BranchId, null,
                order.SalesOwnerUserId, ct, write: write))
            return true;
        var scopes = await authorization.Scopes(userId, permission, ct);
        var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId != null)
            .Select(x => x.ScopeId!.Value).ToArray();
        return plants.Length > 0 && await db.Set<M06DeliverySchedule>().AnyAsync(x =>
            x.SalesOrderId == order.SalesOrderId && x.PreferredPlantId != null &&
            plants.Contains(x.PreferredPlantId.Value), ct);
    }

    private static M06Activity Activity(Guid tenantId, Guid actorId, Guid orderId,
        string action, object detail, DateTimeOffset at) => new()
    {
        TenantId = tenantId, ActivityId = Guid.NewGuid(), SalesOrderId = orderId,
        ActorUserId = actorId, Action = action,
        DetailJson = JsonSerializer.Serialize(detail, Json), At = at
    };

    private static string DeriveOrderType(IEnumerable<M06SalesOrderLine> lines)
    {
        var categories = lines.Select(x => x.ProductCategorySnapshot).ToArray();
        if (categories.Length == 0) throw new ValidationException("Add at least one order line.");
        var hasRmc = categories.Any(x => x.Equals("RMC", StringComparison.OrdinalIgnoreCase));
        return hasRmc && categories.Any(x => !x.Equals("RMC", StringComparison.OrdinalIgnoreCase))
            ? "Mixed" : hasRmc ? "RMC" : "Material";
    }

    private static decimal ToleranceFor(string category, Setup setup) =>
        category.Equals("RMC", StringComparison.OrdinalIgnoreCase)
            ? setup.M06RmcOverdeliveryTolerancePercent
            : category.Contains("steel", StringComparison.OrdinalIgnoreCase) ||
              category.Contains("tmt", StringComparison.OrdinalIgnoreCase)
                ? setup.M06SteelOverdeliveryTolerancePercent : 0m;

    private static void ValidateDirect(DirectOrderWrite request)
    {
        if (request.Lines is null || request.Lines.Length is < 1 or > 100 ||
            request.Charges is null || request.Charges.Length > 100)
            throw new ValidationException("An order requires 1–100 lines and up to 100 charges.");
        if (request.OrderType is not ("RMC" or "Material" or "Mixed"))
            throw new ValidationException("Order type must be RMC, Material or Mixed.");
        if (request.RateValidityUntil is not { } validity || validity < request.OrderDate)
            throw new ValidationException("A rate-validity date on or after the order date is required.");
        if (request.PriceBasis is not ("ExPlant" or "Delivered" or "Site"))
            throw new ValidationException("Price basis must be ExPlant, Delivered or Site.");
        if (request.Lines.Any(x => x.Quantity <= 0 || decimal.Round(x.Quantity, 3) != x.Quantity ||
            x.DiscountPercent is < 0 or > 100 || decimal.Round(x.DiscountPercent, 4) != x.DiscountPercent))
            throw new ValidationException("Line quantities must be positive to 3 decimals; discounts must be 0–100 to 4 decimals.");
        if (request.Lines.Any(x => x.Rate is < 0 || x.Rate is { } rate && decimal.Round(rate, 4) != rate))
            throw new ValidationException("Rates must be non-negative with up to four decimals.");
        if (request.OrderDate == default || string.IsNullOrWhiteSpace(request.PaymentTerms) ||
            string.IsNullOrWhiteSpace(request.DeliveryTerms) ||
            string.IsNullOrWhiteSpace(request.PlaceOfSupplyStateCode))
            throw new ValidationException("Order date, payment terms, delivery terms and place of supply are required.");
        if (request.Charges.Any(x => string.IsNullOrWhiteSpace(x.Description) ||
            x.Description.Length > 200 || x.Amount < 0 || decimal.Round(x.Amount, 2) != x.Amount))
            throw new ValidationException("Charges require a description and non-negative two-decimal amount.");
        if (request.Notes?.Length > 4000 || request.PaymentTerms?.Length > 2000 ||
            request.DeliveryTerms?.Length > 2000 ||
            request.CustomerPoAttachmentReference?.Length > 500)
            throw new ValidationException("An order text field exceeds its maximum length.");
    }

    private static void ValidateIdempotency(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 80)
            throw new ValidationException("A request idempotency key of 1–80 characters is required.");
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<string> AllocateOrderNumber(
        BerynxDbContext db, Guid tenantId, int year, CancellationToken ct)
    {
        var sequence = await db.Database.SqlQuery<long>($"""
            INSERT INTO m04_number_counter ("TenantId", "DocumentCode", "CalendarYear", "NextNumber")
            VALUES ({tenantId}, {"SalesOrder"}, {year}, 2)
            ON CONFLICT ("TenantId", "DocumentCode", "CalendarYear")
            DO UPDATE SET "NextNumber" = m04_number_counter."NextNumber" + 1
            RETURNING "NextNumber" - 1 AS "Value"
            """).SingleAsync(ct);
        return $"SO-{year}-{sequence:D6}";
    }

    private static JsonElement ParseJson(string value)
    {
        using var document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static string Csv(string? value) =>
        "\"" + (value ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}

internal static class M06LineAmounts
{
    public static decimal GrossAmount(this M06SalesOrderLine line) =>
        decimal.Round(line.OrderedQuantity * line.Rate, 2, MidpointRounding.AwayFromZero);

    public static decimal DiscountAmount(this M06SalesOrderLine line) =>
        decimal.Round(line.GrossAmount() * line.DiscountPercent / 100m, 2,
            MidpointRounding.AwayFromZero);
}
