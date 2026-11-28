namespace Berynx.Application.SalesOrders;

/// <summary>
/// Read-only order context exposed to future purchase, dispatch, billing and receipt modules.
/// These contracts do not create downstream documents or assert that those modules are live.
/// </summary>
public sealed record M06OrderLineReference(
    Guid SalesOrderLineId,
    Guid ProductId,
    string ProductName,
    string UnitCode,
    decimal OrderedQuantity,
    decimal Rate,
    decimal DeliveredQuantity,
    decimal InvoicedQuantity);

public sealed record M06OrderSource(
    Guid SalesOrderId,
    string OrderNumber,
    Guid TenantId,
    Guid CustomerPartyId,
    Guid? SiteId,
    Guid? BranchId,
    string Status,
    string PaymentTerms,
    string DeliveryTerms,
    IReadOnlyList<M06OrderLineReference> Lines);

public sealed record M06DeliveryScheduleContract(
    Guid ScheduleId,
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    Guid ProductId,
    decimal Quantity,
    DateOnly DeliveryDate,
    string? TimeWindow,
    Guid? SiteId,
    Guid? PreferredPlantId,
    string OperationalNotes);

public sealed record M06DeliveryFact(
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    Guid SourceTripId,
    Guid SourceChallanId,
    decimal DeliveredQuantity,
    string EvidenceStatus,
    DateTimeOffset DeliveredAt,
    string IdempotencyKey);

public sealed record M06BillingEligibility(
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    decimal DeliveredQuantity,
    decimal InvoicedQuantity,
    decimal MaximumInvoiceableQuantity,
    string RateSnapshotJson,
    string State);

public sealed record M06CustomerPaymentProgress(
    Guid SalesOrderId,
    decimal PaidAmount,
    decimal OutstandingAmount,
    DateTimeOffset AsOf);
