using Berynx.Domain.Tenancy;

namespace Berynx.Domain.DeliveryChallans;

public sealed class M09DeliveryChallan : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ChallanId { get; set; }
    public Guid TripId { get; set; }
    public Guid PourId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CustomerPartyId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid ProductId { get; set; }
    public string Number { get; set; } = "";
    public DateOnly ChallanDate { get; set; }
    public DateTimeOffset DeliveredAt { get; set; }
    public string Status { get; set; } = "Unverified";
    public string CustomerNameSnapshot { get; set; } = "";
    public string SiteAddressSnapshot { get; set; } = "";
    public string OrderNumberSnapshot { get; set; } = "";
    public string? CustomerPoNumberSnapshot { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string UnitCodeSnapshot { get; set; } = "";
    public decimal DeliveredQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal UnitRateSnapshot { get; set; }
    public decimal TaxPercentSnapshot { get; set; }
    public decimal DeliveredValueSnapshot { get; set; }
    public string VehicleNumberSnapshot { get; set; } = "";
    public string DriverNameSnapshot { get; set; } = "";
    public string PlantNameSnapshot { get; set; } = "";
    public string BatchSlipNumber { get; set; } = "";
    public string? ReceiverName { get; set; }
    public string? ReceiverMobile { get; set; }
    public string EvidenceState { get; set; } = "Unverified";
    public DateTimeOffset? ReceiverVerifiedAt { get; set; }
    public string? SignatureReference { get; set; }
    public string? PhotoReference { get; set; }
    public string? EwayBillNumber { get; set; }
    public Guid? InvoiceId { get; set; }
    public string Remarks { get; set; } = "";
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public sealed class M09ChallanRevision : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RevisionId { get; set; }
    public Guid ChallanId { get; set; }
    public int RevisionNumber { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public string Reason { get; set; } = "";
    public decimal OriginalQuantity { get; set; }
    public decimal CorrectedQuantity { get; set; }
    public string Status { get; set; } = "Pending approval";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
}

public sealed class M09ChallanEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid ChallanId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class M09ChallanNumberCounter : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string DocumentCode { get; set; } = "DeliveryChallan";
    public int FinancialYearStart { get; set; }
    public long NextNumber { get; set; } = 1;
}
