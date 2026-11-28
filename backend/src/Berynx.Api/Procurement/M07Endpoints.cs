using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.Crm;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Procurement;
using Berynx.Domain.SalesOrders;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Procurement;

public static class M07Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public sealed record PurchaseOrderLineWrite(
        Guid ProductId, Guid? SourceSalesOrderLineId, decimal Quantity, decimal Rate);
    public sealed record PurchaseOrderChargeWrite(string Description, decimal Amount, decimal GstPercent);
    public sealed record PurchaseOrderWrite(
        string IdempotencyKey, Guid BranchId, Guid SupplierPartyId, Guid? SourceSalesOrderId,
        Guid? SiteId, DateOnly OrderDate, DateOnly? ExpectedDate, string PaymentTerms,
        string DeliveryTerms, string? Notes, PurchaseOrderLineWrite[] Lines,
        PurchaseOrderChargeWrite[] Charges);
    public sealed record ReceiptToleranceWrite(long Version, decimal SteelPercent, decimal CementPercent);
    public sealed record VersionWrite(long Version, string? Reason);
    public sealed record ReceiptLineWrite(
        Guid PurchaseOrderLineId, decimal ReceivedQuantity, decimal DamagedQuantity,
        decimal ShortQuantity, string? ExceptionReason, string? LotOrHeatNumber,
        string? ManufacturerName, string? MtcReference);
    public sealed record GoodsReceiptWrite(
        string IdempotencyKey, DateOnly ReceiptDate, string? VehicleNumber, string? DriverName,
        string? WeighbridgeSlipNumber, decimal? GrossWeight, decimal? TareWeight,
        string? AttachmentReference, string? Remarks, ReceiptLineWrite[] Lines);

    public static void MapM07(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m07")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices
                .GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M07",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });

        api.MapGet("/purchase-orders", ListPurchaseOrders);
        api.MapGet("/purchase-orders/{purchaseOrderId:guid}", GetPurchaseOrder);
        api.MapPost("/purchase-orders", CreatePurchaseOrder);
        api.MapPost("/purchase-orders/{purchaseOrderId:guid}/request-approval", RequestPurchaseOrderApproval);
        api.MapPost("/purchase-orders/{purchaseOrderId:guid}/release", ReleasePurchaseOrder);
        api.MapPost("/purchase-orders/{purchaseOrderId:guid}/supplier-acknowledgement", RecordSupplierAcknowledgement);
        api.MapPost("/purchase-orders/{purchaseOrderId:guid}/cancel", CancelPurchaseOrder);
        api.MapPost("/purchase-orders/{purchaseOrderId:guid}/close", ClosePurchaseOrder);
        api.MapGet("/purchase-rates", PurchaseRateHistory);
        api.MapGet("/settings/receipt-tolerances", GetReceiptTolerances);
        api.MapPut("/settings/receipt-tolerances", UpdateReceiptTolerances);
        api.MapGet("/goods-receipts", ListGoodsReceipts);
        api.MapGet("/goods-receipts/{goodsReceiptId:guid}", GetGoodsReceipt);
        api.MapPost("/purchase-orders/{purchaseOrderId:guid}/goods-receipts", CreateGoodsReceipt);
        api.MapPost("/goods-receipts/{goodsReceiptId:guid}/post", PostGoodsReceipt);
    }

    private static async Task<IResult> ListPurchaseOrders(
        int page, int pageSize, string? status, Guid? supplierPartyId, Guid? salesOrderId,
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.purchase_orders.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m07.purchase_orders.view", ct);
        var query = db.Set<M07PurchaseOrder>().AsNoTracking();
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x => (own && x.CreatedByUserId == actor.UserId) ||
                                     (x.BranchId.HasValue && branches.Contains(x.BranchId.Value)));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (supplierPartyId is { } supplier)
            query = query.Where(x => x.SupplierPartyId == supplier);
        if (salesOrderId is { } sourceOrder)
            query = query.Where(x => x.SourceSalesOrderId == sourceOrder);
        var total = await query.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var costScopes = await authorization.Scopes(actor.UserId, "global.view_cost", ct);
        var rows = await query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.Number)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.PurchaseOrderId, x.Number, x.OrderDate, x.BranchId, x.CreatedByUserId, x.SupplierPartyId,
                x.SupplierNameSnapshot, x.SourceSalesOrderId, x.SiteId, x.Status,
                x.ExpectedDate, x.Total, x.Version
            }).ToListAsync(ct);
        var visibleRows = rows.Select(x => new
        {
            x.PurchaseOrderId, x.Number, x.OrderDate, x.SupplierPartyId, x.SupplierNameSnapshot,
            x.SourceSalesOrderId, x.SiteId, x.Status, x.ExpectedDate, x.Version,
            total = costScopes.Any(scope => scope.ScopeType switch
            {
                "All" => true,
                "Branch" => x.BranchId == scope.ScopeId,
                "Own" => x.CreatedByUserId == actor.UserId,
                _ => false
            }) ? (decimal?)x.Total : null
        }).ToList();
        return Results.Ok(new { rows = visibleRows, total, page, pageSize });
    }

    private static async Task<IResult> GetPurchaseOrder(
        Guid purchaseOrderId, TenantAuthorizationService authorization, BerynxDbContext db,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.purchase_orders.view", ct);
        var order = await db.Set<M07PurchaseOrder>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId, ct);
        if (order is null || !await authorization.CanAccess("M07", actor.UserId,
                "m07.purchase_orders.view", order.BranchId, null, order.CreatedByUserId, ct, write: false))
            return Results.NotFound();
        var canViewCost = await authorization.CanAccess("M07", actor.UserId,
            "global.view_cost", order.BranchId, null, order.CreatedByUserId, ct, write: false);
        var lines = await db.Set<M07PurchaseOrderLine>().AsNoTracking()
            .Where(x => x.PurchaseOrderId == purchaseOrderId).ToListAsync(ct);
        var charges = await db.Set<M07PurchaseOrderCharge>().AsNoTracking()
            .Where(x => x.PurchaseOrderId == purchaseOrderId).ToListAsync(ct);
        var receipts = await db.Set<M07GoodsReceipt>().AsNoTracking()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(x => x.ReceiptDate).ToListAsync(ct);
        var invoices = await db.Set<M07SupplierInvoice>().AsNoTracking()
            .Where(x => x.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(x => x.InvoiceDate)
            .Select(x => new { x.SupplierInvoiceId, x.Number, x.SupplierInvoiceNumber, x.Status, x.Total, x.PaidAmount })
            .ToListAsync(ct);
        var visibleInvoices = canViewCost ? (object)invoices : invoices.Select(x => new
        {
            x.SupplierInvoiceId, x.Number, x.SupplierInvoiceNumber, x.Status,
            Total = (decimal?)null, PaidAmount = (decimal?)null
        }).ToList();
        var visibleLines = canViewCost ? (object)lines : lines.Select(x => new
        {
            x.PurchaseOrderLineId, x.PurchaseOrderId, x.SourceSalesOrderLineId,
            x.ProductId, x.ProductCodeSnapshot, x.ProductNameSnapshot, x.ProductCategorySnapshot,
            x.UnitCode, x.Traceable, x.OrderedQuantity, x.ReceivedQuantity,
            Rate = (decimal?)null, GstPercent = (decimal?)null,
            TaxableAmount = (decimal?)null, TaxAmount = (decimal?)null, Total = (decimal?)null
        }).ToList();
        return Results.Ok(new
        {
            order.PurchaseOrderId, order.Number, order.OrderDate, order.BranchId,
            order.SupplierPartyId, order.SupplierNameSnapshot, order.SupplierGstinSnapshot,
            order.SourceSalesOrderId, order.SiteId, order.Status, order.ExpectedDate,
            order.PaymentTermsSnapshot, order.DeliveryTermsSnapshot, order.Notes,
            subtotal = canViewCost ? order.Subtotal : (decimal?)null,
            chargesTotal = canViewCost ? order.ChargesTotal : (decimal?)null,
            taxTotal = canViewCost ? order.TaxTotal : (decimal?)null,
            total = canViewCost ? order.Total : (decimal?)null,
            order.ApprovalRequestId, order.Version, lines = visibleLines,
            charges = canViewCost ? charges : null, receipts, invoices = visibleInvoices
        });
    }

    private static async Task<IResult> CreatePurchaseOrder(
        PurchaseOrderWrite request, TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, OnboardingStore onboarding,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.purchase_orders.create", ct);
        ValidatePurchaseOrder(request);
        var duplicate = await db.Set<M07PurchaseOrder>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (duplicate is not null)
        {
            if (duplicate.BranchId != request.BranchId ||
                duplicate.SupplierPartyId != request.SupplierPartyId ||
                duplicate.SourceSalesOrderId != request.SourceSalesOrderId ||
                !await authorization.CanAccess("M07", actor.UserId, "m07.purchase_orders.view",
                    duplicate.BranchId, null, null, ct, write: false))
                return Results.Conflict(new { detail = "The idempotency key was already used for a different purchase order." });
            return Results.Ok(new { duplicate.PurchaseOrderId, duplicate.Number, duplicate.Status, duplicate = true });
        }
        if (!await authorization.CanAccess("M07", actor.UserId, "m07.purchase_orders.create",
                request.BranchId, null, actor.UserId, ct))
            throw new UnauthorizedAccessException("The requested branch is outside your assignment scope.");

        var supplier = await db.Set<M03Party>().AsNoTracking().SingleOrDefaultAsync(
            x => x.PartyId == request.SupplierPartyId && x.IsActive, ct)
            ?? throw new ValidationException("Choose an active M03 supplier.");
        if (!await db.Set<M03PartyRole>().AnyAsync(
                x => x.PartyId == supplier.PartyId && x.RoleCode == "Supplier", ct))
            throw new ValidationException("The selected party is not an M03 supplier.");
        var setup = OnboardingStore.Decode(await onboarding.Current());
        var branch = setup.Branches.SingleOrDefault(x => x.Id == request.BranchId)
            ?? throw new ValidationException("Choose a branch configured for this tenant.");
        var site = request.SiteId is { } siteId
            ? await db.Set<M03Site>().AsNoTracking().SingleOrDefaultAsync(
                x => x.SiteId == siteId && x.Status == "Active", ct)
              ?? throw new ValidationException("Choose an active delivery site.")
            : null;
        if (site?.BranchId is { } siteBranch && siteBranch != request.BranchId)
            throw new ValidationException("The delivery site belongs to another branch.");

        M06SalesOrder? sourceOrder = null;
        if (request.SourceSalesOrderId is { } orderId)
        {
            sourceOrder = await db.Set<M06SalesOrder>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.SalesOrderId == orderId, ct)
                ?? throw new ValidationException("The source sales order was not found.");
            if (sourceOrder.Status is not ("Confirmed" or "In fulfilment"))
                throw new ValidationException("A purchase order can only be raised from a confirmed sales order.");
            if (!await authorization.CanAccess("M06", actor.UserId, "m06.orders.view",
                    sourceOrder.BranchId, null, sourceOrder.SalesOwnerUserId, ct, write: false))
                return Results.NotFound();
            if (sourceOrder.BranchId != request.BranchId)
                throw new ValidationException("Purchase and source sales order must use the same branch.");
            if (site is not null && sourceOrder.SiteId != site.SiteId)
                throw new ValidationException("The purchase-order site must match the source sales order.");
        }
        else if (request.Lines.Any(x => x.SourceSalesOrderLineId is not null))
            throw new ValidationException("Source order lines require a source sales order.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var orderIdNew = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var order = new M07PurchaseOrder
        {
            TenantId = tenant.TenantId, PurchaseOrderId = orderIdNew,
            Number = await AllocateNumber(db, tenant.TenantId, request.BranchId, "PurchaseOrder",
                request.OrderDate, branch.Id, "PurchaseOrder", setup, ct),
            OrderDate = request.OrderDate, BranchId = request.BranchId,
            SupplierPartyId = supplier.PartyId,
            SupplierNameSnapshot = supplier.TradeName ?? supplier.LegalName,
            SupplierGstinSnapshot = supplier.Gstin ?? "",
            SourceSalesOrderId = sourceOrder?.SalesOrderId,
            SiteId = site?.SiteId ?? sourceOrder?.SiteId,
            Status = "Draft", ExpectedDate = request.ExpectedDate,
            PaymentTermsSnapshot = request.PaymentTerms.Trim(),
            DeliveryTermsSnapshot = request.DeliveryTerms.Trim(),
            Notes = request.Notes?.Trim() ?? "",
            IdempotencyKey = request.IdempotencyKey.Trim(),
            CreatedByUserId = actor.UserId,
            CreatedAt = now, UpdatedAt = now
        };
        var lines = new List<M07PurchaseOrderLine>();
        foreach (var input in request.Lines)
        {
            var product = await db.Set<M03Product>().AsNoTracking().SingleOrDefaultAsync(
                x => x.ProductId == input.ProductId && x.IsActive, ct)
                ?? throw new ValidationException("A selected product is inactive or unavailable.");
            M06SalesOrderLine? sourceLine = null;
            if (sourceOrder is not null)
            {
                if (input.SourceSalesOrderLineId is not { } sourceLineId)
                    throw new ValidationException("Every source purchase line must reference its M06 order line.");
                sourceLine = await db.Set<M06SalesOrderLine>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.LineId == sourceLineId && x.SalesOrderId == sourceOrder.SalesOrderId, ct)
                    ?? throw new ValidationException("A selected line does not belong to the source sales order.");
                if (sourceLine.ProductId != product.ProductId)
                    throw new ValidationException("The purchase product does not match its source sales-order line.");
                var alreadyOrdered = await (
                    from purchaseLine in db.Set<M07PurchaseOrderLine>().AsNoTracking()
                    join purchaseOrder in db.Set<M07PurchaseOrder>().AsNoTracking()
                        on new { purchaseLine.TenantId, purchaseLine.PurchaseOrderId } equals
                           new { purchaseOrder.TenantId, purchaseOrder.PurchaseOrderId }
                    where purchaseLine.SourceSalesOrderLineId == sourceLine.LineId &&
                          purchaseOrder.Status != "Cancelled"
                    select (decimal?)(purchaseOrder.Status == "Closed"
                        ? purchaseLine.ReceivedQuantity
                        : purchaseLine.OrderedQuantity)
                ).SumAsync(ct) ?? 0m;
                if (alreadyOrdered + input.Quantity > sourceLine.OrderedQuantity)
                    throw new ValidationException("Purchase quantity across open POs exceeds the source sales-order line.");
            }
            var tax = await ResolvePurchaseTax(db, product, supplier, branch, request.OrderDate, ct);
            var taxable = decimal.Round(input.Quantity * input.Rate, 2, MidpointRounding.AwayFromZero);
            var taxAmount = decimal.Round(taxable * tax.Rate / 100m, 2, MidpointRounding.AwayFromZero);
            lines.Add(new M07PurchaseOrderLine
            {
                TenantId = tenant.TenantId, PurchaseOrderLineId = Guid.NewGuid(),
                PurchaseOrderId = orderIdNew, SourceSalesOrderLineId = sourceLine?.LineId,
                ProductId = product.ProductId, ProductCodeSnapshot = product.Code,
                ProductNameSnapshot = product.Name, ProductCategorySnapshot = product.Category,
                UnitCode = product.BaseUnitCode, Traceable = product.Traceable,
                OrderedQuantity = input.Quantity, Rate = input.Rate, GstPercent = tax.Rate,
                TaxableAmount = taxable, TaxAmount = taxAmount, Total = taxable + taxAmount,
                TaxSnapshotJson = tax.Snapshot
            });
        }
        var charges = request.Charges.Select(input =>
        {
            var tax = decimal.Round(input.Amount * input.GstPercent / 100m, 2, MidpointRounding.AwayFromZero);
            return new M07PurchaseOrderCharge
            {
                TenantId = tenant.TenantId, ChargeId = Guid.NewGuid(), PurchaseOrderId = orderIdNew,
                Description = input.Description.Trim(), Amount = input.Amount, GstPercent = input.GstPercent,
                TaxAmount = tax, Total = input.Amount + tax
            };
        }).ToList();
        order.Subtotal = lines.Sum(x => x.TaxableAmount);
        order.ChargesTotal = charges.Sum(x => x.Amount);
        order.TaxTotal = lines.Sum(x => x.TaxAmount) + charges.Sum(x => x.TaxAmount);
        order.Total = lines.Sum(x => x.Total) + charges.Sum(x => x.Total);
        db.Add(order);
        db.AddRange(lines);
        db.AddRange(charges);
        db.Add(Activity(tenant.TenantId, actor.UserId, "PurchaseOrder", orderIdNew, "Draft created",
            new { order.Number, order.SourceSalesOrderId }, now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/m07/purchase-orders/{orderIdNew}",
            new { order.PurchaseOrderId, order.Number, order.Status, order.Version });
    }

    private static async Task<IResult> RequestPurchaseOrderApproval(
        Guid purchaseOrderId, VersionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, IM02ApprovalEngine approvals,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.purchase_orders.submit", ct);
        var order = await db.Set<M07PurchaseOrder>().SingleOrDefaultAsync(
            x => x.PurchaseOrderId == purchaseOrderId, ct);
        if (order is null || !await authorization.CanAccess("M07", actor.UserId,
                "m07.purchase_orders.submit", order.BranchId, null, order.CreatedByUserId, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The purchase order changed. Reload and retry.", order.Version });
        if (order.Status != "Draft")
            return Results.Conflict(new { detail = "Only a draft purchase order can be submitted." });
        var snapshot = JsonSerializer.SerializeToElement(new
        {
            order.PurchaseOrderId, order.Number, order.SupplierPartyId, order.Total,
            lines = await db.Set<M07PurchaseOrderLine>().AsNoTracking()
                .Where(x => x.PurchaseOrderId == purchaseOrderId)
                .Select(x => new { x.ProductId, x.ProductNameSnapshot, x.OrderedQuantity, x.Rate, x.Total })
                .ToListAsync(ct)
        }, Json);
        var approvalId = await approvals.Submit(new ApprovalSubmission(
            "M07PurchaseOrder", order.PurchaseOrderId, order.BranchId, null, actor.UserId,
            order.Total, null, null, null, snapshot), ct);
        if (approvalId is null)
            return Results.Conflict(new { detail = "No matching M02 purchase-order approval rule is configured." });
        M07Rules.RequirePurchaseOrderTransition(order.Status, "Pending approval");
        order.Status = "Pending approval";
        order.ApprovalRequestId = approvalId;
        order.ApprovalSnapshotJson = snapshot.GetRawText();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        db.Add(Activity(tenant.TenantId, actor.UserId, "PurchaseOrder", purchaseOrderId,
            "Approval requested", new { approvalId, request.Reason }, order.UpdatedAt));
        await db.SaveChangesAsync(ct);
        return Results.Accepted($"/api/v1/m02/approvals/{approvalId}",
            new { order.PurchaseOrderId, order.Number, order.Status, order.ApprovalRequestId, order.Version });
    }

    private static async Task<IResult> ReleasePurchaseOrder(
        Guid purchaseOrderId, VersionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.purchase_orders.approve", ct);
        var order = await db.Set<M07PurchaseOrder>().SingleOrDefaultAsync(
            x => x.PurchaseOrderId == purchaseOrderId, ct);
        if (order is null || !await authorization.CanAccess("M07", actor.UserId,
                "m07.purchase_orders.approve", order.BranchId, null, order.CreatedByUserId, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The purchase order changed. Reload and retry.", order.Version });
        var approval = order.ApprovalRequestId is { } approvalId
            ? await db.Set<M02ApprovalRequest>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.RequestId == approvalId, ct)
            : null;
        if (order.Status != "Pending approval" || approval?.Status != "Approved")
            return Results.Conflict(new { detail = "An approved M02 purchase-order request is required before release." });
        M07Rules.RequirePurchaseOrderTransition(order.Status, "Approved");
        order.Status = "Approved";
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        db.Add(Activity(tenant.TenantId, actor.UserId, "PurchaseOrder", purchaseOrderId,
            "Approved and released", new { approval.RequestId }, order.UpdatedAt));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { order.PurchaseOrderId, order.Number, order.Status, order.Version });
    }

    private static async Task<IResult> RecordSupplierAcknowledgement(
        Guid purchaseOrderId, VersionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.purchase_orders.send", ct);
        var order = await db.Set<M07PurchaseOrder>().SingleOrDefaultAsync(
            x => x.PurchaseOrderId == purchaseOrderId, ct);
        if (order is null || !await authorization.CanAccess("M07", actor.UserId,
                "m07.purchase_orders.send", order.BranchId, null, order.CreatedByUserId, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The purchase order changed. Reload and retry.", order.Version });
        if (order.Status != "Approved")
            return Results.Conflict(new { detail = "Only an approved purchase order can be marked as sent." });
        M07Rules.RequirePurchaseOrderTransition(order.Status, "Sent");
        order.Status = "Sent";
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        db.Add(Activity(tenant.TenantId, actor.UserId, "PurchaseOrder", purchaseOrderId,
            "Supplier acknowledgement recorded", new
            {
                communicationState = "External delivery provider not connected",
                acknowledgement = request.Reason
            }, order.UpdatedAt));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            order.PurchaseOrderId, order.Number, order.Status, order.Version,
            communicationState = "Provider unavailable; acknowledgement entered by an authorized user."
        });
    }

    private static Task<IResult> CancelPurchaseOrder(
        Guid purchaseOrderId, VersionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        ChangePurchaseOrderStatus(purchaseOrderId, request, "Cancelled", "m07.purchase_orders.cancel",
            authorization, db, tenant, ct);

    private static Task<IResult> ClosePurchaseOrder(
        Guid purchaseOrderId, VersionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        ChangePurchaseOrderStatus(purchaseOrderId, request, "Closed", "m07.purchase_orders.close",
            authorization, db, tenant, ct);

    private static async Task<IResult> ChangePurchaseOrderStatus(
        Guid purchaseOrderId, VersionWrite request, string target, string permission,
        TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", permission, ct);
        var order = await db.Set<M07PurchaseOrder>().SingleOrDefaultAsync(
            x => x.PurchaseOrderId == purchaseOrderId, ct);
        if (order is null || !await authorization.CanAccess("M07", actor.UserId,
                permission, order.BranchId, null, order.CreatedByUserId, ct))
            return Results.NotFound();
        if (order.Version != request.Version)
            return Results.Conflict(new { detail = "The purchase order changed. Reload and retry.", order.Version });
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            throw new ValidationException("A reason up to 1,000 characters is required.");
        if (target == "Cancelled" &&
            await db.Set<M07GoodsReceipt>().AnyAsync(x =>
                x.PurchaseOrderId == purchaseOrderId && x.Status == "Posted", ct))
            return Results.Conflict(new { detail = "A purchase order with posted receipts cannot be cancelled; close its unreceived balance instead." });
        if (target == "Closed" && order.Status is not ("Partially received" or "Received"))
            return Results.Conflict(new { detail = "Only an order with receipt activity can be closed." });
        try { M07Rules.RequirePurchaseOrderTransition(order.Status, target); }
        catch (InvalidOperationException ex) { return Results.Conflict(new { detail = ex.Message }); }
        order.Status = target;
        order.ClosureReason = request.Reason.Trim();
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.Version++;
        db.Add(Activity(tenant.TenantId, actor.UserId, "PurchaseOrder", purchaseOrderId,
            target, new { request.Reason }, order.UpdatedAt));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The purchase order changed concurrently. Reload and retry." });
        }
        return Results.Ok(new { order.PurchaseOrderId, order.Number, order.Status, order.Version });
    }

    private static async Task<IResult> PurchaseRateHistory(
        Guid supplierPartyId, Guid productId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "global.view_cost", ct);
        var scopes = await authorization.Scopes(actor.UserId, "global.view_cost", ct);
        if (!scopes.Any(x => x.ScopeType == "All"))
            return Results.NotFound();
        var rows = await (from line in db.Set<M07SupplierInvoiceLine>().AsNoTracking()
                          join invoice in db.Set<M07SupplierInvoice>().AsNoTracking()
                              on new { line.TenantId, line.SupplierInvoiceId } equals
                                 new { invoice.TenantId, invoice.SupplierInvoiceId }
                          where invoice.SupplierPartyId == supplierPartyId &&
                                line.ProductId == productId && invoice.Status == "Posted"
                          orderby invoice.InvoiceDate descending
                          select new
                          {
                              invoice.InvoiceDate, invoice.SupplierInvoiceNumber,
                              line.Quantity, line.UnitCode, line.Rate
                          }).Take(5).ToListAsync(ct);
        return Results.Ok(new { rows, state = rows.Count == 0 ? "No posted purchase rates" : "Actual posted supplier invoices" });
    }

    private static async Task<IResult> GetReceiptTolerances(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        await authorization.Require("M07", "m07.goods_receipts.view", ct);
        var tenant = await db.Set<Tenant>().AsNoTracking().SingleAsync(ct);
        var setup = OnboardingStore.Decode(tenant);
        return Results.Ok(new
        {
            version = tenant.Version,
            steelPercent = setup.M07SteelOverReceiptTolerancePercent,
            cementPercent = setup.M07CementOverReceiptTolerancePercent
        });
    }

    private static async Task<IResult> UpdateReceiptTolerances(
        ReceiptToleranceWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenantContext, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.goods_receipts.configure", ct);
        if (request.SteelPercent is < 0 or > 100 || request.CementPercent is < 0 or > 100 ||
            decimal.Round(request.SteelPercent, 4) != request.SteelPercent ||
            decimal.Round(request.CementPercent, 4) != request.CementPercent)
            throw new ValidationException("Receipt tolerances must be between 0 and 100 with up to four decimals.");
        var tenant = await db.Set<Tenant>().SingleAsync(ct);
        if (tenant.Version != request.Version)
            return Results.Conflict(new { detail = "Tenant settings changed. Reload before updating receipt tolerances." });
        var setup = OnboardingStore.Decode(tenant);
        var before = new
        {
            steelPercent = setup.M07SteelOverReceiptTolerancePercent,
            cementPercent = setup.M07CementOverReceiptTolerancePercent
        };
        setup.M07SteelOverReceiptTolerancePercent = request.SteelPercent;
        setup.M07CementOverReceiptTolerancePercent = request.CementPercent;
        tenant.Configuration = OnboardingStore.Encode(setup);
        tenant.Version++;
        var after = new
        {
            steelPercent = setup.M07SteelOverReceiptTolerancePercent,
            cementPercent = setup.M07CementOverReceiptTolerancePercent
        };
        db.Add(new M02AuditEvent
        {
            TenantId = tenantContext.TenantId, EventId = Guid.NewGuid(),
            ActorUserId = actor.UserId, ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString(),
            Action = "M07 goods-receipt tolerances updated", EntityType = "Tenant",
            EntityId = tenantContext.TenantId,
            Before = JsonSerializer.Serialize(before, Json), After = JsonSerializer.Serialize(after, Json),
            At = DateTimeOffset.UtcNow
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "Tenant settings changed concurrently. Reload and retry." });
        }
        return Results.Ok(new { version = tenant.Version, after.steelPercent, after.cementPercent });
    }

    private static async Task<IResult> ListGoodsReceipts(
        int page, int pageSize, string? status, Guid? supplierPartyId,
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.goods_receipts.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m07.goods_receipts.view", ct);
        var query = db.Set<M07GoodsReceipt>().AsNoTracking();
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x => (own && x.ReceivedByUserId == actor.UserId) ||
                                     branches.Contains(x.BranchId));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (supplierPartyId is { } supplier) query = query.Where(x => x.SupplierPartyId == supplier);
        var total = await query.CountAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await query.OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.Number)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.GoodsReceiptId, x.Number, x.ReceiptDate, x.PurchaseOrderId,
                x.SupplierPartyId, x.Status, x.VehicleNumber, x.NetWeight, x.Version
            }).ToListAsync(ct);
        return Results.Ok(new { rows, total, page, pageSize });
    }

    private static async Task<IResult> GetGoodsReceipt(
        Guid goodsReceiptId, TenantAuthorizationService authorization, BerynxDbContext db,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.goods_receipts.view", ct);
        var receipt = await db.Set<M07GoodsReceipt>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.GoodsReceiptId == goodsReceiptId, ct);
        if (receipt is null || !await authorization.CanAccess("M07", actor.UserId,
                "m07.goods_receipts.view", receipt.BranchId, null, receipt.ReceivedByUserId, ct, write: false))
            return Results.NotFound();
        var lines = await db.Set<M07GoodsReceiptLine>().AsNoTracking()
            .Where(x => x.GoodsReceiptId == goodsReceiptId).ToListAsync(ct);
        return Results.Ok(new { receipt, lines });
    }

    private static async Task<IResult> CreateGoodsReceipt(
        Guid purchaseOrderId, GoodsReceiptWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, OnboardingStore onboarding,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.goods_receipts.create", ct);
        ValidateGoodsReceipt(request);
        var duplicate = await db.Set<M07GoodsReceipt>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (duplicate is not null)
        {
            if (duplicate.PurchaseOrderId != purchaseOrderId ||
                !await authorization.CanAccess("M07", actor.UserId, "m07.goods_receipts.view",
                    duplicate.BranchId, null, duplicate.ReceivedByUserId, ct, write: false))
                return Results.Conflict(new { detail = "The idempotency key was already used for a different goods receipt." });
            return Results.Ok(new { duplicate.GoodsReceiptId, duplicate.Number, duplicate.Status, duplicate = true });
        }
        var order = await db.Set<M07PurchaseOrder>().SingleOrDefaultAsync(
            x => x.PurchaseOrderId == purchaseOrderId, ct);
        if (order is null || !await authorization.CanAccess("M07", actor.UserId,
                "m07.goods_receipts.create", order.BranchId, null, order.CreatedByUserId, ct))
            return Results.NotFound();
        if (order.Status is not ("Approved" or "Sent" or "Partially received"))
            return Results.Conflict(new { detail = "Only an approved, sent or partially received PO can receive goods." });
        var orderLines = await db.Set<M07PurchaseOrderLine>().Where(x =>
            x.PurchaseOrderId == purchaseOrderId).ToDictionaryAsync(x => x.PurchaseOrderLineId, ct);
        if (request.Lines.Any(x => !orderLines.ContainsKey(x.PurchaseOrderLineId)))
            throw new ValidationException("Every goods receipt line must match a line on the purchase order.");
        var receiptId = Guid.NewGuid();
        var branchId = order.BranchId ?? throw new ValidationException("A branch is required for goods receipt numbering.");
        var setup = OnboardingStore.Decode(await onboarding.Current());
        var branch = setup.Branches.SingleOrDefault(x => x.Id == branchId)
            ?? throw new ValidationException("The purchase order branch is not configured.");
        var now = DateTimeOffset.UtcNow;
        var receipt = new M07GoodsReceipt
        {
            TenantId = tenant.TenantId, GoodsReceiptId = receiptId,
            Number = await AllocateNumber(db, tenant.TenantId, branchId, "GoodsReceipt",
                request.ReceiptDate, branchId, "GoodsReceipt", setup, ct),
            ReceiptDate = request.ReceiptDate, BranchId = branchId,
            PurchaseOrderId = purchaseOrderId, SupplierPartyId = order.SupplierPartyId,
            SiteId = order.SiteId, Status = "Draft",
            VehicleNumber = EmptyToNull(request.VehicleNumber),
            DriverName = EmptyToNull(request.DriverName),
            WeighbridgeSlipNumber = EmptyToNull(request.WeighbridgeSlipNumber),
            GrossWeight = request.GrossWeight, TareWeight = request.TareWeight,
            NetWeight = request.GrossWeight is { } gross && request.TareWeight is { } tare ? gross - tare : null,
            AttachmentReference = EmptyToNull(request.AttachmentReference),
            Remarks = request.Remarks?.Trim() ?? "", ReceivedByUserId = actor.UserId,
            IdempotencyKey = request.IdempotencyKey.Trim(), CreatedAt = now
        };
        if (receipt.NetWeight is < 0)
            throw new ValidationException("Tare weight cannot exceed gross weight.");
        var lines = request.Lines.Select(x => new M07GoodsReceiptLine
        {
            TenantId = tenant.TenantId, GoodsReceiptLineId = Guid.NewGuid(),
            GoodsReceiptId = receiptId, PurchaseOrderLineId = x.PurchaseOrderLineId,
            ReceivedQuantity = x.ReceivedQuantity, DamagedQuantity = x.DamagedQuantity,
            ShortQuantity = x.ShortQuantity, ExceptionReason = EmptyToNull(x.ExceptionReason),
            LotOrHeatNumber = EmptyToNull(x.LotOrHeatNumber),
            ManufacturerName = EmptyToNull(x.ManufacturerName), MtcReference = EmptyToNull(x.MtcReference)
        }).ToList();
        db.Add(receipt);
        db.AddRange(lines);
        db.Add(Activity(tenant.TenantId, actor.UserId, "GoodsReceipt", receiptId, "Draft created",
            new { receipt.Number, receipt.PurchaseOrderId, lineCount = lines.Count }, now));
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m07/goods-receipts/{receiptId}",
            new { receipt.GoodsReceiptId, receipt.Number, receipt.Status, receipt.Version });
    }

    private static async Task<IResult> PostGoodsReceipt(
        Guid goodsReceiptId, VersionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, OnboardingStore onboarding,
        IM02ApprovalEngine approvals,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M07", "m07.goods_receipts.post", ct);
        var receipt = await db.Set<M07GoodsReceipt>().SingleOrDefaultAsync(
            x => x.GoodsReceiptId == goodsReceiptId, ct);
        if (receipt is null || !await authorization.CanAccess("M07", actor.UserId,
                "m07.goods_receipts.post", receipt.BranchId, null, receipt.ReceivedByUserId, ct))
            return Results.NotFound();
        if (receipt.Version != request.Version)
            return Results.Conflict(new { detail = "The goods receipt changed. Reload and retry.", receipt.Version });
        if (receipt.Status is not ("Draft" or "Pending approval"))
            return Results.Conflict(new { detail = "Only a draft or approved-pending goods receipt can be posted." });
        if (receipt.Status == "Pending approval")
        {
            var approval = receipt.ApprovalRequestId is { } approvalRequestId
                ? await db.Set<M02ApprovalRequest>().AsNoTracking()
                    .SingleOrDefaultAsync(x => x.RequestId == approvalRequestId, ct)
                : null;
            if (approval?.Status != "Approved")
                return Results.Conflict(new { detail = "Over-receipt approval has not been granted." });
        }
        var order = await db.Set<M07PurchaseOrder>().SingleAsync(
            x => x.PurchaseOrderId == receipt.PurchaseOrderId, ct);
        var orderLines = await db.Set<M07PurchaseOrderLine>().Where(x =>
            x.PurchaseOrderId == order.PurchaseOrderId).ToDictionaryAsync(x => x.PurchaseOrderLineId, ct);
        var receiptLines = await db.Set<M07GoodsReceiptLine>().Where(x =>
            x.GoodsReceiptId == goodsReceiptId).ToListAsync(ct);
        var setup = OnboardingStore.Decode(await onboarding.Current());
        var waitingForApproval = false;
        foreach (var line in receiptLines)
        {
            var purchaseLine = orderLines[line.PurchaseOrderLineId];
            var category = purchaseLine.ProductCategorySnapshot;
            var tolerance = category.Contains("steel", StringComparison.OrdinalIgnoreCase) ||
                            category.Contains("rebar", StringComparison.OrdinalIgnoreCase)
                ? setup.M07SteelOverReceiptTolerancePercent
                : category.Contains("cement", StringComparison.OrdinalIgnoreCase)
                    ? setup.M07CementOverReceiptTolerancePercent
                    : 0m;
            if (purchaseLine.Traceable &&
                (string.IsNullOrWhiteSpace(line.LotOrHeatNumber) ||
                 string.IsNullOrWhiteSpace(line.MtcReference)))
                throw new ValidationException("Traceable products require lot/heat and MTC references on the GRN.");
            if ((line.DamagedQuantity > 0 || line.ShortQuantity > 0) &&
                string.IsNullOrWhiteSpace(line.ExceptionReason))
                throw new ValidationException("Shortages and damage require a reason.");
            if (line.DamagedQuantity + line.ShortQuantity > line.ReceivedQuantity)
                throw new ValidationException("Damaged and short quantity cannot exceed the received quantity.");
            try
            {
                M07Rules.ValidateReceiptQuantity(purchaseLine.OrderedQuantity,
                    purchaseLine.ReceivedQuantity, line.ReceivedQuantity, tolerance);
            }
            catch (InvalidOperationException)
            {
                waitingForApproval = true;
            }
        }
        if (waitingForApproval && receipt.Status != "Pending approval")
        {
            var snapshot = JsonSerializer.SerializeToElement(new
            {
                receipt.GoodsReceiptId, receipt.Number, order.PurchaseOrderId,
                lines = receiptLines.Select(x => new { x.PurchaseOrderLineId, x.ReceivedQuantity, x.LotOrHeatNumber })
            }, Json);
            var approvalId = await approvals.Submit(new ApprovalSubmission(
                "M07GoodsReceipt", receipt.GoodsReceiptId, receipt.BranchId, null,
                actor.UserId, 0m, null, null, null, snapshot), ct);
            if (approvalId is null)
                return Results.Conflict(new { detail = "Over-receipt requires a matching M02 goods-receipt approval rule." });
            receipt.ApprovalRequestId = approvalId;
            receipt.Status = "Pending approval";
            receipt.Version++;
            await db.SaveChangesAsync(ct);
            return Results.Accepted($"/api/v1/m02/approvals/{approvalId}",
                new { receipt.GoodsReceiptId, receipt.Number, receipt.Status, receipt.ApprovalRequestId });
        }
        foreach (var line in receiptLines)
            orderLines[line.PurchaseOrderLineId].ReceivedQuantity += line.ReceivedQuantity;
        var allReceived = orderLines.Values.All(x => x.ReceivedQuantity >= x.OrderedQuantity);
        var anyReceived = orderLines.Values.Any(x => x.ReceivedQuantity > 0);
        var nextStatus = allReceived ? "Received" : anyReceived ? "Partially received" : order.Status;
        if (nextStatus != order.Status)
            M07Rules.RequirePurchaseOrderTransition(order.Status, nextStatus);
        order.Status = nextStatus;
        order.Version++;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        receipt.Status = "Posted";
        receipt.PostedAt = order.UpdatedAt;
        receipt.Version++;
        db.Add(Activity(tenant.TenantId, actor.UserId, "GoodsReceipt", goodsReceiptId, "Posted",
            new { receipt.Number, receipt.PurchaseOrderId, quantityOnly = true }, receipt.PostedAt.Value));
        db.Add(Activity(tenant.TenantId, actor.UserId, "PurchaseOrder", order.PurchaseOrderId,
            "Receipt quantities updated", new { receipt.GoodsReceiptId, order.Status }, receipt.PostedAt.Value));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The purchase order or GRN changed concurrently. Reload and retry." });
        }
        return Results.Ok(new
        {
            receipt.GoodsReceiptId, receipt.Number, receipt.Status,
            order.PurchaseOrderId, purchaseOrderStatus = order.Status, order.Version,
            accountingState = "No accounting entry; BR-M07-09 reserves accounting for supplier invoice posting."
        });
    }

    private static async Task<(decimal Rate, string Snapshot)> ResolvePurchaseTax(
        BerynxDbContext db, M03Product product, M03Party supplier,
        Branch branch, DateOnly date, CancellationToken ct)
    {
        var rate = await db.Set<M03TaxRate>().AsNoTracking()
            .Where(x => x.HsnCode == product.HsnCode && x.IsActive && x.IsApproved &&
                        x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct)
            ?? throw new ValidationException(
                $"No approved M03 tax rate exists for HSN {product.HsnCode} on {date:yyyy-MM-dd}.");
        return (rate.RatePercent, JsonSerializer.Serialize(new
        {
            rate.TaxRateId, rate.HsnCode, rate.RatePercent, rate.EffectiveFrom,
            rate.EffectiveTo, rate.Reason, source = "M03 effective-dated tax master",
            supplierGstin = supplier.Gstin, supplierState = supplier.StateCode,
            buyerBranchGstin = branch.Gstin,
            taxSplitState = "Determine from supplier invoice place-of-supply evidence"
        }, Json));
    }

    private static async Task<string> AllocateNumber(
        BerynxDbContext db, Guid tenantId, Guid branchId, string documentCode,
        DateOnly date, Guid branchKey, string document, Setup setup, CancellationToken ct)
    {
        var startMonth = setup.StartMonth ?? 4;
        var year = date.Year - (date.Month < startMonth ? 1 : 0);
        var series = setup.Series.SingleOrDefault(x =>
            x.BranchId == branchKey && x.Document.Equals(document, StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException(
                $"Configure a {document} numbering series for this branch before use.");
        var sequence = await db.Database.SqlQuery<long>($"""
            INSERT INTO m07_number_counter
                ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart", "NextNumber")
            VALUES ({tenantId}, {branchId}, {documentCode}, {year}, 2)
            ON CONFLICT ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart")
            DO UPDATE SET "NextNumber" = m07_number_counter."NextNumber" + 1
            RETURNING "NextNumber" - 1 AS "Value"
            """).SingleAsync(ct);
        return SetupRules.Number(series, startMonth, date, sequence);
    }

    private static M07Activity Activity(
        Guid tenantId, Guid actorId, string entityType, Guid entityId,
        string action, object detail, DateTimeOffset at) =>
        new()
        {
            TenantId = tenantId, ActivityId = Guid.NewGuid(), EntityType = entityType,
            EntityId = entityId, ActorUserId = actorId, Action = action,
            DetailJson = JsonSerializer.Serialize(detail, Json), At = at
        };

    private static void ValidatePurchaseOrder(PurchaseOrderWrite request)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100)
            throw new ValidationException("An idempotency key of 1–100 characters is required.");
        if (request.BranchId == Guid.Empty || request.SupplierPartyId == Guid.Empty)
            throw new ValidationException("Branch and supplier are required.");
        if (request.OrderDate < DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)) ||
            request.OrderDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))
            throw new ValidationException("Purchase order date is outside the permitted date range.");
        if (request.Lines is null || request.Lines.Length is 0 or > 100 ||
            request.Charges is null || request.Charges.Length > 100)
            throw new ValidationException("A purchase order requires 1–100 lines and up to 100 charges.");
        if (string.IsNullOrWhiteSpace(request.PaymentTerms) || request.PaymentTerms.Length > 1000 ||
            string.IsNullOrWhiteSpace(request.DeliveryTerms) || request.DeliveryTerms.Length > 1000 ||
            request.Notes?.Length > 4000)
            throw new ValidationException("Purchase-order terms or notes exceed their limits.");
        if (request.Lines.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0 ||
                decimal.Round(x.Quantity, 3) != x.Quantity || x.Rate <= 0 ||
                decimal.Round(x.Rate, 4) != x.Rate) ||
            request.Lines.Select(x => x.SourceSalesOrderLineId).Where(x => x is not null).Distinct().Count() !=
                request.Lines.Count(x => x.SourceSalesOrderLineId is not null))
            throw new ValidationException("Purchase lines require positive quantity, rate, and unique source order lines.");
        if (request.Charges.Any(x => string.IsNullOrWhiteSpace(x.Description) ||
                x.Description.Length > 240 || x.Amount < 0 || decimal.Round(x.Amount, 2) != x.Amount ||
                x.GstPercent is < 0 or > 100))
            throw new ValidationException("Purchase-order charges are invalid.");
    }

    private static void ValidateGoodsReceipt(GoodsReceiptWrite request)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100)
            throw new ValidationException("An idempotency key of 1–100 characters is required.");
        if (request.Lines is null || request.Lines.Length is 0 or > 100 ||
            request.Lines.Select(x => x.PurchaseOrderLineId).Distinct().Count() != request.Lines.Length)
            throw new ValidationException("A goods receipt requires 1–100 distinct purchase-order lines.");
        if (request.Lines.Any(x => x.PurchaseOrderLineId == Guid.Empty ||
                x.ReceivedQuantity <= 0 || decimal.Round(x.ReceivedQuantity, 3) != x.ReceivedQuantity ||
                x.DamagedQuantity < 0 || x.ShortQuantity < 0))
            throw new ValidationException("Receipt quantities are invalid.");
        if (request.GrossWeight is < 0 || request.TareWeight is < 0 ||
            request.GrossWeight is not null && request.TareWeight is not null &&
                request.TareWeight > request.GrossWeight)
            throw new ValidationException("Gross and tare weights are invalid.");
        if (request.VehicleNumber?.Length > 32 || request.DriverName?.Length > 120 ||
            request.WeighbridgeSlipNumber?.Length > 80 || request.AttachmentReference?.Length > 500 ||
            request.Remarks?.Length > 2000)
            throw new ValidationException("A goods receipt field exceeds its maximum length.");
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
