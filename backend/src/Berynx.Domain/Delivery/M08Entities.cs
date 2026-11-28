using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Delivery;

public sealed class M08Pour : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PourId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid SalesOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? PlantId { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string CustomerNameSnapshot { get; set; } = "";
    public string SiteNameSnapshot { get; set; } = "";
    public string ProductCategorySnapshot { get; set; } = "";
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd { get; set; }
    public decimal ScheduledQuantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public string Slump { get; set; } = "";
    public string PlacementMethod { get; set; } = "Direct";
    public string PumpType { get; set; } = "";
    public decimal PumpQuantity { get; set; }
    public string CustomerEngineer { get; set; } = "";
    public string Coordinator { get; set; } = "";
    public string SpecialInstructions { get; set; } = "";
    public string Status { get; set; } = "Scheduled";
    public string? ClosureReason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M08Trip : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid TripId { get; set; }
    public Guid PourId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? PlantId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public string Number { get; set; } = "";
    public string Status { get; set; } = "Planned";
    public decimal PlannedQuantity { get; set; }
    public decimal LoadedQuantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public string? BatchSlipNumber { get; set; }
    public string? LotOrHeatNumber { get; set; }
    public string? MtcReference { get; set; }
    public string? WeighbridgeSlipNumber { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? TareWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public DateTimeOffset? LoadedAt { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
    public DateTimeOffset? ArrivedAt { get; set; }
    public DateTimeOffset? UnloadingStartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverMobile { get; set; }
    public string? SignatureReference { get; set; }
    public string? PhotoReference { get; set; }
    public DateTimeOffset? ReceiverVerifiedAt { get; set; }
    public Guid? ReceiverVerifiedByUserId { get; set; }
    public string? EvidenceDecisionReason { get; set; }
    public string EvidenceState { get; set; } = "Missing";
    public string ExceptionType { get; set; } = "";
    public string? ExceptionReason { get; set; }
    public decimal WaitingMinutes { get; set; }
    public decimal WaitingCharge { get; set; }
    public decimal PartLoadCharge { get; set; }
    public decimal? PurchaseRateSnapshot { get; set; }
    public decimal? PurchaseCostSnapshot { get; set; }
    public string CostSnapshotJson { get; set; } = "{}";
    public string? ReplacedByTripId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M08TripEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid TripId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string FromStatus { get; set; } = "";
    public string ToStatus { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset DeviceAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public string ActionId { get; set; } = "";
}

public sealed class M08DriverAction : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid DriverActionId { get; set; }
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public string ActionId { get; set; } = "";
    public string ActionType { get; set; } = "";
    public string State { get; set; } = "Sent";
    public string? RejectionReason { get; set; }
    public DateTimeOffset DeviceAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class M08ReceiverChallenge : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ChallengeId { get; set; }
    public Guid TripId { get; set; }
    public string DestinationMobileProtected { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int ResendCount { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public int FailedAttempts { get; set; }
}
