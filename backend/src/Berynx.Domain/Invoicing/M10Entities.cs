using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Invoicing;

public sealed class M10Invoice : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerPartyId { get; set; }
    public Guid? SiteId { get; set; }
    public string Number { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string InvoiceType { get; set; } = "Tax invoice";
    public string TaxTreatment { get; set; } = "CGST+SGST";
    public string? BillingModeSnapshot { get; set; }
    public string PlaceOfSupplyStateCode { get; set; } = "";
    public string CustomerNameSnapshot { get; set; } = "";
    public string CustomerGstinSnapshot { get; set; } = "";
    public string BranchGstinSnapshot { get; set; } = "";
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TcsAmount { get; set; }
    public decimal RoundOffAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = "";
    public string PostingSnapshotJson { get; set; } = "{}";
    public string EInvoiceStatus { get; set; } = "Not prepared";
    public string? Irn { get; set; }
    public string? AckNumber { get; set; }
    public DateTimeOffset? AckAt { get; set; }
    public string? EwayBillNumber { get; set; }
    public DateTimeOffset? EwayBillValidUntil { get; set; }
    public bool DisputeHold { get; set; }
    public long Version { get; set; } = 1;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public Guid? VoucherId { get; set; }
}

public sealed class M10InvoiceLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid InvoiceLineId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string HsnCodeSnapshot { get; set; } = "";
    public string UnitCodeSnapshot { get; set; } = "";
    public string ChallanReferencesJson { get; set; } = "[]";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string TaxSnapshotJson { get; set; } = "{}";
}

public sealed class M10InvoiceChallan : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid InvoiceChallanId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ChallanId { get; set; }
    public decimal Quantity { get; set; }
    public decimal DeliveredValueSnapshot { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}

public sealed class M10InvoiceEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}
