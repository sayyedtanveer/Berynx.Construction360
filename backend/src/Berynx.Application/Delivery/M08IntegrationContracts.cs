namespace Berynx.Application.Delivery;

/// <summary>Non-commercial delivery view for M06 order tracking and customer support.</summary>
public sealed record M08TripProgress(
    Guid TripId,
    string TripNumber,
    Guid PourId,
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    string Status,
    decimal PlannedQuantity,
    decimal DeliveredQuantity,
    decimal ReturnedQuantity,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? ArrivedAt,
    DateTimeOffset? CompletedAt,
    string EvidenceState);

/// <summary>Immutable facts supplied to M09 when a completed trip is eligible for a challan.</summary>
public sealed record M08ChallanSource(
    Guid TripId,
    string TripNumber,
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    Guid ProductId,
    Guid? SiteId,
    Guid BranchId,
    decimal DeliveredQuantity,
    string UnitCode,
    string? VehicleRegistration,
    string? DriverName,
    Guid? PlantId,
    string? BatchSlipNumber,
    string? ReceiverName,
    string? SignatureReference,
    string? PhotoReference,
    DateTimeOffset? ReceiverVerifiedAt,
    string EvidenceState,
    string? LotOrHeatNumber,
    string? MtcReference);

/// <summary>
/// Tenant-isolated trip information for the assigned driver. Contains no customer rate,
/// purchase rate, purchase cost, invoice value, or margin fields.
/// </summary>
public sealed record M08DriverTripCard(
    Guid TripId,
    string TripNumber,
    string Status,
    decimal PlannedQuantity,
    string CustomerName,
    string SiteName,
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    string? BatchSlipNumber,
    string? ExceptionType);

public sealed record M08QueuedDriverAction(
    string ActionId,
    Guid TripId,
    string ActionType,
    DateTimeOffset DeviceAt,
    string? ReceiverName,
    string? SignatureReference,
    string? PhotoReference,
    string? ExceptionType,
    string? ExceptionReason);
