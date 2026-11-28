namespace Berynx.Application.Receivables;

public sealed record M11AllocationWrite(Guid InvoiceId, decimal Amount);
public sealed record M11ReceiptDraftRequest(
    Guid CustomerPartyId, Guid BranchId, DateOnly ReceiptDate,
    string PaymentMethod, string? ReferenceNumber, decimal Amount,
    decimal TdsAmount, decimal BankCharges, string? Notes,
    string IdempotencyKey, M11AllocationWrite[] Allocations);

public sealed record M11PromiseWrite(
    Guid CustomerPartyId, Guid BranchId, Guid? InvoiceId,
    DateOnly PromiseDate, decimal Amount, string? Notes);

public sealed record M11ReceiptPostRequest(long Version, string Reason);
