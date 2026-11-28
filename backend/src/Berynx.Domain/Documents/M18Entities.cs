using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Documents;

public sealed class M18Document : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid BranchId { get; set; }
    public Guid TripId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? PreviousDocumentId { get; set; }
    public string Purpose { get; set; } = "";
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = "";
    public string ScanStatus { get; set; } = M18DocumentRules.ScanPending;
    public string StorageStatus { get; set; } = M18DocumentRules.StorageQuarantined;
    public bool IsCurrent { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? ScannedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M18DocumentEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TripId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}