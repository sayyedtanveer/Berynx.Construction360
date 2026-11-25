using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Quotations;

public sealed class M05Quotation : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid SeriesId { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public string? Number { get; set; }
    public bool IsActiveRevision { get; set; } = true;
    public Guid? SourceEnquiryId { get; set; }
    public Guid CustomerPartyId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid SalespersonUserId { get; set; }
    public string Status { get; set; } = "Draft";
    public string PriceBasis { get; set; } = "ExPlant";
    public DateOnly ValidUntil { get; set; }
    public Guid? PaymentTermId { get; set; }
    public string PlaceOfSupplyStateCode { get; set; } = "";
    public string CompanyLegalNameSnapshot { get; set; } = "";
    public string SellerGstinSnapshot { get; set; } = "";
    public string CustomerNameSnapshot { get; set; } = "";
    public string? CustomerGstinSnapshot { get; set; }
    public string CustomerAddressSnapshot { get; set; } = "";
    public string SiteAddressSnapshot { get; set; } = "";
    public string DeliveryTerms { get; set; } = "";
    public string TermsAndConditions { get; set; } = "";
    public string InternalNotes { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ChargesTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal? MarginPercent { get; set; }
    public string CostStatus { get; set; } = "Unavailable";
    public string CreditStatus { get; set; } = "Unknown";
    public string CreditSnapshotJson { get; set; } = "{}";
    public string ApprovalSnapshotJson { get; set; } = "{}";
    public string RateComparisonJson { get; set; } = "[]";
    public Guid? ApprovalRequestId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M05QuotationLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid LineId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid ProductId { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal GstPercent { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public Guid? PriceListId { get; set; }
    public string RateSource { get; set; } = "";
    public string? RmcGrade { get; set; }
    public string? RmcSlumpBand { get; set; }
    public string? RmcAdmixtureCode { get; set; }
    public string? RmcPumpType { get; set; }
    public string TaxSnapshotJson { get; set; } = "{}";
    public string TaxTreatmentSnapshotJson { get; set; } = "{}";
}

public sealed class M05QuotationCharge : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ChargeId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid? ChargeTypeId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string TaxTreatmentSnapshotJson { get; set; } = "{}";
}

public sealed class M05QuotationLink : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid LinkId { get; set; }
    public Guid QuotationId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class M05QuotationActivity : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ActivityId { get; set; }
    public Guid QuotationId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset At { get; set; }
}
