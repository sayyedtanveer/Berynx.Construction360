using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.Crm;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Crm;

public static class M04Endpoints
{
    private static readonly string[] Sources =
        ["Phone", "WalkIn", "WhatsApp", "Referral", "SiteVisit", "Website", "Campaign", "Repeat"];
    private static readonly string[] LostReasons =
        ["Price", "Competitor", "Credit", "Timing", "Quality", "NoResponse", "Other"];
    private static readonly string[] OpenStatuses =
        ["New", "Contacted", "Follow-up", "Quotation Sent", "Negotiation", "On hold"];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const long MaximumExpectedValue = 9_999_999_999_999_999;

    public sealed record EnquiryLineWrite(
        Guid ProductId, decimal Quantity, string UnitCode, string? RmcGrade = null,
        string? RmcSlumpBand = null, string? RmcAdmixtureCode = null, string? RmcPumpType = null);
    public sealed record EnquiryWrite(
        string Source, Guid? ContactId, Guid? CustomerPartyId, Guid? SiteId, Guid? BranchId,
        EnquiryLineWrite[] Lines, DateOnly? RequiredBy, decimal? ExpectedValue, string Priority,
        DateTimeOffset? NextActionAt, string Notes, string IdempotencyKey,
        Guid? OwnerUserId = null, bool ConfirmDuplicate = false, long Version = 0);
    public sealed record StatusWrite(string Status, string? LostReason, long Version,
        DateTimeOffset? NextActionAt = null);
    public sealed record AssignmentWrite(Guid OwnerUserId, string Reason, long Version);
    public sealed record ActivityWrite(string Kind, string Channel, string Outcome, string Notes,
        DateTimeOffset? NextActionAt, string NextAction);
    public sealed record FollowUpWrite(DateTimeOffset NextActionAt, string NextAction, string Outcome, long Version);
    public sealed record AssignmentRuleWrite(string Source, string? RegionStateCode,
        Guid OwnerUserId, int Priority, bool IsActive, long Version = 0);
    public sealed record BulkReassignment(Guid EnquiryId, Guid NewOwnerUserId, long Version);

    public static void MapM04(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m04")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M04",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });

        api.MapGet("/enquiries", List);
        api.MapPost("/enquiries", Create);
        api.MapGet("/enquiries/{enquiryId:guid}", Detail);
        api.MapPut("/enquiries/{enquiryId:guid}", Update);
        api.MapPost("/enquiries/{enquiryId:guid}/status", ChangeStatus);
        api.MapPost("/enquiries/{enquiryId:guid}/assign", Assign);
        api.MapPost("/enquiries/{enquiryId:guid}/activities", AddActivity);
        api.MapGet("/enquiries/{enquiryId:guid}/assignment-history", AssignmentHistory);
        api.MapGet("/follow-ups", FollowUps);
        api.MapPost("/follow-ups/{followUpId:guid}/complete", CompleteFollowUp);
        api.MapGet("/analytics", Analytics);
        api.MapGet("/assignment-rules", ListAssignmentRules);
        api.MapPost("/assignment-rules", CreateAssignmentRule);
        api.MapPut("/assignment-rules/{ruleId:guid}", UpdateAssignmentRule);
        api.MapPost("/reassign-inactive", ReassignInactive);
        api.MapPost("/sla/refresh", RefreshSlaNotifications);
        api.MapPost("/enquiries/{enquiryId:guid}/reopen", ReopenLost);
    }

    private static async Task<IResult> List(
        string? q, string? status, Guid? ownerId, Guid? branchId, Guid? customerId,
        int page, int pageSize, TenantAuthorizationService authorization, BerynxDbContext db,
        CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.view", ct);
        var query = await ScopedEnquiries(db, authorization, user.UserId, ct);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (ownerId is not null)
            query = query.Where(x => x.OwnerUserId == ownerId);
        if (branchId is not null)
            query = query.Where(x => x.BranchId == branchId);
        if (customerId is not null)
            query = query.Where(x => x.CustomerPartyId == customerId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var value = q.Trim();
            query = query.Where(x => x.Number.Contains(value) || x.Notes.Contains(value));
        }
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize is 0 ? 25 : pageSize, 1, 100);
        var total = await query.LongCountAsync(ct);
        var rows = await query.AsNoTracking().OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.EnquiryId, x.Number, x.Source, x.ContactId, x.CustomerPartyId, x.SiteId,
                x.BranchId, x.OwnerUserId, x.Status, x.Priority, x.RequiredBy, x.ExpectedValue,
                x.NextActionAt, x.FirstResponseDueAt, x.FirstRespondedAt, x.LostReason, x.Version,
                x.CreatedAt, x.UpdatedAt
            }).ToListAsync(ct);
        return Results.Ok(new { page, pageSize, total, rows });
    }

    private static async Task<IResult> Create(
        EnquiryWrite request, TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, OnboardingStore onboarding, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.create", ct);
        await authorization.Require("M04", "m04.enquiries.view", ct);
        ValidateWrite(request);
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new ValidationException("An idempotency key is required.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var lockName = $"M04:{tenant.TenantId:N}:{request.IdempotencyKey.Trim()}";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockName}, 0))", ct);
        var prior = await db.Set<M04Enquiry>().AsNoTracking().SingleOrDefaultAsync(
            x => x.IdempotencyKey == request.IdempotencyKey.Trim(), ct);
        if (prior is not null)
        {
            await transaction.CommitAsync(ct);
            return Results.Ok(new { enquiryId = prior.EnquiryId, prior.Number, prior.Version, duplicate = true });
        }

        var references = await ValidateReferences(db, tenant.TenantId, request, onboarding, ct);
        var duplicateRows = await FindDuplicates(db, request.ContactId, references.SiteId,
            request.Lines.Select(x => x.ProductId).Distinct().ToArray(), null, ct);
        if (duplicateRows.Count > 0 && !request.ConfirmDuplicate)
            return Results.Conflict(new
            {
                detail = "A similar open enquiry exists for this contact, site and product within seven days.",
                duplicateWarning = true,
                duplicates = duplicateRows
            });

        var ownerId = request.OwnerUserId is { } chosen
            ? await ValidateRequestedOwner(db, authorization, actor, chosen, references.BranchId, ct)
            : await SelectOwner(db, tenant.TenantId, authorization, actor, request.Source,
                references.RegionStateCode, references.BranchId, ct);
        if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.create",
                references.BranchId, null, ownerId, ct, write: true) ||
            !await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.view",
                references.BranchId, null, ownerId, ct, write: false))
            throw new UnauthorizedAccessException("The selected branch is outside your assignment scope.");

        var now = DateTimeOffset.UtcNow;
        var year = now.Year;
        var sequence = await db.Database.SqlQuery<long>($"""
            INSERT INTO m04_number_counter ("TenantId", "DocumentCode", "CalendarYear", "NextNumber")
            VALUES ({tenant.TenantId}, {"Enquiry"}, {year}, 2)
            ON CONFLICT ("TenantId", "DocumentCode", "CalendarYear")
            DO UPDATE SET "NextNumber" = m04_number_counter."NextNumber" + 1
            RETURNING "NextNumber" - 1 AS "Value"
            """).SingleAsync(ct);
        var enquiry = new M04Enquiry
        {
            TenantId = tenant.TenantId,
            EnquiryId = Guid.NewGuid(),
            Number = $"ENQ-{year}-{sequence:D6}",
            Source = request.Source,
            ContactId = request.ContactId,
            CustomerPartyId = references.CustomerPartyId,
            SiteId = references.SiteId,
            BranchId = references.BranchId,
            OwnerUserId = ownerId,
            Status = "New",
            Priority = request.Priority,
            RequiredBy = request.RequiredBy,
            ExpectedValue = request.ExpectedValue,
            NextActionAt = request.NextActionAt,
            FirstResponseDueAt = now.AddMinutes(await FirstResponseSla(db, tenant.TenantId, ct)),
            Notes = request.Notes.Trim(),
            IdempotencyKey = request.IdempotencyKey.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Add(enquiry);
        db.AddRange(request.Lines.Select(line => new M04EnquiryLine
        {
            TenantId = tenant.TenantId,
            LineId = Guid.NewGuid(),
            EnquiryId = enquiry.EnquiryId,
            ProductId = line.ProductId,
            Quantity = line.Quantity,
            UnitCode = line.UnitCode.Trim(),
            RmcGrade = Clean(line.RmcGrade), RmcSlumpBand = Clean(line.RmcSlumpBand),
            RmcAdmixtureCode = Clean(line.RmcAdmixtureCode), RmcPumpType = Clean(line.RmcPumpType)
        }));
        db.Add(new M04FollowUp
        {
            TenantId = tenant.TenantId,
            FollowUpId = Guid.NewGuid(),
            EnquiryId = enquiry.EnquiryId,
            OwnerUserId = ownerId,
            DueAt = request.NextActionAt!.Value,
            Action = "Call",
            Status = "Pending"
        });
        db.Add(new M04EnquiryActivity
        {
            TenantId = tenant.TenantId,
            ActivityId = Guid.NewGuid(),
            EnquiryId = enquiry.EnquiryId,
            ActorUserId = actor.UserId,
            Kind = "Created",
            Channel = request.Source,
            Outcome = "Enquiry captured",
            At = now
        });
        AddAudit(db, tenant.TenantId, actor, "Enquiry created", "M04Enquiry",
            enquiry.EnquiryId, null, new { enquiry.Number, enquiry.Source, enquiry.CustomerPartyId });
        if (enquiry.CustomerPartyId is { } partyId)
            AddAudit(db, tenant.TenantId, actor, "Enquiry created", "M03Party",
                partyId, null, new { enquiry.EnquiryId, enquiry.Number });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/m04/enquiries/{enquiry.EnquiryId}",
            new { enquiry.EnquiryId, enquiry.Number, enquiry.Version, duplicate = false });
    }

    private static async Task<IResult> Detail(
        Guid enquiryId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        var enquiry = await db.Set<M04Enquiry>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null || !await authorization.CanAccess("M04", user.UserId,
                "m04.enquiries.view", enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: false))
            return Results.NotFound();
        var lines = await db.Set<M04EnquiryLine>().AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId).ToListAsync(ct);
        var activities = await db.Set<M04EnquiryActivity>().AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId).OrderByDescending(x => x.At).Take(200).ToListAsync(ct);
        var followUps = await db.Set<M04FollowUp>().AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId).OrderByDescending(x => x.DueAt).ToListAsync(ct);
        return Results.Ok(new { enquiry, lines, activities, followUps });
    }

    private static async Task<IResult> Update(
        Guid enquiryId, EnquiryWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, OnboardingStore onboarding, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.edit", ct);
        ValidateWrite(request);
        var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null)
            return Results.NotFound();
        if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            return Results.NotFound();
        if (enquiry.Version != request.Version)
            return Results.Conflict(new { detail = "The enquiry changed. Reload before editing." });
        if (enquiry.Status is "Won" or "Lost")
            throw new ValidationException("Closed enquiries cannot be edited.");
        var refs = await ValidateReferences(db, tenant.TenantId, request, onboarding, ct);
        if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                refs.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            throw new UnauthorizedAccessException("The target branch is outside your assignment scope.");
        var before = Snapshot(enquiry);
        var duplicateRows = await FindDuplicates(db, request.ContactId, refs.SiteId,
            request.Lines.Select(x => x.ProductId).Distinct().ToArray(), enquiryId, ct);
        if (duplicateRows.Count > 0 && !request.ConfirmDuplicate)
            return Results.Conflict(new { duplicateWarning = true, duplicates = duplicateRows });

        enquiry.Source = request.Source;
        enquiry.ContactId = request.ContactId;
        enquiry.CustomerPartyId = refs.CustomerPartyId;
        enquiry.SiteId = refs.SiteId;
        enquiry.BranchId = refs.BranchId;
        enquiry.Priority = request.Priority;
        enquiry.RequiredBy = request.RequiredBy;
        enquiry.ExpectedValue = request.ExpectedValue;
        enquiry.NextActionAt = request.NextActionAt;
        enquiry.Notes = request.Notes.Trim();
        enquiry.UpdatedAt = DateTimeOffset.UtcNow;
        enquiry.Version++;
        db.RemoveRange(await db.Set<M04EnquiryLine>().Where(x => x.EnquiryId == enquiryId).ToListAsync(ct));
        var pendingFollowUps = await db.Set<M04FollowUp>()
            .Where(x => x.EnquiryId == enquiryId && x.Status == "Pending").ToListAsync(ct);
        foreach (var followUp in pendingFollowUps)
        {
            followUp.Status = "Cancelled";
            followUp.Version++;
        }
        db.Add(new M04FollowUp
        {
            TenantId = enquiry.TenantId, FollowUpId = Guid.NewGuid(), EnquiryId = enquiryId,
            OwnerUserId = enquiry.OwnerUserId, DueAt = request.NextActionAt!.Value,
            Action = "Call", Status = "Pending"
        });
        db.AddRange(request.Lines.Select(line => new M04EnquiryLine
        {
            TenantId = tenant.TenantId, LineId = Guid.NewGuid(), EnquiryId = enquiryId,
            ProductId = line.ProductId, Quantity = line.Quantity, UnitCode = line.UnitCode.Trim(),
            RmcGrade = Clean(line.RmcGrade), RmcSlumpBand = Clean(line.RmcSlumpBand),
            RmcAdmixtureCode = Clean(line.RmcAdmixtureCode), RmcPumpType = Clean(line.RmcPumpType)
        }));
        AddAudit(db, tenant.TenantId, actor, "Enquiry updated", "M04Enquiry", enquiryId, before, Snapshot(enquiry));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enquiry.EnquiryId, enquiry.Number, enquiry.Version });
    }

    private static async Task<IResult> ChangeStatus(
        Guid enquiryId, StatusWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.edit", ct);
        var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null || !await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            return Results.NotFound();
        if (enquiry.Version != request.Version)
            return Results.Conflict(new { detail = "The enquiry changed. Reload before continuing." });
        if (request.Status == "Won")
            throw new ValidationException("An enquiry can be marked Won only after M06 creates and links its sales order.");
        if (request.Status == "Quotation Sent")
            throw new ValidationException("Only the M05 quotation issue action may update an enquiry to Quotation Sent.");
        if (!OpenStatuses.Contains(enquiry.Status) || !IsAllowedTransition(enquiry.Status, request.Status))
            throw new ValidationException("This enquiry status transition is not allowed.");
        if (request.Status == "Lost" && (request.LostReason is null || !LostReasons.Contains(request.LostReason)))
            throw new ValidationException("Select a valid loss reason.");
        if (request.Status != "Lost" && request.LostReason is not null)
            throw new ValidationException("A loss reason is only valid when closing as Lost.");
        var before = Snapshot(enquiry);
        enquiry.Status = request.Status;
        enquiry.LostReason = request.LostReason;
        if (request.Status == "Lost") enquiry.NextActionAt = null;
        enquiry.UpdatedAt = DateTimeOffset.UtcNow;
        enquiry.Version++;
        db.Add(new M04EnquiryActivity
        {
            TenantId = enquiry.TenantId, ActivityId = Guid.NewGuid(), EnquiryId = enquiryId,
            ActorUserId = actor.UserId, Kind = "StatusChanged", Channel = "",
            Outcome = request.Status, Notes = request.LostReason ?? "", At = DateTimeOffset.UtcNow
        });
        AddAudit(db, enquiry.TenantId, actor, "Enquiry status changed", "M04Enquiry",
            enquiryId, before, Snapshot(enquiry));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enquiry.Status, enquiry.LostReason, enquiry.Version });
    }

    private static async Task<IResult> Assign(
        Guid enquiryId, AssignmentWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.assign", ct);
        var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null || !await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.assign",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            return Results.NotFound();
        if (enquiry.Version != request.Version)
            return Results.Conflict(new { detail = "The enquiry changed. Reload before reassigning." });
        await ValidateOwner(db, request.OwnerUserId, ct);
        if (!await OwnerCanAccessBranch(authorization, request.OwnerUserId, enquiry.BranchId, ct))
            throw new ValidationException("The selected owner is not assigned to this branch.");
        var previous = enquiry.OwnerUserId;
        enquiry.OwnerUserId = request.OwnerUserId;
        enquiry.Version++;
        enquiry.UpdatedAt = DateTimeOffset.UtcNow;
        db.Add(new M04AssignmentAudit
        {
            TenantId = enquiry.TenantId, AssignmentAuditId = Guid.NewGuid(), EnquiryId = enquiryId,
            PreviousOwnerUserId = previous, NewOwnerUserId = request.OwnerUserId,
            ActorUserId = actor.UserId, Reason = request.Reason.Trim(), At = DateTimeOffset.UtcNow
        });
        db.Add(new M02Notification
        {
            TenantId = enquiry.TenantId, NotificationId = Guid.NewGuid(),
            RecipientUserId = request.OwnerUserId, Kind = "M04EnquiryAssigned",
            EntityId = enquiryId, Payload = JsonSerializer.Serialize(new { enquiry.Number }, Json),
            CreatedAt = DateTimeOffset.UtcNow
        });
        AddAudit(db, enquiry.TenantId, actor, "Enquiry reassigned", "M04Enquiry",
            enquiryId, new { ownerUserId = previous }, new { ownerUserId = request.OwnerUserId, request.Reason });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enquiry.EnquiryId, enquiry.OwnerUserId, enquiry.Version });
    }

    private static async Task<IResult> AddActivity(
        Guid enquiryId, ActivityWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.edit", ct);
        var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null || !await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            return Results.NotFound();
        var kind = request.Kind.Trim();
        if (kind is not ("Call" or "Message" or "Email" or "Meeting" or "Note" or "SiteVisit"))
            throw new ValidationException("Select a valid activity type.");
        var channel = request.Channel.Trim();
        if (channel is "WhatsApp" or "SMS" or "Email")
            await RequireConsent(db, enquiry.ContactId, channel, ct);
        if (enquiry.Status is not ("Won" or "Lost") && request.NextActionAt is null)
            throw new ValidationException("An open enquiry requires a next-action date after logging an outcome.");
        var now = DateTimeOffset.UtcNow;
        db.Add(new M04EnquiryActivity
        {
            TenantId = enquiry.TenantId, ActivityId = Guid.NewGuid(), EnquiryId = enquiryId,
            ActorUserId = actor.UserId, Kind = kind, Channel = channel,
            Outcome = request.Outcome.Trim(), Notes = request.Notes.Trim(), At = now
        });
        if (enquiry.FirstRespondedAt is null)
            enquiry.FirstRespondedAt = now;
        if (enquiry.Status == "New")
            enquiry.Status = "Contacted";
        if (enquiry.Status is not ("Won" or "Lost") && request.NextActionAt is { } due)
        {
            enquiry.NextActionAt = due;
            db.Add(new M04FollowUp
            {
                TenantId = enquiry.TenantId, FollowUpId = Guid.NewGuid(), EnquiryId = enquiryId,
                OwnerUserId = enquiry.OwnerUserId, DueAt = due,
                Action = string.IsNullOrWhiteSpace(request.NextAction) ? "Call" : request.NextAction.Trim(),
                Status = "Pending"
            });
        }
        enquiry.UpdatedAt = now;
        enquiry.Version++;
        AddAudit(db, enquiry.TenantId, actor, "Enquiry activity logged", "M04Enquiry",
            enquiryId, null, new { kind, channel, outcome = request.Outcome, request.NextActionAt });
        if (enquiry.CustomerPartyId is { } partyId)
            AddAudit(db, enquiry.TenantId, actor, "Enquiry activity logged", "M03Party",
                partyId, null, new { enquiry.EnquiryId, kind, channel });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m04/enquiries/{enquiryId}",
            new { enquiry.EnquiryId, enquiry.FirstRespondedAt, enquiry.NextActionAt, enquiry.Version });
    }

    private static async Task<IResult> AssignmentHistory(
        Guid enquiryId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.view", ct);
        var enquiry = await db.Set<M04Enquiry>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null || !await authorization.CanAccess("M04", user.UserId, "m04.enquiries.view",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: false))
            return Results.NotFound();
        return Results.Ok(await db.Set<M04AssignmentAudit>().AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId).OrderByDescending(x => x.At).ToListAsync(ct));
    }

    private static async Task<IResult> FollowUps(
        string? bucket, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.view", ct);
        var scopes = await authorization.Scopes(user.UserId, "m04.enquiries.view", ct);
        var query = from followUp in db.Set<M04FollowUp>().AsNoTracking()
                    join enquiry in db.Set<M04Enquiry>().AsNoTracking()
                        on new { followUp.TenantId, followUp.EnquiryId } equals new { enquiry.TenantId, enquiry.EnquiryId }
                    where followUp.Status == "Pending"
                    select new { followUp, enquiry };
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branchIds = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x =>
                (own && x.enquiry.OwnerUserId == user.UserId) ||
                (x.enquiry.BranchId != null && branchIds.Contains(x.enquiry.BranchId.Value)));
        }
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        query = bucket switch
        {
            "Overdue" => query.Where(x => x.followUp.DueAt < today),
            "Today" => query.Where(x => x.followUp.DueAt >= today && x.followUp.DueAt < today.AddDays(1)),
            "Upcoming" => query.Where(x => x.followUp.DueAt >= today.AddDays(1)),
            _ => query
        };
        return Results.Ok(await query.OrderBy(x => x.followUp.DueAt).Take(250)
            .Select(x => new
            {
                x.followUp.FollowUpId, x.followUp.EnquiryId, x.enquiry.Number,
                x.followUp.OwnerUserId, x.followUp.DueAt, x.followUp.Action,
                x.enquiry.Status, x.enquiry.CustomerPartyId, x.enquiry.SiteId, x.enquiry.ContactId,
                x.followUp.Version
            }).ToListAsync(ct));
    }

    private static async Task<IResult> CompleteFollowUp(
        Guid followUpId, FollowUpWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.edit", ct);
        var row = await db.Set<M04FollowUp>().SingleOrDefaultAsync(x => x.FollowUpId == followUpId, ct);
        if (row is null || row.Status != "Pending")
            return Results.NotFound();
        var enquiry = await db.Set<M04Enquiry>().SingleAsync(x => x.EnquiryId == row.EnquiryId, ct);
        if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.edit",
                enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            return Results.NotFound();
        if (row.Version != request.Version)
            return Results.Conflict(new { detail = "The follow-up changed. Reload before completing." });
        if (enquiry.Status is not ("Won" or "Lost") && request.NextActionAt <= DateTimeOffset.UtcNow)
            throw new ValidationException("Choose a future next-action date for this open enquiry.");
        row.Status = "Completed";
        row.CompletedAt = DateTimeOffset.UtcNow;
        row.Outcome = request.Outcome.Trim();
        row.Version++;
        if (enquiry.Status is not ("Won" or "Lost"))
        {
            enquiry.NextActionAt = request.NextActionAt;
            enquiry.UpdatedAt = DateTimeOffset.UtcNow;
            enquiry.Version++;
            db.Add(new M04FollowUp
            {
                TenantId = row.TenantId, FollowUpId = Guid.NewGuid(), EnquiryId = row.EnquiryId,
                OwnerUserId = enquiry.OwnerUserId, DueAt = request.NextActionAt,
                Action = request.NextAction.Trim(), Status = "Pending"
            });
        }
        db.Add(new M04EnquiryActivity
        {
            TenantId = row.TenantId, ActivityId = Guid.NewGuid(), EnquiryId = row.EnquiryId,
            ActorUserId = actor.UserId, Kind = "FollowUpCompleted", Outcome = row.Outcome ?? "",
            Notes = "", At = DateTimeOffset.UtcNow
        });
        AddAudit(db, row.TenantId, actor, "Follow-up completed", "M04Enquiry", row.EnquiryId,
            null, new { row.FollowUpId, row.Outcome, request.NextActionAt });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { row.FollowUpId, row.Status, row.Version, nextActionAt = enquiry.NextActionAt });
    }

    private static async Task<IResult> Analytics(
        DateOnly? from, DateOnly? to, Guid? ownerId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var user = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.view", ct);
        var query = await ScopedEnquiries(db, authorization, user.UserId, ct);
        if (from is not null)
        {
            var start = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(x => x.CreatedAt >= start);
        }
        if (to is not null)
        {
            var end = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(x => x.CreatedAt < end);
        }
        if (ownerId is not null) query = query.Where(x => x.OwnerUserId == ownerId);
        var result = await query.GroupBy(x => new { x.Source, x.OwnerUserId, x.Status, x.LostReason })
            .Select(group => new
            {
                group.Key.Source, group.Key.OwnerUserId, group.Key.Status, group.Key.LostReason,
                Count = group.LongCount(), Value = group.Sum(x => x.ExpectedValue ?? 0m)
            }).ToListAsync(ct);
        return Results.Ok(new { result, generatedAt = DateTimeOffset.UtcNow });
    }

    private static async Task<IResult> ListAssignmentRules(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        await authorization.Require("M04", "m04.enquiries.assign", ct);
        return Results.Ok(await db.Set<M04AssignmentRule>().AsNoTracking()
            .OrderBy(x => x.Priority).ThenBy(x => x.Source).ToListAsync(ct));
    }

    private static async Task<IResult> CreateAssignmentRule(
        AssignmentRuleWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.assign", ct);
        ValidateAssignmentRule(request);
        await ValidateOwner(db, request.OwnerUserId, ct);
        if (!await OwnerCanAccessBranch(authorization, request.OwnerUserId, null, ct))
            throw new ValidationException("The assignment rule owner is not an active M04 sales user.");
        var row = new M04AssignmentRule
        {
            TenantId = tenant.TenantId, RuleId = Guid.NewGuid(), Source = request.Source,
            RegionStateCode = NormalizeRegion(request.RegionStateCode), OwnerUserId = request.OwnerUserId,
            Priority = request.Priority, IsActive = request.IsActive
        };
        db.Add(row);
        AddAudit(db, tenant.TenantId, actor, "Enquiry assignment rule created",
            "M04AssignmentRule", row.RuleId, null, row);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m04/assignment-rules/{row.RuleId}", row);
    }

    private static async Task<IResult> UpdateAssignmentRule(
        Guid ruleId, AssignmentRuleWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.assign", ct);
        ValidateAssignmentRule(request);
        var row = await db.Set<M04AssignmentRule>().SingleOrDefaultAsync(x => x.RuleId == ruleId, ct);
        if (row is null) return Results.NotFound();
        if (row.Version != request.Version)
            return Results.Conflict(new { detail = "The assignment rule changed. Reload before saving." });
        await ValidateOwner(db, request.OwnerUserId, ct);
        if (!await OwnerCanAccessBranch(authorization, request.OwnerUserId, null, ct))
            throw new ValidationException("The assignment rule owner is not an active M04 sales user.");
        var before = new { row.Source, row.RegionStateCode, row.OwnerUserId, row.Priority, row.IsActive };
        row.Source = request.Source;
        row.RegionStateCode = NormalizeRegion(request.RegionStateCode);
        row.OwnerUserId = request.OwnerUserId;
        row.Priority = request.Priority;
        row.IsActive = request.IsActive;
        row.Version++;
        AddAudit(db, tenant.TenantId, actor, "Enquiry assignment rule updated",
            "M04AssignmentRule", ruleId, before, row);
        await db.SaveChangesAsync(ct);
        return Results.Ok(row);
    }

    private static async Task<IResult> ReassignInactive(
        BulkReassignment[] requests, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.assign", ct);
        if (requests.Length is 0 or > 500)
            throw new ValidationException("Select between one and 500 enquiries.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var request in requests)
        {
            var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(x => x.EnquiryId == request.EnquiryId, ct)
                ?? throw new KeyNotFoundException("An enquiry was not found.");
            if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.assign",
                    enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
                throw new UnauthorizedAccessException("An enquiry is outside your branch assignment scope.");
            if (enquiry.Version != request.Version)
                throw new DbUpdateConcurrencyException("An enquiry changed. Reload the inactive-owner list.");
            var ownerActive = await db.Set<M02User>().AnyAsync(
                x => x.UserId == enquiry.OwnerUserId && x.Status == "Active", ct);
            if (ownerActive)
                throw new ValidationException("Bulk reassignment only accepts enquiries owned by deactivated users.");
            await ValidateOwner(db, request.NewOwnerUserId, ct);
            if (!await OwnerCanAccessBranch(authorization, request.NewOwnerUserId, enquiry.BranchId, ct))
                throw new ValidationException("The selected owner is not assigned to this branch.");
            var old = enquiry.OwnerUserId;
            enquiry.OwnerUserId = request.NewOwnerUserId;
            enquiry.Version++;
            enquiry.UpdatedAt = DateTimeOffset.UtcNow;
            db.Add(new M04AssignmentAudit
            {
                TenantId = tenant.TenantId, AssignmentAuditId = Guid.NewGuid(), EnquiryId = enquiry.EnquiryId,
                PreviousOwnerUserId = old, NewOwnerUserId = request.NewOwnerUserId,
                ActorUserId = actor.UserId, Reason = "Bulk reassignment of deactivated owner",
                At = DateTimeOffset.UtcNow
            });
        }
        AddAudit(db, tenant.TenantId, actor, "Inactive enquiry owners reassigned",
            "M04Enquiry", null, null, new { count = requests.Length });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Ok(new { reassigned = requests.Length });
    }

    private static async Task<IResult> RefreshSlaNotifications(
        TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.assign", ct);
        var candidates = await db.Set<M04Enquiry>().Where(x =>
                x.Status != "Won" && x.Status != "Lost" && x.FirstRespondedAt == null &&
                x.FirstResponseDueAt <= DateTimeOffset.UtcNow)
            .OrderBy(x => x.FirstResponseDueAt).Take(500).ToListAsync(ct);
        var managers = await (
            from user in db.Set<M02User>()
            join assignment in db.Set<M02UserRole>() on user.UserId equals assignment.UserId
            join grant in db.Set<M02RolePermission>() on assignment.RoleId equals grant.RoleId
            where user.Status == "Active" && grant.PermissionCode == "m04.enquiries.assign"
            select user.UserId
        ).Distinct().ToListAsync(ct);
        var sent = 0;
        foreach (var enquiry in candidates)
        {
            if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.assign",
                    enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
                continue;
            var inserted = await db.Database.SqlQuery<Guid>($"""
                INSERT INTO m04_sla_alert ("TenantId", "EnquiryId", "AlertedAt")
                VALUES ({tenant.TenantId}, {enquiry.EnquiryId}, {DateTimeOffset.UtcNow})
                ON CONFLICT ("TenantId", "EnquiryId") DO NOTHING
                RETURNING "EnquiryId" AS "Value"
                """).ToListAsync(ct);
            if (inserted.Count == 0) continue;
            foreach (var manager in managers)
                db.Add(new M02Notification
                {
                    TenantId = tenant.TenantId, NotificationId = Guid.NewGuid(),
                    RecipientUserId = manager, Kind = "M04FirstResponseSlaBreach",
                    EntityId = enquiry.EnquiryId,
                    Payload = JsonSerializer.Serialize(new { enquiry.Number, enquiry.OwnerUserId, enquiry.FirstResponseDueAt }, Json),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            AddAudit(db, tenant.TenantId, actor, "First response SLA breached",
                "M04Enquiry", enquiry.EnquiryId, null, new { enquiry.Number, notifiedManagers = managers.Count });
            sent++;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { breaches = candidates.Count, managerNotifications = sent });
    }

    private static async Task<IQueryable<M04Enquiry>> ScopedEnquiries(
        BerynxDbContext db, TenantAuthorizationService authorization, Guid userId, CancellationToken ct)
    {
        var scopes = await authorization.Scopes(userId, "m04.enquiries.view", ct);
        if (scopes.Any(x => x.ScopeType == "All"))
            return db.Set<M04Enquiry>();
        var branchIds = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        var own = scopes.Any(x => x.ScopeType == "Own");
        return db.Set<M04Enquiry>().Where(x =>
            (own && x.OwnerUserId == userId) ||
            (x.BranchId != null && branchIds.Contains(x.BranchId.Value)));
    }

    private sealed record EnquiryReferences(Guid? CustomerPartyId, Guid? SiteId,
        Guid? BranchId, string? RegionStateCode);

    private static async Task<EnquiryReferences> ValidateReferences(
        BerynxDbContext db, Guid tenantId, EnquiryWrite request,
        OnboardingStore onboarding, CancellationToken ct)
    {
        var customerId = request.CustomerPartyId;
        if (request.ContactId is { } contactId)
        {
            var contact = await db.Set<M03Contact>().AsNoTracking().SingleOrDefaultAsync(
                x => x.ContactId == contactId && x.IsActive, ct)
                ?? throw new ValidationException("Choose an active M03 contact.");
            if (customerId is not null && customerId != contact.PartyId)
                throw new ValidationException("The contact does not belong to the selected customer.");
            if (request.SiteId is { } selectedSite && contact.SiteId is { } contactSite && selectedSite != contactSite)
                throw new ValidationException("The contact is associated with a different customer site.");
            customerId ??= contact.PartyId;
        }
        if (customerId is { } partyId &&
            (!await db.Set<M03Party>().AnyAsync(x => x.PartyId == partyId && x.IsActive, ct) ||
             !await db.Set<M03PartyRole>().AnyAsync(x => x.PartyId == partyId && x.RoleCode == "Customer", ct)))
            throw new ValidationException("Choose an active M03 customer.");

        M03Site? site = null;
        if (request.SiteId is { } siteId)
        {
            site = await db.Set<M03Site>().AsNoTracking().SingleOrDefaultAsync(
                x => x.SiteId == siteId && x.Status == "Active", ct)
                ?? throw new ValidationException("Choose an active M03 customer site.");
            if (customerId is null || site.CustomerPartyId != customerId)
                throw new ValidationException("The site must belong to the selected customer.");
        }
        if (customerId is null && request.ContactId is null)
            throw new ValidationException("Select a contact or customer.");
        if (request.Lines.Length == 0)
            throw new ValidationException("Add at least one product interest line.");
        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0 || decimal.Round(line.Quantity, 3) != line.Quantity)
                throw new ValidationException("Product quantities must be positive with up to three decimals.");
            var product = await db.Set<M03Product>().AsNoTracking().SingleOrDefaultAsync(
                x => x.ProductId == line.ProductId && x.IsActive, ct)
                ?? throw new ValidationException("Choose an active M03 product.");
            if (!string.Equals(product.BaseUnitCode, line.UnitCode.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Use the product's configured base unit.");
            if (product.Category == "RMC")
            {
                if (string.IsNullOrWhiteSpace(line.RmcGrade) ||
                    string.IsNullOrWhiteSpace(line.RmcSlumpBand) ||
                    string.IsNullOrWhiteSpace(line.RmcAdmixtureCode) ||
                    string.IsNullOrWhiteSpace(line.RmcPumpType))
                    throw new ValidationException("RMC enquiries require grade, slump, admixture and pump requirements.");
                var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
                if (!await db.Set<M03RmcRateCard>().AnyAsync(x =>
                        x.ProductId == line.ProductId && x.Grade == line.RmcGrade &&
                        x.SlumpBand == line.RmcSlumpBand && x.AdmixtureCode == line.RmcAdmixtureCode &&
                        x.PumpType == line.RmcPumpType && x.IsActive && x.IsApproved &&
                        x.EffectiveFrom <= asOf && (x.EffectiveTo == null || x.EffectiveTo >= asOf), ct))
                    throw new ValidationException("No approved M03 RMC rate card matches this enquiry specification.");
            }
            else if (line.RmcGrade is not null || line.RmcSlumpBand is not null ||
                     line.RmcAdmixtureCode is not null || line.RmcPumpType is not null)
                throw new ValidationException("RMC grade and delivery requirements are valid only for RMC products.");
        }
        var branchId = site?.BranchId ?? request.BranchId;
        if (site?.BranchId is { } siteBranch && request.BranchId is { } requestedBranch && siteBranch != requestedBranch)
            throw new ValidationException("The selected site belongs to a different branch.");
        if (branchId is { } branchKey)
        {
            if (!OnboardingStore.Decode(await onboarding.Current()).Branches.Any(x => x.Id == branchKey))
                throw new ValidationException("Choose a branch configured for this tenant.");
        }
        return new(customerId, site?.SiteId, branchId, site?.StateCode);
    }

    private static async Task<List<object>> FindDuplicates(
        BerynxDbContext db, Guid? contactId, Guid? siteId, Guid[] productIds, Guid? excludeId,
        CancellationToken ct)
    {
        if (contactId is null || siteId is null || productIds.Length == 0)
            return [];
        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var rows = await (
            from enquiry in db.Set<M04Enquiry>().AsNoTracking()
            join line in db.Set<M04EnquiryLine>().AsNoTracking()
                on new { enquiry.TenantId, enquiry.EnquiryId } equals new { line.TenantId, line.EnquiryId }
            where enquiry.ContactId == contactId && enquiry.SiteId == siteId &&
                  enquiry.CreatedAt >= since && enquiry.Status != "Won" && enquiry.Status != "Lost" &&
                  (excludeId == null || enquiry.EnquiryId != excludeId) &&
                  productIds.Contains(line.ProductId)
            select new { enquiry.EnquiryId, enquiry.Number, line.ProductId, enquiry.Status }
        ).Distinct().ToListAsync(ct);
        return rows.Select(x => (object)x).ToList();
    }

    private static async Task<Guid> SelectOwner(
        BerynxDbContext db, Guid tenantId, TenantAuthorizationService authorization, M02User actor,
        string source, string? region, Guid? branch, CancellationToken ct)
    {
        var rule = await db.Set<M04AssignmentRule>().AsNoTracking()
            .Where(x => x.IsActive && x.Source == source &&
                        (x.RegionStateCode == null || x.RegionStateCode == region))
            .OrderByDescending(x => x.RegionStateCode != null).ThenBy(x => x.Priority)
            .FirstOrDefaultAsync(ct);
        if (rule is not null)
        {
            await ValidateOwner(db, rule.OwnerUserId, ct);
            if (!await OwnerCanAccessBranch(authorization, rule.OwnerUserId, branch, ct))
                throw new ValidationException("The matching assignment rule owner is not assigned to the enquiry branch.");
            return rule.OwnerUserId;
        }
        var candidates = await (
            from user in db.Set<M02User>()
            join assignment in db.Set<M02UserRole>() on user.UserId equals assignment.UserId
            join grant in db.Set<M02RolePermission>() on assignment.RoleId equals grant.RoleId
            where user.Status == "Active" && grant.PermissionCode == "m04.enquiries.view"
            select user.UserId
        ).Distinct().OrderBy(x => x).ToListAsync(ct);
        if (branch is not null)
        {
            var eligible = new List<Guid>();
            foreach (var candidate in candidates)
                if (await OwnerCanAccessBranch(authorization, candidate, branch, ct))
                    eligible.Add(candidate);
            candidates = eligible;
        }
        if (candidates.Count == 0)
            return actor.UserId;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO m04_assignment_cursor ("TenantId", "Source", "LastOwnerUserId")
            VALUES ({tenantId}, {source}, NULL)
            ON CONFLICT ("TenantId", "Source") DO NOTHING
            """, ct);
        var cursor = await db.Set<M04AssignmentCursor>().FromSqlInterpolated($"""
            SELECT * FROM m04_assignment_cursor
            WHERE "TenantId"={tenantId} AND "Source"={source}
            FOR UPDATE
            """).SingleAsync(ct);
        var index = cursor.LastOwnerUserId is { } last ? candidates.IndexOf(last) : -1;
        var ownerId = candidates[(index + 1) % candidates.Count];
        cursor.LastOwnerUserId = ownerId;
        await db.SaveChangesAsync(ct);
        return ownerId;
    }

    private static async Task<Guid> ValidateRequestedOwner(
        BerynxDbContext db, TenantAuthorizationService authorization, M02User actor,
        Guid ownerId, Guid? branchId, CancellationToken ct)
    {
        if (!await authorization.CanAccess("M04", actor.UserId, "m04.enquiries.assign",
                branchId, null, ownerId, ct, write: true))
            throw new UnauthorizedAccessException("You cannot assign this enquiry.");
        await ValidateOwner(db, ownerId, ct);
        if (!await OwnerCanAccessBranch(authorization, ownerId, branchId, ct))
            throw new ValidationException("The selected owner is not assigned to this branch.");
        return ownerId;
    }

    private static async Task ValidateOwner(BerynxDbContext db, Guid ownerId, CancellationToken ct)
    {
        var allowed = await (
            from user in db.Set<M02User>()
            join assignment in db.Set<M02UserRole>() on user.UserId equals assignment.UserId
            join grant in db.Set<M02RolePermission>() on assignment.RoleId equals grant.RoleId
            where user.UserId == ownerId && user.Status == "Active" &&
                  grant.PermissionCode == "m04.enquiries.view"
            select user.UserId
        ).AnyAsync(ct);
        if (!allowed)
            throw new ValidationException("Assign the enquiry to an active M04 sales user.");
    }

    private static async Task<bool> OwnerCanAccessBranch(
        TenantAuthorizationService authorization, Guid ownerId, Guid? branchId, CancellationToken ct)
    {
        if (branchId is null)
            return true;
        var scopes = await authorization.Scopes(ownerId, "m04.enquiries.view", ct);
        return scopes.Any(x => x.ScopeType == "All" ||
                               x.ScopeType == "Branch" && x.ScopeId == branchId ||
                               x.ScopeType == "Own");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task RequireConsent(BerynxDbContext db, Guid? contactId, string channel, CancellationToken ct)
    {
        if (contactId is null)
            throw new ValidationException("A contact with recorded consent is required for this channel.");
        var contact = await db.Set<M03Contact>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ContactId == contactId && x.IsActive, ct);
        var consent = channel switch
        {
            "WhatsApp" => contact?.WhatsAppConsent == true,
            "SMS" => contact?.SmsConsent == true,
            "Email" => contact?.EmailConsent == true,
            _ => true
        };
        if (!consent)
            throw new ValidationException($"The contact has not consented to {channel} communication.");
    }

    private static async Task<int> FirstResponseSla(BerynxDbContext db, Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Set<Tenant>().AsNoTracking().SingleAsync(x => x.TenantId == tenantId, ct);
        return Math.Clamp(OnboardingStore.Decode(tenant).M04FirstResponseSlaMinutes, 1, 1440);
    }

    private static void ValidateWrite(EnquiryWrite request)
    {
        if (!Sources.Contains(request.Source))
            throw new ValidationException("Choose a valid enquiry source.");
        if (request.Priority is not ("Low" or "Normal" or "High" or "Urgent"))
            throw new ValidationException("Choose a valid priority.");
        if (request.ExpectedValue is < 0 or > MaximumExpectedValue ||
            request.ExpectedValue is { } expectedValue && decimal.Round(expectedValue, 2) != expectedValue)
            throw new ValidationException("Expected value must be non-negative and fit currency precision.");
        if (string.IsNullOrWhiteSpace(request.Notes) || request.Notes.Length > 4000)
            throw new ValidationException("Add a brief enquiry note up to 4,000 characters.");
        if (request.NextActionAt is null)
            throw new ValidationException("Every open enquiry requires a next-action date.");
        if (request.Lines is null || request.Lines.Length > 100)
            throw new ValidationException("Add between one and 100 product lines.");
    }

    private static async Task<IResult> ReopenLost(
        Guid enquiryId, StatusWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M04", "m04.enquiries.edit", ct);
        var enquiry = await db.Set<M04Enquiry>().SingleOrDefaultAsync(x => x.EnquiryId == enquiryId, ct);
        if (enquiry is null || !await authorization.CanAccess("M04", actor.UserId,
                "m04.enquiries.edit", enquiry.BranchId, null, enquiry.OwnerUserId, ct, write: true))
            return Results.NotFound();
        if (enquiry.Version != request.Version)
            return Results.Conflict(new { detail = "The enquiry changed. Reload before reopening." });
        if (enquiry.Status != "Lost" || request.Status is not ("New" or "Contacted" or "Follow-up" or "Negotiation" or "On hold") ||
            request.LostReason is not null || request.NextActionAt is null || request.NextActionAt <= DateTimeOffset.UtcNow)
            throw new ValidationException("Reopen a lost enquiry to an open stage only after setting a future next-action date.");

        var before = Snapshot(enquiry);
        enquiry.Status = request.Status;
        enquiry.LostReason = null;
        enquiry.NextActionAt = request.NextActionAt;
        enquiry.UpdatedAt = DateTimeOffset.UtcNow;
        enquiry.Version++;
        db.Add(new M04FollowUp
        {
            TenantId = enquiry.TenantId, FollowUpId = Guid.NewGuid(), EnquiryId = enquiryId,
            OwnerUserId = enquiry.OwnerUserId, DueAt = enquiry.NextActionAt.Value,
            Action = "Call", Status = "Pending"
        });
        db.Add(new M04EnquiryActivity
        {
            TenantId = enquiry.TenantId, ActivityId = Guid.NewGuid(), EnquiryId = enquiryId,
            ActorUserId = actor.UserId, Kind = "Reopened", Outcome = request.Status,
            Notes = "Lost enquiry reopened with a new follow-up.", At = enquiry.UpdatedAt
        });
        AddAudit(db, enquiry.TenantId, actor, "Lost enquiry reopened", "M04Enquiry",
            enquiryId, before, Snapshot(enquiry));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enquiry.EnquiryId, enquiry.Status, enquiry.NextActionAt, enquiry.Version });
    }

    private static bool IsAllowedTransition(string current, string next) =>
        (current, next) switch
        {
            ("New", "Contacted" or "On hold" or "Lost") => true,
            ("Contacted", "Follow-up" or "Quotation Sent" or "Negotiation" or "On hold" or "Lost") => true,
            ("Follow-up", "Contacted" or "Quotation Sent" or "Negotiation" or "On hold" or "Lost") => true,
            ("Quotation Sent", "Negotiation" or "On hold" or "Lost") => true,
            ("Negotiation", "Quotation Sent" or "On hold" or "Lost") => true,
            ("On hold", "New" or "Contacted" or "Follow-up" or "Quotation Sent" or "Negotiation" or "Lost") => true,
            _ => false
        };

    private static void ValidateAssignmentRule(AssignmentRuleWrite request)
    {
        if (!Sources.Contains(request.Source) || request.Priority < 0 ||
            request.RegionStateCode is { Length: > 0 } state && state.Length != 2)
            throw new ValidationException("Enter a valid source, optional two-character region and non-negative priority.");
    }

    private static string? NormalizeRegion(string? region) =>
        string.IsNullOrWhiteSpace(region) ? null : region.Trim().ToUpperInvariant();

    private static object Snapshot(M04Enquiry enquiry) => new
    {
        enquiry.Number, enquiry.Source, enquiry.ContactId, enquiry.CustomerPartyId, enquiry.SiteId,
        enquiry.BranchId, enquiry.OwnerUserId, enquiry.Status, enquiry.Priority, enquiry.RequiredBy,
        enquiry.ExpectedValue, enquiry.NextActionAt, enquiry.LostReason, enquiry.Version
    };

    private static void AddAudit(BerynxDbContext db, Guid tenantId, M02User actor, string action,
        string entityType, Guid? entityId, object? before, object? after) =>
        db.Add(new M02AuditEvent
        {
            TenantId = tenantId, EventId = Guid.NewGuid(), ActorUserId = actor.UserId,
            ActorSubject = actor.IdentityUserId?.ToString("D") ?? actor.UserId.ToString("D"),
            Action = action, EntityType = entityType, EntityId = entityId,
            Before = JsonSerializer.Serialize(before ?? new { }, Json),
            After = JsonSerializer.Serialize(after ?? new { }, Json),
            At = DateTimeOffset.UtcNow
        });
}
