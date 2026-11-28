using Berynx.Domain.Tenancy;

namespace Berynx.Domain.SalesOrders;

public sealed class M06SalesOrder : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid SalesOrderId { get; set; }
    public string Number { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CustomerPartyId { get; set; }
    public string CustomerNameSnapshot { get; set; } = "";
    public Guid? SiteId { get; set; }
    public string SiteAddressSnapshot { get; set; } = "";
    public string? CustomerPoNumber { get; set; }
    public DateOnly? CustomerPoDate { get; set; }
    public string? CustomerPoAttachmentReference { get; set; }
    public Guid? SourceQuotationId { get; set; }
    public string? SourceQuotationNumber { get; set; }
    public Guid? SourceEnquiryId { get; set; }
    public string OrderType { get; set; } = "";
    public string Status { get; set; } = "Draft";
    public Guid SalesOwnerUserId { get; set; }
    public DateOnly? RateValidityUntil { get; set; }
    public Guid? PaymentTermId { get; set; }
    public string PaymentTermsSnapshot { get; set; } = "";
    public string DeliveryTermsSnapshot { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ChargesTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string CreditStatus { get; set; } = "Unknown";
    public string CreditSnapshotJson { get; set; } = "{}";
    public string CostStatus { get; set; } = "Unavailable";
    public decimal? EstimatedMarginPercent { get; set; }
    public string ApprovalSnapshotJson { get; set; } = "{}";
    public Guid? ApprovalRequestId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? ShortClosedAt { get; set; }
    public string? ClosureReason { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M06SalesOrderLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid LineId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid? SourceQuotationLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string ProductCategorySnapshot { get; set; } = "";
    public string UnitCode { get; set; } = "";
    public decimal OrderedQuantity { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GstPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string TaxSnapshotJson { get; set; } = "{}";
    public decimal DeliveredQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal PaidAmount { get; set; }
}

public sealed class M06SalesOrderCharge : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ChargeId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid? SourceQuotationChargeId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string TaxSnapshotJson { get; set; } = "{}";
}

public sealed class M06QuotationAllocation : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid AllocationId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid QuotationLineId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public decimal Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class M06QuotationChargeAllocation : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid AllocationId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid QuotationChargeId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderChargeId { get; set; }
    public decimal Amount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class M06DeliverySchedule : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public DateOnly DeliveryDate { get; set; }
    public string? TimeWindow { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? PreferredPlantId { get; set; }
    public string OperationalNotes { get; set; } = "";
    public string Status { get; set; } = "Planned";
    public Guid? ApprovalRequestId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class M06Amendment : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid AmendmentId { get; set; }
    public Guid SalesOrderId { get; set; }
    public int RevisionNumber { get; set; }
    public string Reason { get; set; } = "";
    public string BeforeSnapshotJson { get; set; } = "{}";
    public string ProposedSnapshotJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending approval";
    public Guid? ApprovalRequestId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class M06Activity : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ActivityId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset At { get; set; }
}

/// <summary>Persisted link contract for real documents supplied by later modules.</summary>
public sealed class M06LinkedDocument : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid LinkId { get; set; }
    public Guid SalesOrderId { get; set; }
    public string SourceModule { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset LinkedAt { get; set; }
}
