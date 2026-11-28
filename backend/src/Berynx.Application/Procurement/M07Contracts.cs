namespace Berynx.Application.Procurement;

/// <summary>Stable references for M06 purchase requests and M08 receipt handoff.</summary>
public sealed record M07PurchaseSource(
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid SupplierPartyId,
    Guid? SourceSalesOrderId,
    Guid? SiteId,
    IReadOnlyList<M07PurchaseLineSource> Lines);

public sealed record M07PurchaseLineSource(
    Guid PurchaseOrderLineId,
    Guid ProductId,
    string ProductName,
    string UnitCode,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal Rate,
    string? LotOrHeatNumber);

public sealed record M07GoodsReceiptSource(
    Guid GoodsReceiptId,
    string GoodsReceiptNumber,
    Guid PurchaseOrderId,
    Guid PurchaseOrderLineId,
    Guid ProductId,
    decimal ReceivedQuantity,
    string? LotOrHeatNumber,
    string? MtcReference);

/// <summary>
/// Posted supplier transactions are inputs to the shared M12 journal engine,
/// not a second payable ledger owned by M07.
/// </summary>
public sealed record M07SupplierInvoicePosting(
    Guid SupplierInvoiceId,
    Guid SupplierPartyId,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal TaxableAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    IReadOnlyList<M07SupplierInvoicePostingLine> Lines,
    string IdempotencyKey);

public sealed record M07SupplierInvoicePostingLine(
    Guid ProductId,
    string HsnCode,
    decimal TaxableAmount,
    decimal TaxAmount,
    bool ItcEligible,
    bool ReverseCharge,
    string TaxSnapshotJson);

public sealed record M07SupplierPaymentPosting(
    Guid SupplierPaymentId,
    Guid SupplierPartyId,
    DateOnly PaymentDate,
    Guid? DepositAccountId,
    decimal GrossAmount,
    decimal TdsAmount,
    decimal BankCharges,
    IReadOnlyList<M07SupplierPaymentAllocationSource> Allocations,
    string IdempotencyKey);

public sealed record M07SupplierPaymentAllocationSource(
    Guid? SupplierInvoiceId,
    decimal Amount);
