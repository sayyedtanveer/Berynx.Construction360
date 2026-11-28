using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Quality;

public sealed class M19QualityCriteria : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid CriteriaId { get; set; }
    public string MaterialType { get; set; } = "";
    public string Grade { get; set; } = "";
    public int TestAgeDays { get; set; }
    public decimal MinimumStrengthMpa { get; set; }
    public string StandardReference { get; set; } = "";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveUntil { get; set; }
    public string ChangeReason { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M19QualitySample : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid SampleId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public Guid BranchId { get; set; }
    public Guid TripId { get; set; }
    public Guid PourId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid ProductId { get; set; }
    public string SampleNumber { get; set; } = "";
    public string MaterialType { get; set; } = "";
    public string GradeSnapshot { get; set; } = "";
    public DateTimeOffset CastAt { get; set; }
    public int CubeCount { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; } = 1;
}

/// <summary>Scheduled test and immutable acceptance snapshot; no threshold is inferred when absent.</summary>
public sealed class M19QualityTest : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid TestId { get; set; }
    public Guid SampleId { get; set; }
    public int TestAgeDays { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public decimal? MinimumStrengthMpaSnapshot { get; set; }
    public string AcceptanceReferenceSnapshot { get; set; } = "";
    public string Status { get; set; } = "Scheduled";
    public decimal? StrengthMpa { get; set; }
    public string Outcome { get; set; } = "Pending";
    public DateTimeOffset? TestedAt { get; set; }
    public Guid? ReportDocumentId { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public string ResultReason { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M19Dispute : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid DisputeId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public Guid BranchId { get; set; }
    public Guid? CustomerPartyId { get; set; }
    public Guid? SupplierPartyId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? ChallanId { get; set; }
    public Guid? SampleId { get; set; }
    public string Type { get; set; } = "Other";
    public string Description { get; set; } = "";
    public Guid OwnerUserId { get; set; }
    public string Status { get; set; } = "Open";
    public string Resolution { get; set; } = "";
    public DateTimeOffset OpenedAt { get; set; }
    public Guid OpenedByUserId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M19DisputeInvoice : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid DisputeId { get; set; }
    public Guid InvoiceId { get; set; }
}

public sealed class M19DisputeEvidence : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EvidenceId { get; set; }
    public Guid DisputeId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid AddedByUserId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
public sealed class M19QualityEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}