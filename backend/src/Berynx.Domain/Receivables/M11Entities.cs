using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Receivables;

public sealed class M11Receipt : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid CustomerPartyId { get; set; }
    public Guid BranchId { get; set; }
    public string Number { get; set; } = "";
    public DateOnly ReceiptDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string PaymentMethod { get; set; } = "";
    public string ReferenceNumber { get; set; } = "";
    public Guid? DepositAccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal TdsAmount { get; set; }
    public decimal BankCharges { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal AdvanceAmount { get; set; }
    public string Notes { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public long Version { get; set; } = 1;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public Guid? VoucherId { get; set; }
}

public sealed class M11ReceiptAllocation : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid AllocationId { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class M11PromiseToPay : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid PromiseId { get; set; }
    public Guid CustomerPartyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateOnly PromiseDate { get; set; }
    public decimal Amount { get; set; }
    public string Outcome { get; set; } = "Open";
    public string Notes { get; set; } = "";
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class M11ReceivableEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}
