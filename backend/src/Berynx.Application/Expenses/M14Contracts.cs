namespace Berynx.Application.Expenses;

public sealed record M14CreateExpenseRequest(
    Guid BranchId, Guid CategoryId, DateOnly ExpenseDate, decimal Amount, string Description,
    string PaidFromLedgerCode, bool EmployeePaid, Guid? VendorPartyId, Guid? SiteId,
    Guid? SalesOrderId, Guid? TripId, string? SupplierGstin, string? SupplierBillNumber,
    decimal GstAmount, bool ItcEligible, bool ReverseCharge, string? AttachmentReference,
    string IdempotencyKey);

public sealed record M14ApprovalRequest(string Comment);
public sealed record M14CategoryRequest(string Name, string Type, decimal? ApprovalThreshold, string ExpenseLedgerCode);
