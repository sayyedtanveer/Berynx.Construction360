using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Accounting;

public sealed class M12Voucher : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid VoucherId { get; set; }
    public Guid BranchId { get; set; }
    public string Number { get; set; } = "";
    public string VoucherType { get; set; } = "";
    public string Status { get; set; } = "Posted";
    public DateOnly VoucherDate { get; set; }
    public string SourceModule { get; set; } = "";
    public Guid? SourceEntityId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string Narration { get; set; } = "";
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public Guid? ReversalOfVoucherId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
}

public sealed class M12VoucherLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid VoucherLineId { get; set; }
    public Guid VoucherId { get; set; }
    public string LedgerCode { get; set; } = "";
    public Guid? PartyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? TripId { get; set; }
    public string Narration { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public sealed class M12FinancialPeriod : ITenantOwned
{
    public Guid TenantId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public Guid? LockedByUserId { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
}

public sealed class M12VoucherNumberCounter : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public string VoucherType { get; set; } = "";
    public int CalendarYear { get; set; }
    public long NextNumber { get; set; } = 1;
}

public sealed class M12VoucherEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid VoucherId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class M12PeriodEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
}
