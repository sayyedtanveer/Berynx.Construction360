using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Expenses;

public sealed class M14ExpenseCategory : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Direct";
    public decimal? ApprovalThreshold { get; set; }
    public string ExpenseLedgerCode { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class M14Expense : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? VendorPartyId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? TripId { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string Status { get; set; } = "Draft";
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public string PaidFromLedgerCode { get; set; } = "";
    public bool EmployeePaid { get; set; }
    public string SupplierGstin { get; set; } = "";
    public string SupplierBillNumber { get; set; } = "";
    public decimal GstAmount { get; set; }
    public bool ItcEligible { get; set; }
    public bool ReverseCharge { get; set; }
    public string AttachmentReference { get; set; } = "";
    public string ApprovalComment { get; set; } = "";
    public Guid? ApprovalRequestId { get; set; }
    public Guid? VoucherId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
}

public sealed class M14ExpenseEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
}
