using Berynx.Domain.IdentityAccess;

namespace Berynx.Application.IdentityAccess;

public sealed record M02InvitationDelivery(Guid TenantId, Guid UserId, string? Email, string? Mobile,
    string[] RoleKeys, Guid DeliveryId, DateTimeOffset ExpiresAt, string? ActivationUrl);

public interface IM02InvitationDeliveryProvider
{
    Task DeliverInvitation(M02InvitationDelivery invitation, CancellationToken cancellationToken);
}

public interface IM02Access
{
    Task<M02User> CurrentUser(CancellationToken cancellationToken = default);
    Task Require(string permissionCode, CancellationToken cancellationToken = default);
    Task<bool> HasPermission(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
    Task<bool> CanAccess(Guid userId, string permissionCode, Guid? branchId, Guid? plantId, Guid? ownerId,
        CancellationToken cancellationToken = default);
}

public interface IM02ApprovalEngine
{
    Task<Guid?> Submit(ApprovalSubmission submission, CancellationToken cancellationToken = default);
}

public sealed record ApprovalSubmission(string EntityType, Guid SourceId, Guid? BranchId, Guid? PlantId,
    Guid? OwnerUserId,
    decimal Amount, decimal? DiscountPercent, decimal? MarginPercent, string? CreditStatus,
    System.Text.Json.JsonElement Snapshot);

public sealed record M02OpenTask(string Provider, string TaskId, string Summary);
public sealed record M02TaskReassignment(string Provider, string TaskId, Guid NewOwnerUserId);

public interface IM02TaskReassignmentProvider
{
    string Provider { get; }
    Task<IReadOnlyList<M02OpenTask>> OpenTasks(Guid currentOwnerUserId,
        CancellationToken cancellationToken = default);
    Task Reassign(Guid currentOwnerUserId, IReadOnlyList<M02TaskReassignment> reassignments,
        CancellationToken cancellationToken = default);
}
