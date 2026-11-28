using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Procurement;

public sealed class M07PurchaseOrder : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string Number { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public Guid? BranchId { get; set; }
    public Guid SupplierPartyId { get; set; }
    public Guid? SourceSalesOrderId { get; set; }
    public Guid? SiteId { get; set; }
    public string SupplierNameSnapshot { get; set; } = "";
    public string SupplierGstinSnapshot { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public DateOnly? ExpectedDate { get; set; }
    public string PaymentTermsSnapshot { get; set; } = "";
    public string DeliveryTermsSnapshot { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal ChargesTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public string ApprovalSnapshotJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public string? ClosureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M07PurchaseOrderLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? SourceSalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCodeSnapshot { get; set; } = "";
    public string ProductNameSnapshot { get; set; } = "";
    public string ProductCategorySnapshot { get; set; } = "";
    public string UnitCode { get; set; } = "";
    public bool Traceable { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal GstPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string TaxSnapshotJson { get; set; } = "{}";
}

public sealed class M07PurchaseOrderCharge : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ChargeId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal GstPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
}

public sealed class M07GoodsReceipt : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public string Number { get; set; } = "";
    public DateOnly ReceiptDate { get; set; }
    public Guid BranchId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid SupplierPartyId { get; set; }
    public Guid? SiteId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public string? WeighbridgeSlipNumber { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? TareWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public string? AttachmentReference { get; set; }
    public string? Remarks { get; set; }
    public Guid ReceivedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M07GoodsReceiptLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal ShortQuantity { get; set; }
    public string? ExceptionReason { get; set; }
    public string? LotOrHeatNumber { get; set; }
    public string? ManufacturerName { get; set; }
    public string? MtcReference { get; set; }
}

public sealed class M07SupplierInvoice : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid SupplierInvoiceId { get; set; }
    public string Number { get; set; } = "";
    public Guid SupplierPartyId { get; set; }
    public string SupplierInvoiceNumber { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal TdsWithheld { get; set; }
    public string? AttachmentReference { get; set; }
    public string? Notes { get; set; }
    public Guid? JournalId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M07SupplierInvoiceLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid InvoiceLineId { get; set; }
    public Guid SupplierInvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? GoodsReceiptLineId { get; set; }
    public string Description { get; set; } = "";
    public string HsnCode { get; set; } = "";
    public string UnitCode { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal GstPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public bool ItcEligible { get; set; }
    public bool ReverseCharge { get; set; }
    public string TaxSnapshotJson { get; set; } = "{}";
}

public sealed class M07SupplierPayment : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid SupplierPaymentId { get; set; }
    public string Number { get; set; } = "";
    public Guid SupplierPartyId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMode { get; set; } = "";
    public string? ReferenceNumber { get; set; }
    public Guid? DepositAccountId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TdsAmount { get; set; }
    public decimal BankCharges { get; set; }
    public decimal NetBankAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid? JournalId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M07SupplierPaymentAllocation : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid AllocationId { get; set; }
    public Guid SupplierPaymentId { get; set; }
    public Guid? SupplierInvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class M07Activity : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ActivityId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset At { get; set; }
}

public sealed class M07NumberCounter : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string DocumentCode { get; set; } = "";
    public int FinancialYearStart { get; set; }
    public long NextNumber { get; set; } = 1;
}
