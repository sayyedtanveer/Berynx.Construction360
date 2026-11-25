using System.Text.Json;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Persistence;

public sealed class M02ApprovalEngine(
    BerynxDbContext db,
    IM02Access access,
    ICurrentTenantContext tenant) : IM02ApprovalEngine
{
    public async Task<Guid?> Submit(ApprovalSubmission submission, CancellationToken cancellationToken = default)
    {
        await access.Require("approvals.create", cancellationToken);
        var current = await access.CurrentUser(cancellationToken);
        var snapshot = submission.Snapshot.GetRawText();
        var latest = await db.Set<M02ApprovalRequest>().Where(x =>
                x.SourceType == submission.EntityType && x.SourceId == submission.SourceId)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && string.Equals(latest.Snapshot, snapshot, StringComparison.Ordinal))
            return latest.RequestId;
        var existing = latest?.Status == "Pending" ? latest : null;

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (existing is not null)
        {
            var now = DateTimeOffset.UtcNow;
            var before = JsonSerializer.Serialize(new
            {
                existing.Status, existing.CurrentLevel, existing.Version, existing.Snapshot
            });
            existing.Status = "Cancelled";
            existing.Version++;
            db.Add(new M02ApprovalDecision
            {
                TenantId = tenant.TenantId,
                DecisionId = Guid.NewGuid(),
                RequestId = existing.RequestId,
                ActorUserId = current.UserId,
                Level = existing.CurrentLevel,
                Decision = "Cancel",
                Comment = "Source snapshot changed; submit a new approval request.",
                At = now
            });
            db.Add(new M02Notification
            {
                TenantId = tenant.TenantId,
                NotificationId = Guid.NewGuid(),
                RecipientUserId = existing.RequesterUserId,
                Kind = "ApprovalCancelled",
                EntityId = existing.RequestId,
                Payload = JsonSerializer.Serialize(new { reason = "Source snapshot changed." }),
                CreatedAt = now
            });
            db.Add(new M02AuditEvent
            {
                TenantId = tenant.TenantId,
                EventId = Guid.NewGuid(),
                ActorUserId = current.UserId,
                ActorSubject = current.ProviderSubject ?? current.UserId.ToString(),
                Action = "Approval cancelled for changed snapshot",
                EntityType = submission.EntityType,
                EntityId = submission.SourceId,
                Before = before,
                After = JsonSerializer.Serialize(new
                {
                    existing.Status, existing.CurrentLevel, existing.Version, snapshot
                }),
                At = now
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var rules = await db.Set<M02ApprovalRule>().Where(x =>
            x.Enabled && x.EntityType == submission.EntityType).ToListAsync(cancellationToken);
        var matched = rules.Where(rule => M02Rules.Matches(rule, submission.Amount,
            submission.DiscountPercent, submission.MarginPercent, submission.CreditStatus)).ToList();
        if (matched.Count == 0)
        {
            if (ownsTransaction)
                await transaction!.CommitAsync(cancellationToken);
            return null;
        }
        if (matched.Count > 1)
            throw new InvalidOperationException(
                "More than one enabled approval rule matches this request. Resolve the rule overlap before submitting.");

        var rule = matched[0];
        var level = await db.Set<M02ApprovalLevel>().SingleOrDefaultAsync(x =>
            x.RuleId == rule.RuleId && x.Level == 1, cancellationToken)
            ?? throw new InvalidOperationException("The configured approval rule has no first level.");
        var request = new M02ApprovalRequest
        {
            TenantId = tenant.TenantId,
            RequestId = Guid.NewGuid(),
            RuleId = rule.RuleId,
            SourceType = submission.EntityType,
            SourceId = submission.SourceId,
            RequesterUserId = current.UserId,
            BranchId = submission.BranchId,
            PlantId = submission.PlantId,
            OwnerUserId = submission.OwnerUserId ?? current.UserId,
            Status = "Pending",
            CurrentLevel = 1,
            Amount = submission.Amount,
            DiscountPercent = submission.DiscountPercent,
            MarginPercent = submission.MarginPercent,
            CreditStatus = submission.CreditStatus,
            Snapshot = snapshot,
            DueAt = DateTimeOffset.UtcNow.AddMinutes(level.SlaMinutes),
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Add(request);
        var candidates = level.ApproverUserId.HasValue
            ? [level.ApproverUserId.Value]
            : await db.Set<M02UserRole>().Where(x => x.RoleId == level.ApproverRoleId)
                .Select(x => x.UserId).Distinct().ToArrayAsync(cancellationToken);
        var recipients = new List<Guid>();
        foreach (var recipient in candidates)
            if (recipient != request.RequesterUserId &&
                await access.CanAccess(recipient, "approvals.view", request.BranchId,
                    request.PlantId, request.OwnerUserId, cancellationToken) &&
                await access.CanAccess(recipient, "approvals.approve", request.BranchId,
                    request.PlantId, request.OwnerUserId, cancellationToken))
                recipients.Add(recipient);
        if (recipients.Count == 0)
            throw new InvalidOperationException("No active approver has inbox permission for the request data scope.");
        foreach (var recipient in recipients)
            db.Add(new M02Notification
            {
                TenantId = tenant.TenantId,
                NotificationId = Guid.NewGuid(),
                RecipientUserId = recipient,
                Kind = "ApprovalRequested",
                EntityId = request.RequestId,
                Payload = JsonSerializer.Serialize(new
                {
                    request.SourceType, request.SourceId, request.Amount,
                    request.BranchId, request.DueAt
                }),
                CreatedAt = DateTimeOffset.UtcNow
            });
        db.Add(new M02AuditEvent
        {
            TenantId = tenant.TenantId,
            EventId = Guid.NewGuid(),
            ActorUserId = current.UserId,
            ActorSubject = current.ProviderSubject ?? current.UserId.ToString(),
            Action = "Approval requested",
            EntityType = submission.EntityType,
            EntityId = submission.SourceId,
            Before = "{}",
            After = JsonSerializer.Serialize(new
            {
                request.RequestId, request.Amount, request.DiscountPercent, request.MarginPercent,
                request.CreditStatus, request.CurrentLevel, request.DueAt
            }),
            At = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        if (ownsTransaction)
            await transaction!.CommitAsync(cancellationToken);
        return request.RequestId;
    }
}
