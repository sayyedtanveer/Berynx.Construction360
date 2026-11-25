using Berynx.Domain.Tenancy;

namespace Berynx.Domain.IdentityAccess;

public sealed class M02User : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? IdentityUserId { get; set; }
    public string? ProviderSubject { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string DisplayName { get; set; } = "";
    public string Status { get; set; } = "Invited";
    public Guid? InvitationDeliveryId { get; set; }
    public DateTimeOffset? InviteExpiresAt { get; set; }
    public DateTimeOffset? InvitationDeliveredAt { get; set; }
    public int InvitationAttempts { get; set; }
    public DateTimeOffset InvitationAvailableAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M02Role : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public string RoleKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSystem { get; set; }
    public Guid? ClonedFromRoleId { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M02RolePermission : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public string PermissionCode { get; set; } = "";
}

public sealed class M02UserRole : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public string ScopeType { get; set; } = "All";
    public Guid? ScopeId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public string AssignedBy { get; set; } = "";
}

public sealed class M02ApprovalRule : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RuleId { get; set; }
    public string Name { get; set; } = "";
    public string EntityType { get; set; } = "";
    public decimal? AmountGreaterThan { get; set; }
    public decimal? DiscountPercentGreaterThan { get; set; }
    public decimal? MarginPercentBelow { get; set; }
    public string? CreditStatus { get; set; }
    public bool Enabled { get; set; } = true;
    public long Version { get; set; } = 1;
}

public sealed class M02ApprovalLevel : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RuleId { get; set; }
    public int Level { get; set; }
    public Guid? ApproverRoleId { get; set; }
    public Guid? ApproverUserId { get; set; }
    public int SlaMinutes { get; set; }
    public Guid? EscalationRoleId { get; set; }
    public Guid? EscalationUserId { get; set; }
}

public sealed class M02ApprovalRequest : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid RuleId { get; set; }
    public string SourceType { get; set; } = "";
    public Guid SourceId { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PlantId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public int CurrentLevel { get; set; } = 1;
    public decimal Amount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? MarginPercent { get; set; }
    public string? CreditStatus { get; set; }
    public string Snapshot { get; set; } = "{}";
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? EscalatedAt { get; set; }
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class M02ApprovalDecision : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid DecisionId { get; set; }
    public Guid RequestId { get; set; }
    public Guid ActorUserId { get; set; }
    public int Level { get; set; }
    public string Decision { get; set; } = "";
    public string? Comment { get; set; }
    public DateTimeOffset At { get; set; }
}

public sealed class M02ApprovalTaskAssignment : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public int Level { get; set; }
    public Guid ApproverUserId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}

public sealed class M02Delegation : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid DelegationId { get; set; }
    public Guid DelegatorUserId { get; set; }
    public Guid DelegateUserId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class M02AuditEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorSubject { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Before { get; set; } = "{}";
    public string After { get; set; } = "{}";
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public string? Device { get; set; }
    public DateTimeOffset At { get; set; }
}

public sealed class M02Notification : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid NotificationId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string Kind { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public sealed class M02ProviderSession : ITenantOwned
{
    public Guid TenantId { get; set; }
    public string ProviderSessionId { get; set; } = "";
    public Guid UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? Device { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class M02PermissionCatalog
{
    public string PermissionCode { get; set; } = "";
    public string Module { get; set; } = "";
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class M02SystemRoleTemplate
{
    public string RoleKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class M02SystemRolePermission
{
    public string RoleKey { get; set; } = "";
    public string PermissionCode { get; set; } = "";
}

public sealed class M02AuthSession : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid IdentityUserId { get; set; }
    public string SecurityStamp { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Device { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M02RefreshToken : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid TokenId { get; set; }
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class M02AuthChallenge
{
    public Guid ChallengeId { get; set; }
    public Guid IdentityUserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? MembershipUserId { get; set; }
    public string Purpose { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public int FailedAttempts { get; set; }
}
