using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Berynx.Api.IdentityAccess;

public sealed class M02ApprovalTaskReassignmentProvider(
    BerynxDbContext db,
    IM02Access access,
    ICurrentTenantContext tenant,
    IHttpContextAccessor http) : IM02TaskReassignmentProvider
{
    public string Provider => "Approvals";

    public async Task<IReadOnlyList<M02OpenTask>> OpenTasks(Guid currentOwnerUserId,
        CancellationToken cancellationToken = default)
    {
        var pending = await db.Set<M02ApprovalRequest>()
            .Where(x => x.Status == "Pending" && x.RequesterUserId != currentOwnerUserId)
            .ToListAsync(cancellationToken);
        var tasks = new List<M02OpenTask>();
        foreach (var request in pending)
        {
            var assignee = await EffectiveAssignee(request, cancellationToken);
            if (assignee != currentOwnerUserId ||
                !await access.CanAccess(currentOwnerUserId, "approvals.view", request.BranchId,
                    request.PlantId, request.OwnerUserId, cancellationToken) ||
                !await access.CanAccess(currentOwnerUserId, "approvals.approve", request.BranchId,
                    request.PlantId, request.OwnerUserId, cancellationToken))
                continue;

            tasks.Add(new M02OpenTask(Provider, request.RequestId.ToString("D"),
                $"Approval {request.SourceType} · due {request.DueAt:yyyy-MM-dd HH:mm} UTC"));
        }
        return tasks;
    }

    public async Task Reassign(Guid currentOwnerUserId,
        IReadOnlyList<M02TaskReassignment> reassignments,
        CancellationToken cancellationToken = default)
    {
        var actor = await access.CurrentUser(cancellationToken);
        foreach (var item in reassignments)
        {
            if (item.Provider != Provider || !Guid.TryParse(item.TaskId, out var requestId))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "The approval task assignment is invalid.");
            var request = await db.Set<M02ApprovalRequest>().SingleOrDefaultAsync(x =>
                x.RequestId == requestId && x.Status == "Pending" &&
                x.RequesterUserId != item.NewOwnerUserId, cancellationToken)
                ?? throw new DbUpdateConcurrencyException("The approval task is no longer open.");
            if (await EffectiveAssignee(request, cancellationToken) != currentOwnerUserId)
                throw new DbUpdateConcurrencyException("The approval task was reassigned by another request.");
            if (!await access.CanAccess(item.NewOwnerUserId, "approvals.view", request.BranchId,
                    request.PlantId, request.OwnerUserId, cancellationToken) ||
                !await access.CanAccess(item.NewOwnerUserId, "approvals.approve", request.BranchId,
                    request.PlantId, request.OwnerUserId, cancellationToken))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "The new owner must have approval permission for this request's data scope.");

            var assignment = await db.Set<M02ApprovalTaskAssignment>().SingleOrDefaultAsync(x =>
                x.RequestId == request.RequestId && x.Level == request.CurrentLevel, cancellationToken);
            var before = new
            {
                request.RequestId,
                request.CurrentLevel,
                approverUserId = assignment?.ApproverUserId ?? currentOwnerUserId
            };
            if (assignment is null)
            {
                assignment = new M02ApprovalTaskAssignment
                {
                    TenantId = tenant.TenantId,
                    RequestId = request.RequestId,
                    Level = request.CurrentLevel
                };
                db.Add(assignment);
            }
            assignment.ApproverUserId = item.NewOwnerUserId;
            assignment.AssignedByUserId = actor.UserId;
            assignment.AssignedAt = DateTimeOffset.UtcNow;
            request.Version++;
            db.Add(new M02AuditEvent
            {
                TenantId = tenant.TenantId,
                EventId = Guid.NewGuid(),
                ActorUserId = actor.UserId,
                ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString("D"),
                Action = "Approval task reassigned",
                EntityType = "Approval",
                EntityId = request.RequestId,
                Before = System.Text.Json.JsonSerializer.Serialize(before),
                After = System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.RequestId,
                    request.CurrentLevel,
                    approverUserId = item.NewOwnerUserId
                }),
                IpAddress = http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                Device = http.HttpContext?.Request.Headers.UserAgent.ToString(),
                At = DateTimeOffset.UtcNow
            });
            db.Add(new M02Notification
            {
                TenantId = tenant.TenantId,
                NotificationId = Guid.NewGuid(),
                RecipientUserId = item.NewOwnerUserId,
                Kind = "ApprovalRequested",
                EntityId = request.RequestId,
                Payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.SourceType,
                    request.CurrentLevel,
                    request.DueAt,
                    reassigned = true
                }),
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private async Task<Guid?> EffectiveAssignee(M02ApprovalRequest request, CancellationToken cancellationToken)
    {
        var assignment = await db.Set<M02ApprovalTaskAssignment>().SingleOrDefaultAsync(x =>
            x.RequestId == request.RequestId && x.Level == request.CurrentLevel, cancellationToken);
        if (assignment is not null)
            return assignment.ApproverUserId;

        var level = await db.Set<M02ApprovalLevel>().SingleOrDefaultAsync(x =>
            x.RuleId == request.RuleId && x.Level == request.CurrentLevel, cancellationToken);
        return request.EscalatedAt.HasValue
            ? level?.EscalationUserId ?? level?.ApproverUserId
            : level?.ApproverUserId;
    }
}
