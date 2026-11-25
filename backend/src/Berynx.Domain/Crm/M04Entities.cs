using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Crm;

public sealed class M04Enquiry : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EnquiryId { get; set; }
    public string Number { get; set; } = "";
    public string Source { get; set; } = "";
    public Guid? ContactId { get; set; }
    public Guid? CustomerPartyId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? LinkedSalesOrderId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Normal";
    public DateOnly? RequiredBy { get; set; }
    public decimal? ExpectedValue { get; set; }
    public DateTimeOffset? NextActionAt { get; set; }
    public DateTimeOffset FirstResponseDueAt { get; set; }
    public DateTimeOffset? FirstRespondedAt { get; set; }
    public string? LostReason { get; set; }
    public string Notes { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class M04EnquiryLine : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid LineId { get; set; }
    public Guid EnquiryId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = "";
    public string? RmcGrade { get; set; }
    public string? RmcSlumpBand { get; set; }
    public string? RmcAdmixtureCode { get; set; }
    public string? RmcPumpType { get; set; }
}

public sealed class M04EnquiryActivity : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ActivityId { get; set; }
    public Guid EnquiryId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Kind { get; set; } = "";
    public string Channel { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTimeOffset At { get; set; }
}

public sealed class M04FollowUp : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid FollowUpId { get; set; }
    public Guid EnquiryId { get; set; }
    public Guid OwnerUserId { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public string Action { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Outcome { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M04AssignmentAudit : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid AssignmentAuditId { get; set; }
    public Guid EnquiryId { get; set; }
    public Guid PreviousOwnerUserId { get; set; }
    public Guid NewOwnerUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = "";
    public DateTimeOffset At { get; set; }
}

public sealed class M04NumberCounter : ITenantOwned
{
    public Guid TenantId { get; set; }
    public string DocumentCode { get; set; } = "";
    public int CalendarYear { get; set; }
    public long NextNumber { get; set; }
}

public sealed class M04AssignmentCursor : ITenantOwned
{
    public Guid TenantId { get; set; }
    public string Source { get; set; } = "";
    public Guid? LastOwnerUserId { get; set; }
}

public sealed class M04AssignmentRule : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RuleId { get; set; }
    public string Source { get; set; } = "";
    public string? RegionStateCode { get; set; }
    public Guid OwnerUserId { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M04SlaAlert : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EnquiryId { get; set; }
    public DateTimeOffset AlertedAt { get; set; }
}
