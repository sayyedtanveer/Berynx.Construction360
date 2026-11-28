using System.Data;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Quality;
using Berynx.Application.Tenancy;
using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;
using Berynx.Domain.Documents;
using Berynx.Domain.Invoicing;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Quality;
using Berynx.Domain.SalesOrders;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Quality;

public static class M19Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapM19(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m19").RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var auth = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await auth.RequireModule("M19", !HttpMethods.IsGet(context.HttpContext.Request.Method), cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/trip-options", TripOptions);
        api.MapGet("/samples", Samples);
        api.MapPost("/samples", CreateSample);
        api.MapPost("/samples/{sampleId:guid}/tests/{ageDays:int}", RecordTest);
        api.MapGet("/criteria", Criteria);
        api.MapPost("/criteria", SaveCriteria);
        api.MapGet("/disputes", Disputes);
        api.MapGet("/disputes/{disputeId:guid}/history", DisputeHistory);
        api.MapPost("/disputes/{disputeId:guid}/evidence/{documentId:guid}", AddDisputeEvidence);
        api.MapGet("/samples/{sampleId:guid}/history", SampleHistory);
        api.MapPost("/reminders/refresh", RefreshReminders);
        api.MapPost("/disputes", CreateDispute);
        api.MapPost("/disputes/{disputeId:guid}/status", ChangeDispute);
        api.MapGet("/reports", QualityReport);
    }

    private static async Task<IResult> TripOptions(TenantAuthorizationService auth, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct);
        await auth.Require("M19", "m19.samples.view", ct);
        var scopes = await auth.Scopes(actor.UserId, "m19.samples.view", ct);
        var query = ScopeTrips(db.Set<M08Trip>().AsNoTracking().Where(x => x.Status == "Completed"), scopes, actor.UserId);
        var rows = await (from trip in query
            join pour in db.Set<M08Pour>().AsNoTracking() on new { trip.TenantId, trip.PourId } equals new { pour.TenantId, pour.PourId }
            orderby trip.CompletedAt descending
            select new { trip.TripId, trip.Number, trip.PourId, trip.BranchId, trip.PlantId, trip.CompletedAt, trip.DeliveredQuantity,
                pour.SalesOrderId, pour.SalesOrderLineId, pour.SiteId, pour.ProductId, pour.ProductNameSnapshot, pour.CustomerNameSnapshot, pour.SiteNameSnapshot })
            .Take(250).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> Samples(TenantAuthorizationService auth, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct);
        await auth.Require("M19", "m19.samples.view", ct);
        var scopes = await auth.Scopes(actor.UserId, "m19.samples.view", ct);
        var rows = await ScopeSamples(db, db.Set<M19QualitySample>().AsNoTracking(), scopes, actor.UserId)
            .OrderByDescending(x => x.CastAt).Take(250).ToListAsync(ct);
        var ids = rows.Select(x => x.SampleId).ToArray();
        var tests = await db.Set<M19QualityTest>().AsNoTracking().Where(x => ids.Contains(x.SampleId)).OrderBy(x => x.TestAgeDays).ToListAsync(ct);
        return Results.Ok(rows.Select(s => new { s.SampleId, s.SampleNumber, s.BranchId, s.TripId, s.PourId, s.SalesOrderId,
            s.SalesOrderLineId, s.SiteId, s.ProductId, s.MaterialType, s.GradeSnapshot, s.CastAt, s.CubeCount, s.OwnerUserId, s.Status, s.Version,
            tests = tests.Where(t => t.SampleId == s.SampleId) }));
    }

    private static async Task<IResult> CreateSample(M19CreateSampleRequest request, TenantAuthorizationService auth,
        ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct);
        await auth.Require("M19", "m19.samples.create", ct);
        if (request.IdempotencyKey == Guid.Empty) return Results.ValidationProblem(new Dictionary<string, string[]> { ["idempotencyKey"] = ["A client-generated request key is required."] });
        var prior = await db.Set<M19QualitySample>().SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (prior is not null) return Results.Ok(new { prior.SampleId, prior.SampleNumber, prior.Status, prior.Version });
        var type = M19Rules.NormalizeMaterialType(request.MaterialType);
        var grade = M19Rules.NormalizeGrade(request.Grade);
        if (request.TripId == Guid.Empty || request.CubeCount is not (3 or 6) || request.CastAt > DateTimeOffset.UtcNow.AddMinutes(5))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["sample"] = ["Select a completed trip, record 3 or 6 cubes, and provide a valid cast date."] });
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == request.TripId && x.Status == "Completed", ct);
        if (trip is null) return Results.NotFound(new { detail = "A completed tenant trip was not found." });
        if (!await auth.CanAccess("M19", actor.UserId, "m19.samples.create", trip.BranchId, trip.PlantId, actor.UserId, ct)) return Results.NotFound();
        var pour = await db.Set<M08Pour>().SingleOrDefaultAsync(x => x.PourId == trip.PourId, ct);
        if (pour is null || pour.SalesOrderId == Guid.Empty || pour.SalesOrderLineId == Guid.Empty)
            return Results.Conflict(new { detail = "The completed trip is missing its sales-order references." });
        if (!await db.Set<M06SalesOrder>().AnyAsync(x => x.SalesOrderId == pour.SalesOrderId, ct))
            return Results.Conflict(new { detail = "The source sales order is no longer available." });
        var owner = request.OwnerUserId ?? actor.UserId;
        if (!await db.Set<Berynx.Domain.IdentityAccess.M02User>().AnyAsync(x => x.UserId == owner && x.Status == "Active", ct))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["ownerUserId"] = ["Choose an active tenant user."] });
        var castDate = DateOnly.FromDateTime(request.CastAt.UtcDateTime);
        var found = await db.Set<M19QualityCriteria>().AsNoTracking().Where(x => x.IsActive && x.MaterialType == type && x.Grade == grade &&
            x.EffectiveFrom <= castDate && (x.EffectiveUntil == null || x.EffectiveUntil >= castDate))
            .OrderByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        var criteria = found.GroupBy(x => x.TestAgeDays).ToDictionary(g => g.Key, g => g.First());
        var ages = type == "Concrete" ? new[] { 7, 28 } : criteria.Keys.Order().ToArray();
        var sampleId = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var sample = new M19QualitySample
        {
            TenantId = tenant.TenantId, SampleId = sampleId, IdempotencyKey = request.IdempotencyKey, BranchId = trip.BranchId, TripId = trip.TripId,
            SalesOrderId = pour.SalesOrderId, SalesOrderLineId = pour.SalesOrderLineId, SiteId = pour.SiteId, ProductId = pour.ProductId,
            SampleNumber = $"QS-{now:yyMMdd}-{sampleId.ToString("N")[..8].ToUpperInvariant()}", MaterialType = type, GradeSnapshot = grade,
            CastAt = request.CastAt, CubeCount = request.CubeCount, OwnerUserId = owner, CreatedAt = now, CreatedByUserId = actor.UserId, UpdatedAt = now,
            Status = ages.Length == 0 || ages.Any(a => !criteria.ContainsKey(a)) ? M19Rules.ReviewRequired : M19Rules.Scheduled
        };
        db.Add(sample);
        foreach (var age in ages)
        {
            criteria.TryGetValue(age, out var rule);
            db.Add(new M19QualityTest { TenantId = tenant.TenantId, TestId = Guid.NewGuid(), SampleId = sampleId, TestAgeDays = age,
                DueAt = M19Rules.DueAt(request.CastAt, age), MinimumStrengthMpaSnapshot = rule?.MinimumStrengthMpa,
                AcceptanceReferenceSnapshot = rule?.StandardReference ?? "", Status = rule is null ? M19Rules.ReviewRequired : M19Rules.Scheduled,
                Outcome = rule is null ? M19Rules.ReviewRequired : "Pending", CreatedAt = now });
        }
        AddEvent(db, tenant.TenantId, actor.UserId, "Sample", sampleId, "SampleRegistered", new { sample.SampleNumber, sample.TripId, type, grade, sample.CastAt, sample.CubeCount });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var existing = await db.Set<M19QualitySample>().SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing is not null) return Results.Ok(new { existing.SampleId, existing.SampleNumber, existing.Status, existing.Version });
            return Results.Conflict(new { detail = "Sample creation conflicted with a concurrent request. Retry with the same request key." });
        }
        return Results.Created($"/api/v1/m19/samples/{sampleId}", new { sample.SampleId, sample.SampleNumber, sample.Status, sample.Version });
    }
    private static async Task<IResult> RecordTest(Guid sampleId, int ageDays, M19RecordTestRequest request,
        TenantAuthorizationService auth, ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct);
        await auth.Require("M19", "m19.samples.result", ct);
        if (request.StrengthMpa < 0 || request.StrengthMpa > 1000 || request.TestedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["result"] = ["Enter a valid non-negative result and test date."] });
        var sample = await db.Set<M19QualitySample>().SingleOrDefaultAsync(x => x.SampleId == sampleId, ct);
        if (sample is null || !await auth.CanAccess("M19", actor.UserId, "m19.samples.result", sample.BranchId, null, sample.OwnerUserId, ct)) return Results.NotFound();
        var test = await db.Set<M19QualityTest>().SingleOrDefaultAsync(x => x.SampleId == sampleId && x.TestAgeDays == ageDays, ct);
        if (test is null) return Results.NotFound(new { detail = "The scheduled test age was not found." });
        if (test.Version != request.ExpectedVersion) return Results.Conflict(new { detail = "This test record changed. Refresh it before recording a result." });
        if (request.TestedAt < sample.CastAt) return Results.ValidationProblem(new Dictionary<string, string[]> { ["testedAt"] = ["A test cannot predate the cast time."] });
        var reason = request.Reason?.Trim() ?? "";
        if (test.StrengthMpa.HasValue && string.IsNullOrWhiteSpace(reason)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["reason"] = ["Explain why an existing test result is being corrected."] });
        if (reason.Length > 500) return Results.ValidationProblem(new Dictionary<string, string[]> { ["reason"] = ["Reason cannot exceed 500 characters."] });
        if (request.ReportDocumentId is { } docId && !await db.Set<M18Document>().AnyAsync(x => x.DocumentId == docId && x.TripId == sample.TripId && x.IsCurrent && x.ScanStatus == M18DocumentRules.ScanClean && x.StorageStatus == M18DocumentRules.StorageAvailable, ct))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["reportDocumentId"] = ["Use a current, clean-scanned M18 document belonging to this trip."] });

        var previous = new { test.StrengthMpa, test.Outcome, test.ReportDocumentId, test.TestedAt };
        test.StrengthMpa = request.StrengthMpa; test.TestedAt = request.TestedAt; test.ReportDocumentId = request.ReportDocumentId;
        test.RecordedByUserId = actor.UserId; test.ResultReason = reason; test.Outcome = M19Rules.Evaluate(request.StrengthMpa, test.MinimumStrengthMpaSnapshot);
        test.Status = "Tested"; test.Version++; sample.Version++; sample.UpdatedAt = DateTimeOffset.UtcNow;
        var allTests = await db.Set<M19QualityTest>().Where(x => x.SampleId == sampleId).ToListAsync(ct);
        sample.Status = M19Rules.RecomputeSampleStatus(allTests);
        AddEvent(db, tenant.TenantId, actor.UserId, "Test", test.TestId, "TestResultRecorded", new { previous, test.StrengthMpa, test.Outcome, ageDays, reason });
        if (test.TestAgeDays >= 28 && test.Outcome == M19Rules.Fail) await OpenQualityDisputeAndHoldInvoices(sample, actor.UserId, tenant.TenantId, db, ct);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(new { detail = "The quality record changed concurrently. Refresh and retry." }); }
        return Results.Ok(new { test.TestId, test.TestAgeDays, test.DueAt, test.StrengthMpa, test.Outcome, sample.Status, test.Version });
    }

    private static async Task<IResult> Criteria(TenantAuthorizationService auth, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct);
        await auth.Require("M19", "m19.criteria.view", ct);
        var rows = await db.Set<M19QualityCriteria>().AsNoTracking().OrderBy(x => x.MaterialType).ThenBy(x => x.Grade)
            .ThenBy(x => x.TestAgeDays).ThenByDescending(x => x.EffectiveFrom)
            .Select(x => new { x.CriteriaId, x.MaterialType, x.Grade, x.TestAgeDays, x.MinimumStrengthMpa, x.StandardReference, x.EffectiveFrom, x.EffectiveUntil, x.ChangeReason, x.CreatedAt, x.Version })
            .ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> SaveCriteria(M19SaveCriteriaRequest request, TenantAuthorizationService auth,
        ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct);
        await auth.Require("M19", "m19.criteria.manage", ct);
        var type = M19Rules.NormalizeMaterialType(request.MaterialType); var grade = M19Rules.NormalizeGrade(request.Grade);
        var reference = request.StandardReference?.Trim() ?? ""; var reason = request.ChangeReason?.Trim() ?? "";
        M19Rules.ValidateCriteria(type, grade, request.TestAgeDays, request.MinimumStrengthMpa, reference, reason);
        if (request.EffectiveUntil < request.EffectiveFrom) return Results.ValidationProblem(new Dictionary<string, string[]> { ["effectiveUntil"] = ["End date cannot precede start date."] });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var overlap = await db.Set<M19QualityCriteria>().AnyAsync(x => x.IsActive && x.MaterialType == type && x.Grade == grade && x.TestAgeDays == request.TestAgeDays &&
            (x.EffectiveUntil == null || x.EffectiveUntil >= request.EffectiveFrom) && (request.EffectiveUntil == null || x.EffectiveFrom <= request.EffectiveUntil), ct);
        if (overlap) return Results.Conflict(new { detail = "An active acceptance criterion already overlaps this effective period." });
        var row = new M19QualityCriteria { TenantId = tenant.TenantId, CriteriaId = Guid.NewGuid(), MaterialType = type, Grade = grade,
            TestAgeDays = request.TestAgeDays, MinimumStrengthMpa = request.MinimumStrengthMpa, StandardReference = reference,
            EffectiveFrom = request.EffectiveFrom, EffectiveUntil = request.EffectiveUntil, ChangeReason = reason,
            CreatedByUserId = actor.UserId, CreatedAt = DateTimeOffset.UtcNow };
        db.Add(row);
        AddEvent(db, tenant.TenantId, actor.UserId, "Criteria", row.CriteriaId, "AcceptanceCriteriaCreated",
            new { row.MaterialType, row.Grade, row.TestAgeDays, row.MinimumStrengthMpa, row.StandardReference, row.EffectiveFrom, row.EffectiveUntil, row.ChangeReason });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Results.Created($"/api/v1/m19/criteria/{row.CriteriaId}", new { row.CriteriaId, row.MaterialType, row.Grade, row.TestAgeDays, row.MinimumStrengthMpa, row.StandardReference, row.EffectiveFrom, row.Version });
    }

    private static async Task<IResult> Disputes(TenantAuthorizationService auth, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct); await auth.Require("M19", "m19.disputes.view", ct);
        var scopes = await auth.Scopes(actor.UserId, "m19.disputes.view", ct);
        var rows = await ScopeDisputes(db, db.Set<M19Dispute>().AsNoTracking(), scopes, actor.UserId).OrderByDescending(x => x.OpenedAt).Take(250).ToListAsync(ct);
        var ids = rows.Select(x => x.DisputeId).ToArray();
        var invoices = await db.Set<M19DisputeInvoice>().AsNoTracking().Where(x => ids.Contains(x.DisputeId)).ToListAsync(ct);
        var evidence = await db.Set<M19DisputeEvidence>().AsNoTracking().Where(x => ids.Contains(x.DisputeId)).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new { x.DisputeId, x.BranchId, x.CustomerPartyId, x.SupplierPartyId, x.SiteId, x.SalesOrderId,
            x.TripId, x.ChallanId, x.SampleId, x.Type, x.Description, x.OwnerUserId, x.Status, x.Resolution, x.OpenedAt, x.UpdatedAt, x.Version,
            invoiceIds = invoices.Where(i => i.DisputeId == x.DisputeId).Select(i => i.InvoiceId),
            evidenceDocumentIds = evidence.Where(i => i.DisputeId == x.DisputeId).Select(i => i.DocumentId) }));
    }
    private static async Task<IResult> CreateDispute(M19CreateDisputeRequest request, TenantAuthorizationService auth,
        ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct); await auth.Require("M19", "m19.disputes.create", ct);
        if (request.IdempotencyKey == Guid.Empty) return Results.ValidationProblem(new Dictionary<string, string[]> { ["idempotencyKey"] = ["A client-generated request key is required."] });
        var prior = await db.Set<M19Dispute>().SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (prior is not null) return Results.Ok(new { prior.DisputeId, prior.Status, prior.Version });
        var type = request.Type?.Trim(); var description = request.Description?.Trim() ?? "";
        if (type is not ("Quality" or "Quantity" or "Delay" or "Billing" or "Other") || description.Length is < 1 or > 2000)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["dispute"] = ["Choose a dispute type and provide a description up to 2,000 characters."] });
        if (request.SalesOrderId is null && request.TripId is null && request.ChallanId is null && request.InvoiceId is null && request.SampleId is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["source"] = ["Link the dispute to an order, trip, challan, invoice, or quality sample."] });
        var orderId = request.SalesOrderId; var tripId = request.TripId; var challanId = request.ChallanId;
        var branchId = Guid.Empty; Guid? plantId = null; var siteId = request.SiteId; var customerId = request.CustomerPartyId;
        if (request.SampleId is { } sampleId)
        {
            var sample = await db.Set<M19QualitySample>().SingleOrDefaultAsync(x => x.SampleId == sampleId, ct);
            if (sample is null) return Results.NotFound();
            if (tripId.HasValue && tripId != sample.TripId || orderId.HasValue && orderId != sample.SalesOrderId)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["source"] = ["Sample, order and trip references must match."] });
            tripId = sample.TripId; orderId = sample.SalesOrderId; siteId ??= sample.SiteId; branchId = sample.BranchId;
        }
        if (tripId is { } tid)
        {
            var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tid, ct);
            if (trip is null) return Results.NotFound();
            var pour = await db.Set<M08Pour>().SingleOrDefaultAsync(x => x.PourId == trip.PourId, ct);
            if (pour is null) return Results.NotFound();
            if (orderId.HasValue && orderId != pour.SalesOrderId) return Results.ValidationProblem(new Dictionary<string, string[]> { ["salesOrderId"] = ["Trip does not belong to the selected order."] });
            orderId = pour.SalesOrderId; branchId = trip.BranchId; plantId = trip.PlantId; siteId ??= pour.SiteId;
        }
        if (challanId is { } challanKey)
        {
            var challan = await db.Set<M09DeliveryChallan>().SingleOrDefaultAsync(x => x.ChallanId == challanKey, ct);
            if (challan is null) return Results.NotFound();
            if (orderId.HasValue && orderId != challan.SalesOrderId || tripId.HasValue && tripId != challan.TripId)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["challanId"] = ["Challan does not match the selected order or trip."] });
            orderId = challan.SalesOrderId; tripId = challan.TripId; branchId = challan.BranchId; siteId ??= challan.SiteId; customerId ??= challan.CustomerPartyId;
        }
        if (request.InvoiceId is { } invoiceId)
        {
            var invoice = await db.Set<M10Invoice>().SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
            if (invoice is null) return Results.NotFound();
            if (branchId != Guid.Empty && branchId != invoice.BranchId) return Results.ValidationProblem(new Dictionary<string, string[]> { ["invoiceId"] = ["Invoice and delivery source belong to different branches."] });
            branchId = invoice.BranchId; siteId ??= invoice.SiteId; customerId ??= invoice.CustomerPartyId;
        }
        if (orderId is { } orderKey)
        {
            var order = await db.Set<M06SalesOrder>().SingleOrDefaultAsync(x => x.SalesOrderId == orderKey, ct);
            if (order is null) return Results.NotFound();
            if (order.BranchId.HasValue && branchId != Guid.Empty && order.BranchId != branchId)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["salesOrderId"] = ["Order does not belong to the selected branch."] });
            branchId = order.BranchId ?? branchId; siteId ??= order.SiteId; customerId ??= order.CustomerPartyId;
        }
        if (branchId == Guid.Empty) return Results.ValidationProblem(new Dictionary<string, string[]> { ["source"] = ["A branch-scoped source document is required."] });
        if (customerId.HasValue && !await db.Set<M03Party>().AnyAsync(x => x.PartyId == customerId, ct)) return Results.NotFound();
        if (request.SupplierPartyId.HasValue && !await db.Set<M03Party>().AnyAsync(x => x.PartyId == request.SupplierPartyId, ct)) return Results.NotFound();
        var owner = request.OwnerUserId ?? actor.UserId;
        if (!await db.Set<Berynx.Domain.IdentityAccess.M02User>().AnyAsync(x => x.UserId == owner && x.Status == "Active", ct))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["ownerUserId"] = ["Choose an active tenant user."] });
        if (!await auth.CanAccess("M19", actor.UserId, "m19.disputes.create", branchId, plantId, owner, ct)) return Results.NotFound();

        var dispute = new M19Dispute { TenantId = tenant.TenantId, DisputeId = Guid.NewGuid(), IdempotencyKey = request.IdempotencyKey, BranchId = branchId,
            CustomerPartyId = customerId, SupplierPartyId = request.SupplierPartyId, SiteId = siteId, SalesOrderId = orderId,
            TripId = tripId, ChallanId = challanId, SampleId = request.SampleId, Type = type!, Description = description,
            OwnerUserId = owner, Status = "Open", OpenedAt = DateTimeOffset.UtcNow, OpenedByUserId = actor.UserId, UpdatedAt = DateTimeOffset.UtcNow };
        db.Add(dispute);
        if (request.InvoiceId is { } linkedInvoice) db.Add(new M19DisputeInvoice { TenantId = tenant.TenantId, DisputeId = dispute.DisputeId, InvoiceId = linkedInvoice });
        await AddInvoiceHolds(db, tenant.TenantId, actor.UserId, dispute, ct);
        AddEvent(db, tenant.TenantId, actor.UserId, "Dispute", dispute.DisputeId, "DisputeOpened", new { dispute.Type, dispute.Description, dispute.TripId, dispute.SalesOrderId, request.InvoiceId });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var existing = await db.Set<M19Dispute>().SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing is not null) return Results.Ok(new { existing.DisputeId, existing.Status, existing.Version });
            return Results.Conflict(new { detail = "Dispute creation conflicted with a concurrent request. Retry with the same request key." });
        }
        return Results.Created($"/api/v1/m19/disputes/{dispute.DisputeId}", new { dispute.DisputeId, dispute.Status, dispute.Version });
    }

    private static async Task<IResult> ChangeDispute(Guid disputeId, M19ChangeDisputeRequest request,
        TenantAuthorizationService auth, ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct);
        var status = request.Status?.Trim();
        var permission = status == "Resolved" ? "m19.disputes.resolve" : "m19.disputes.update";
        await auth.Require("M19", permission, ct);
        var dispute = await db.Set<M19Dispute>().SingleOrDefaultAsync(x => x.DisputeId == disputeId, ct);
        if (dispute is null || !await auth.CanAccess("M19", actor.UserId, permission, dispute.BranchId, null, dispute.OwnerUserId, ct)) return Results.NotFound();
        if (dispute.Version != request.ExpectedVersion) return Results.Conflict(new { detail = "The dispute changed. Refresh before updating its status." });
        var reason = request.Reason?.Trim() ?? ""; var resolution = request.Resolution?.Trim() ?? "";
        if (reason.Length is < 1 or > 500 || !CanTransition(dispute.Status, status))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Choose a permitted next status and give a reason (maximum 500 characters)."] });
        if (request.CreditNoteId.HasValue || string.Equals(resolution, "CreditNote", StringComparison.OrdinalIgnoreCase))
            return Results.Conflict(new { detail = "M11 credit-note persistence and authorized credit-note linking are not yet available; no credit-note resolution was recorded." });
        if (status == "Resolved" && resolution.Length is < 1 or > 500)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["resolution"] = ["Describe the verified resolution (maximum 500 characters)."] });
        var previous = dispute.Status; dispute.Status = status!; dispute.Resolution = resolution;
        dispute.UpdatedAt = DateTimeOffset.UtcNow; dispute.Version++;
        if (status is "Resolved" or "Rejected") { dispute.ResolvedAt = dispute.UpdatedAt; dispute.ResolvedByUserId = actor.UserId; }
        AddEvent(db, tenant.TenantId, actor.UserId, "Dispute", disputeId, "DisputeStatusChanged", new { previous, status, reason, resolution });
        if (status is "Resolved" or "Rejected") await ReleaseInvoiceHoldsWhenClear(db, tenant.TenantId, actor.UserId, disputeId, ct);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(new { detail = "The dispute changed concurrently. Refresh and retry." }); }
        return Results.Ok(new { dispute.DisputeId, dispute.Status, dispute.Resolution, dispute.Version, dispute.ResolvedAt });
    }
    private static async Task<IResult> DisputeHistory(Guid disputeId, TenantAuthorizationService auth, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct); await auth.Require("M19", "m19.disputes.view", ct);
        var row = await db.Set<M19Dispute>().AsNoTracking().SingleOrDefaultAsync(x => x.DisputeId == disputeId, ct);
        if (row is null || !await auth.CanAccess("M19", actor.UserId, "m19.disputes.view", row.BranchId, null, row.OwnerUserId, ct)) return Results.NotFound();
        return Results.Ok(await db.Set<M19QualityEvent>().AsNoTracking().Where(x => x.EntityType == "Dispute" && x.EntityId == disputeId)
            .OrderBy(x => x.OccurredAt).Select(x => new { x.EventId, x.ActorUserId, x.EventType, x.DetailJson, x.OccurredAt }).ToListAsync(ct));
    }

    private static async Task<IResult> SampleHistory(Guid sampleId, TenantAuthorizationService auth, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct); await auth.Require("M19", "m19.samples.view", ct);
        var row = await db.Set<M19QualitySample>().AsNoTracking().SingleOrDefaultAsync(x => x.SampleId == sampleId, ct);
        if (row is null || !await auth.CanAccess("M19", actor.UserId, "m19.samples.view", row.BranchId, null, row.OwnerUserId, ct)) return Results.NotFound();
        var sampleEvents = db.Set<M19QualityEvent>().AsNoTracking().Where(x => x.EntityType == "Sample" && x.EntityId == sampleId);
        var testEvents = db.Set<M19QualityTest>().AsNoTracking().Where(x => x.SampleId == sampleId).Select(x => x.TestId).ToArray();
        var events = await sampleEvents.ToListAsync(ct);
        if (testEvents.Length > 0) events.AddRange(await db.Set<M19QualityEvent>().AsNoTracking().Where(x => x.EntityType == "Test" && testEvents.Contains(x.EntityId)).ToListAsync(ct));
        return Results.Ok(events.OrderBy(x => x.OccurredAt).Select(x => new { x.EventId, x.EntityType, x.EntityId, x.ActorUserId, x.EventType, x.DetailJson, x.OccurredAt }));
    }

    private static async Task<IResult> AddDisputeEvidence(Guid disputeId, Guid documentId, TenantAuthorizationService auth,
        ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct); await auth.Require("M19", "m19.disputes.update", ct);
        var dispute = await db.Set<M19Dispute>().SingleOrDefaultAsync(x => x.DisputeId == disputeId, ct);
        if (dispute is null || !await auth.CanAccess("M19", actor.UserId, "m19.disputes.update", dispute.BranchId, null, dispute.OwnerUserId, ct)) return Results.NotFound();
        if (dispute.TripId is not { } tripId) return Results.Conflict(new { detail = "Evidence links require a delivery-trip source." });
        var cleanDocument = await db.Set<M18Document>().AnyAsync(x => x.DocumentId == documentId && x.TripId == tripId && x.IsCurrent &&
            x.ScanStatus == M18DocumentRules.ScanClean && x.StorageStatus == M18DocumentRules.StorageAvailable, ct);
        if (!cleanDocument) return Results.NotFound(new { detail = "A current, malware-scanned M18 document for this trip was not found." });
        if (await db.Set<M19DisputeEvidence>().AnyAsync(x => x.DisputeId == disputeId && x.DocumentId == documentId, ct))
            return Results.Ok(new { disputeId, documentId, alreadyLinked = true });
        db.Add(new M19DisputeEvidence { TenantId = tenant.TenantId, EvidenceId = Guid.NewGuid(), DisputeId = disputeId,
            DocumentId = documentId, AddedByUserId = actor.UserId, AddedAt = DateTimeOffset.UtcNow });
        AddEvent(db, tenant.TenantId, actor.UserId, "Dispute", disputeId, "EvidenceLinked", new { documentId });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { disputeId, documentId, linked = true });
    }

    private static async Task<IResult> RefreshReminders(TenantAuthorizationService auth, ICurrentTenantContext tenant, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct); await auth.Require("M19", "m19.samples.view", ct);
        var scopes = await auth.Scopes(actor.UserId, "m19.samples.view", ct);
        var samples = ScopeSamples(db, db.Set<M19QualitySample>().AsNoTracking(), scopes, actor.UserId);
        var due = await (from test in db.Set<M19QualityTest>().AsNoTracking()
            join sample in samples on new { test.TenantId, test.SampleId } equals new { sample.TenantId, sample.SampleId }
            where test.Status == M19Rules.Scheduled && test.DueAt <= DateTimeOffset.UtcNow
            select new { test.TestId, test.TestAgeDays, test.DueAt, sample.SampleId, sample.SampleNumber, sample.OwnerUserId }).Take(500).ToListAsync(ct);
        var created = 0;
        foreach (var item in due)
        {
            if (await db.Set<M02Notification>().AnyAsync(x => x.RecipientUserId == item.OwnerUserId && x.Kind == "M19TestDue" && x.EntityId == item.TestId, ct)) continue;
            db.Add(new M02Notification { TenantId = tenant.TenantId, NotificationId = Guid.NewGuid(), RecipientUserId = item.OwnerUserId,
                Kind = "M19TestDue", EntityId = item.TestId, Payload = JsonSerializer.Serialize(new { item.SampleId, item.SampleNumber, item.TestAgeDays, item.DueAt }, Json), CreatedAt = DateTimeOffset.UtcNow });
            created++;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { dueTests = due.Count, notificationsCreated = created, providerDelivery = "In-app M02 notifications only; external channels are not configured here." });
    }
    private static async Task<IResult> QualityReport(TenantAuthorizationService auth, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await auth.CurrentUser(ct); await auth.Require("M19", "m19.reports.view", ct);
        var scopes = await auth.Scopes(actor.UserId, "m19.reports.view", ct);
        var samples = await ScopeSamples(db, db.Set<M19QualitySample>().AsNoTracking(), scopes, actor.UserId).ToListAsync(ct);
        var ids = samples.Select(x => x.SampleId).ToArray();
        var tests = await db.Set<M19QualityTest>().AsNoTracking().Where(x => ids.Contains(x.SampleId)).ToListAsync(ct);
        var disputes = await ScopeDisputes(db, db.Set<M19Dispute>().AsNoTracking(), scopes, actor.UserId).ToListAsync(ct);
        var measured = tests.Where(x => x.Outcome is M19Rules.Pass or M19Rules.Fail).ToArray();
        var plantSummary = await (from sample in db.Set<M19QualitySample>().AsNoTracking()
            join trip in db.Set<M08Trip>().AsNoTracking() on new { sample.TenantId, sample.TripId } equals new { trip.TenantId, trip.TripId }
            join plant in db.Set<M03Plant>().AsNoTracking() on new { trip.TenantId, trip.PlantId } equals new { plant.TenantId, PlantId = (Guid?)plant.PlantId } into plants
            from plant in plants.DefaultIfEmpty()
            where ids.Contains(sample.SampleId)
            group sample by new { trip.PlantId, PlantName = plant == null ? "Unassigned" : plant.Name } into groupRows
            select new { groupRows.Key.PlantId, groupRows.Key.PlantName, sampleCount = groupRows.Count() }).ToListAsync(ct);
        var customerSummary = await (from sample in db.Set<M19QualitySample>().AsNoTracking()
            join order in db.Set<M06SalesOrder>().AsNoTracking() on new { sample.TenantId, sample.SalesOrderId } equals new { order.TenantId, order.SalesOrderId }
            join party in db.Set<M03Party>().AsNoTracking() on new { order.TenantId, PartyId = order.CustomerPartyId } equals new { party.TenantId, party.PartyId }
            where ids.Contains(sample.SampleId)
            group sample by new { order.CustomerPartyId, party.LegalName } into groupRows
            select new { groupRows.Key.CustomerPartyId, customerName = groupRows.Key.LegalName, sampleCount = groupRows.Count() }).ToListAsync(ct);
        return Results.Ok(new
        {
            sampleCount = samples.Count,
            dueOrOverdueTests = tests.Count(x => x.Status == M19Rules.Scheduled && x.DueAt <= DateTimeOffset.UtcNow),
            reviewRequired = tests.Count(x => x.Status == M19Rules.ReviewRequired || x.Outcome == M19Rules.ReviewRequired),
            passCount = measured.Count(x => x.Outcome == M19Rules.Pass),
            failCount = measured.Count(x => x.Outcome == M19Rules.Fail),
            passPercent = measured.Length == 0 ? (decimal?)null : decimal.Round(measured.Count(x => x.Outcome == M19Rules.Pass) * 100m / measured.Length, 2),
            openDisputes = disputes.Count(x => M19Rules.IsActiveDispute(x.Status)),
            closedDisputes = disputes.Count(x => x.Status is "Resolved" or "Rejected"),
            byPlant = plantSummary, byCustomer = customerSummary, generatedAt = DateTimeOffset.UtcNow
        });
    }

    private static async Task OpenQualityDisputeAndHoldInvoices(M19QualitySample sample, Guid actor, Guid tenantId, BerynxDbContext db, CancellationToken ct)
    {
        if (await db.Set<M19Dispute>().AnyAsync(x => x.SampleId == sample.SampleId && x.Status != "Rejected", ct)) return;
        var order = await db.Set<M06SalesOrder>().SingleAsync(x => x.SalesOrderId == sample.SalesOrderId, ct);
        var dispute = new M19Dispute { TenantId = tenantId, DisputeId = Guid.NewGuid(), IdempotencyKey = Guid.NewGuid(), BranchId = sample.BranchId,
            CustomerPartyId = order.CustomerPartyId, SiteId = sample.SiteId, SalesOrderId = sample.SalesOrderId, TripId = sample.TripId,
            SampleId = sample.SampleId, Type = "Quality", Description = $"Failed {sample.MaterialType} test for sample {sample.SampleNumber}; manager review required.",
            OwnerUserId = sample.OwnerUserId, Status = "Open", OpenedAt = DateTimeOffset.UtcNow, OpenedByUserId = actor, UpdatedAt = DateTimeOffset.UtcNow };
        db.Add(dispute);
        var managers = await (from user in db.Set<M02User>().AsNoTracking()
            join assignment in db.Set<M02UserRole>().AsNoTracking() on new { user.TenantId, user.UserId } equals new { assignment.TenantId, assignment.UserId }
            join grant in db.Set<M02RolePermission>().AsNoTracking() on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
            where user.Status == "Active" && grant.PermissionCode == "m19.disputes.resolve"
            select user.UserId).Distinct().ToListAsync(ct);
        foreach (var managerId in managers)
        {
            if (await db.Set<M02Notification>().AnyAsync(x => x.RecipientUserId == managerId && x.Kind == "M19QualityDispute" && x.EntityId == dispute.DisputeId, ct)) continue;
            db.Add(new M02Notification { TenantId = tenantId, NotificationId = Guid.NewGuid(), RecipientUserId = managerId,
                Kind = "M19QualityDispute", EntityId = dispute.DisputeId,
                Payload = JsonSerializer.Serialize(new { disputeId = dispute.DisputeId, sampleId = sample.SampleId, sample.SampleNumber }, Json),
                CreatedAt = DateTimeOffset.UtcNow });
        }
        await AddInvoiceHolds(db, tenantId, actor, dispute, ct);
        AddEvent(db, tenantId, actor, "Dispute", dispute.DisputeId, "FailedQualityTestDisputeOpened", new { sample.SampleId, sample.TripId });
    }

    private static async Task AddInvoiceHolds(BerynxDbContext db, Guid tenantId, Guid actor, M19Dispute dispute, CancellationToken ct)
    {
        var ids = new List<Guid>();
        if (dispute.TripId is { } tripId)
        {
            var tripInvoices = await (from link in db.Set<M10InvoiceChallan>().AsNoTracking()
                join challan in db.Set<M09DeliveryChallan>().AsNoTracking() on new { link.TenantId, link.ChallanId } equals new { challan.TenantId, challan.ChallanId }
                join invoice in db.Set<M10Invoice>().AsNoTracking() on new { link.TenantId, link.InvoiceId } equals new { invoice.TenantId, invoice.InvoiceId }
                where challan.TripId == tripId && invoice.Status == "Posted" select invoice.InvoiceId).Distinct().ToListAsync(ct);
            ids.AddRange(tripInvoices);
        }
        if (dispute.SalesOrderId is { } orderId)
        {
            var orderInvoices = await (from line in db.Set<M10InvoiceLine>().AsNoTracking()
                join invoice in db.Set<M10Invoice>().AsNoTracking() on new { line.TenantId, line.InvoiceId } equals new { invoice.TenantId, invoice.InvoiceId }
                where line.SalesOrderId == orderId && invoice.Status == "Posted" select invoice.InvoiceId).Distinct().ToListAsync(ct);
            ids.AddRange(orderInvoices);
        }
        ids.AddRange(db.ChangeTracker.Entries<M19DisputeInvoice>().Where(x => x.State == EntityState.Added && x.Entity.DisputeId == dispute.DisputeId).Select(x => x.Entity.InvoiceId));
        foreach (var invoice in await db.Set<M10Invoice>().Where(x => ids.Distinct().Contains(x.InvoiceId) && x.Status == "Posted").ToListAsync(ct))
        {
            var alreadyLinked = db.ChangeTracker.Entries<M19DisputeInvoice>().Any(x => x.Entity.DisputeId == dispute.DisputeId && x.Entity.InvoiceId == invoice.InvoiceId) ||
                await db.Set<M19DisputeInvoice>().AnyAsync(x => x.DisputeId == dispute.DisputeId && x.InvoiceId == invoice.InvoiceId, ct);
            if (!alreadyLinked) db.Add(new M19DisputeInvoice { TenantId = tenantId, DisputeId = dispute.DisputeId, InvoiceId = invoice.InvoiceId });
            if (invoice.DisputeHold) continue;
            invoice.DisputeHold = true;
            db.Add(new M10InvoiceEvent { TenantId = tenantId, EventId = Guid.NewGuid(), InvoiceId = invoice.InvoiceId, ActorUserId = actor,
                EventType = "DisputeHoldRaised", DetailJson = JsonSerializer.Serialize(new { disputeId = dispute.DisputeId }, Json), OccurredAt = DateTimeOffset.UtcNow });
        }
    }

    private static async Task ReleaseInvoiceHoldsWhenClear(BerynxDbContext db, Guid tenantId, Guid actor, Guid disputeId, CancellationToken ct)
    {
        var ids = await db.Set<M19DisputeInvoice>().Where(x => x.DisputeId == disputeId).Select(x => x.InvoiceId).ToListAsync(ct);
        foreach (var invoiceId in ids)
        {
            var another = await (from link in db.Set<M19DisputeInvoice>().AsNoTracking()
                join dispute in db.Set<M19Dispute>().AsNoTracking() on new { link.TenantId, link.DisputeId } equals new { dispute.TenantId, dispute.DisputeId }
                where link.InvoiceId == invoiceId && link.DisputeId != disputeId && (dispute.Status == "Open" || dispute.Status == "Investigating" || dispute.Status == "Escalated")
                select link.DisputeId).AnyAsync(ct);
            if (another) continue;
            var invoice = await db.Set<M10Invoice>().SingleOrDefaultAsync(x => x.InvoiceId == invoiceId && x.Status == "Posted", ct);
            if (invoice is null || !invoice.DisputeHold) continue;
            invoice.DisputeHold = false;
            db.Add(new M10InvoiceEvent { TenantId = tenantId, EventId = Guid.NewGuid(), InvoiceId = invoiceId, ActorUserId = actor,
                EventType = "DisputeHoldReleased", DetailJson = JsonSerializer.Serialize(new { disputeId }, Json), OccurredAt = DateTimeOffset.UtcNow });
        }
    }
    private static bool CanTransition(string current, string? next) => current switch
    {
        "Open" => next is "Investigating" or "Escalated" or "Rejected",
        "Investigating" => next is "Open" or "Escalated" or "Resolved" or "Rejected",
        "Escalated" => next is "Investigating" or "Resolved" or "Rejected",
        _ => false
    };

    private static IQueryable<M08Trip> ScopeTrips(IQueryable<M08Trip> query,
        IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes, Guid userId)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId.HasValue).Select(x => x.ScopeId!.Value).ToArray();
        var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId.HasValue).Select(x => x.ScopeId!.Value).ToArray();
        var own = scopes.Any(x => x.ScopeType == "Own");
        return query.Where(x => branches.Contains(x.BranchId) || own && x.CreatedByUserId == userId ||
            x.PlantId.HasValue && plants.Contains(x.PlantId.Value));
    }

    private static IQueryable<M19QualitySample> ScopeSamples(BerynxDbContext db, IQueryable<M19QualitySample> query,
        IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes, Guid userId)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId.HasValue).Select(x => x.ScopeId!.Value).ToArray();
        var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId.HasValue).Select(x => x.ScopeId!.Value).ToArray();
        var own = scopes.Any(x => x.ScopeType == "Own");
        return query.Where(x => branches.Contains(x.BranchId) || own && x.OwnerUserId == userId ||
            plants.Length > 0 && db.Set<M08Trip>().Any(t => t.TripId == x.TripId && t.PlantId.HasValue && plants.Contains(t.PlantId.Value)));
    }

    private static IQueryable<M19Dispute> ScopeDisputes(BerynxDbContext db, IQueryable<M19Dispute> query,
        IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes, Guid userId)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId.HasValue).Select(x => x.ScopeId!.Value).ToArray();
        var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId.HasValue).Select(x => x.ScopeId!.Value).ToArray();
        var own = scopes.Any(x => x.ScopeType == "Own");
        return query.Where(x => branches.Contains(x.BranchId) || own && x.OwnerUserId == userId ||
            plants.Length > 0 && x.TripId.HasValue && db.Set<M08Trip>().Any(t => t.TripId == x.TripId && t.PlantId.HasValue && plants.Contains(t.PlantId.Value)));
    }

    private static void AddEvent(BerynxDbContext db, Guid tenantId, Guid actor, string entityType,
        Guid entityId, string eventType, object detail) => db.Add(new M19QualityEvent
        {
            TenantId = tenantId, EventId = Guid.NewGuid(), EntityType = entityType, EntityId = entityId,
            ActorUserId = actor, EventType = eventType, DetailJson = JsonSerializer.Serialize(detail, Json), OccurredAt = DateTimeOffset.UtcNow
        });
}