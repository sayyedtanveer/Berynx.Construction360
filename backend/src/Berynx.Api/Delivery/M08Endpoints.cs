using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Api.DeliveryChallans;
using Berynx.Api.SalesOrders;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Delivery;
using Berynx.Application.Documents;
using Berynx.Domain.Documents;
using Berynx.Application.Tenancy;
using Berynx.Domain.Delivery;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Procurement;
using Berynx.Domain.SalesOrders;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Delivery;

public static class M08Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public sealed record PourWrite(
        Guid ScheduleId, Guid? PlantId, DateTimeOffset WindowStart, DateTimeOffset WindowEnd,
        string? Slump, string PlacementMethod, string? PumpType, decimal PumpQuantity,
        string? CustomerEngineer, string? Coordinator, string? SpecialInstructions);
    public sealed record TripPlanLine(
        Guid? VehicleId, Guid? DriverId, decimal PlannedQuantity,
        string? LotOrHeatNumber, string? MtcReference);
    public sealed record TripPlanWrite(long Version, TripPlanLine[] Trips);
    public sealed record TripActionWrite(
        string ActionId, long Version, string Action, DateTimeOffset DeviceAt,
        decimal? LoadedQuantity, decimal? DeliveredQuantity, decimal? ReturnedQuantity,
        string? BatchSlipNumber, string? ReceiverName, string? ReceiverMobile,
        string? SignatureReference, string? PhotoReference,
        string? ExceptionType, string? ExceptionReason);

    public sealed record EvidenceResubmissionWrite(
        long Version, string ReceiverName, string? ReceiverMobile,
        string? SignatureReference, string? PhotoReference, string Reason);
    public sealed record EvidenceReviewWrite(long Version, string Decision, string Reason);
    public static void MapM08(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m08")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices
                .GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M08",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });

        api.MapGet("/dispatch-board", DispatchBoard);
        api.MapGet("/evidence/pending", PendingEvidence);
        api.MapGet("/trips/{tripId:guid}/evidence-review", GetEvidenceReview);
        api.MapPost("/trips/{tripId:guid}/evidence-review", ReviewEvidence);
        api.MapPost("/trips/{tripId:guid}/evidence", ResubmitEvidence);
        api.MapGet("/evidence/upload-policy", GetEvidenceUploadPolicy);
        api.MapPost("/trips/{tripId:guid}/evidence-assets", UploadTripEvidence);
        api.MapGet("/trips/{tripId:guid}/evidence-assets/pending", PendingEvidenceScans);
        api.MapPost("/trips/{tripId:guid}/evidence-assets/{documentId:guid}/scan-retry", RetryEvidenceScan);
        api.MapGet("/trips/{tripId:guid}/evidence/{documentId:guid}/content", PreviewEvidence);
        api.MapGet("/trips/{tripId:guid}/evidence/{documentId:guid}/download", DownloadEvidence);
        api.MapPost("/pours", CreatePour);
        api.MapGet("/pours/{pourId:guid}", GetPour);
        api.MapPost("/pours/{pourId:guid}/trips", PlanTrips);
        api.MapPost("/trips/{tripId:guid}/actions", RecordTripAction);
    }

    private static async Task<IResult> DispatchBoard(
        DateOnly? date, Guid? plantId, Guid? branchId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.dispatch.view", ct);
        var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = start.AddDays(1);
        var scopes = await authorization.Scopes(actor.UserId, "m08.dispatch.view", ct);
        var query = db.Set<M08Pour>().AsNoTracking()
            .Where(x => x.WindowStart >= start && x.WindowStart < end);
        if (plantId is { } plant) query = query.Where(x => x.PlantId == plant);
        if (branchId is { } branch) query = query.Where(x => x.BranchId == branch);
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x => own && x.CreatedByUserId == actor.UserId ||
                                     branches.Contains(x.BranchId) ||
                                     x.PlantId != null && plants.Contains(x.PlantId.Value));
        }
        var pours = await query.OrderBy(x => x.WindowStart)
            .Select(x => new
            {
                x.PourId, x.ScheduleId, x.SalesOrderId, x.SalesOrderLineId,
                x.BranchId, x.PlantId, x.ProductNameSnapshot, x.CustomerNameSnapshot,
                x.SiteNameSnapshot, x.WindowStart, x.WindowEnd, x.ScheduledQuantity,
                x.DeliveredQuantity, x.ReturnedQuantity, x.Status, x.Version
            }).ToListAsync(ct);
        var pourIds = pours.Select(x => x.PourId).ToArray();
        var trips = await db.Set<M08Trip>().AsNoTracking()
            .Where(x => pourIds.Contains(x.PourId))
            .Select(x => new
            {
                x.TripId, x.PourId, x.Number, x.Status, x.VehicleId, x.DriverId,
                x.PlannedQuantity, x.LoadedQuantity, x.DeliveredQuantity, x.ReturnedQuantity,
                x.LoadedAt, x.DispatchedAt, x.ArrivedAt, x.CompletedAt, x.EvidenceState, x.Version
            }).ToListAsync(ct);
        var unscheduledQuery =
            from schedule in db.Set<M06DeliverySchedule>().AsNoTracking()
            join order in db.Set<M06SalesOrder>().AsNoTracking()
                on new { schedule.TenantId, schedule.SalesOrderId } equals new { order.TenantId, order.SalesOrderId }
            where schedule.DeliveryDate == day && schedule.Status == "Planned" &&
                  !db.Set<M08Pour>().Any(p => p.ScheduleId == schedule.ScheduleId)
            select new
            {
                schedule.ScheduleId, schedule.SalesOrderId, schedule.SalesOrderLineId,
                schedule.ProductId, schedule.SiteId, schedule.PreferredPlantId,
                schedule.DeliveryDate, schedule.TimeWindow, schedule.Quantity,
                order.BranchId, order.SalesOwnerUserId, order.Number,
                order.CustomerNameSnapshot
            };
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            unscheduledQuery = unscheduledQuery.Where(x =>
                own && x.SalesOwnerUserId == actor.UserId ||
                x.BranchId != null && branches.Contains(x.BranchId.Value) ||
                x.PreferredPlantId != null && plants.Contains(x.PreferredPlantId.Value));
        }
        if (plantId is { } filterPlant)
            unscheduledQuery = unscheduledQuery.Where(x => x.PreferredPlantId == filterPlant);
        if (branchId is { } filterBranch)
            unscheduledQuery = unscheduledQuery.Where(x => x.BranchId == filterBranch);
        var unscheduled = await unscheduledQuery.OrderBy(x => x.DeliveryDate)
            .Select(x => new
            {
                x.ScheduleId, x.SalesOrderId, x.SalesOrderLineId, x.ProductId,
                x.SiteId, x.PreferredPlantId, x.DeliveryDate, x.TimeWindow, x.Quantity,
                x.BranchId, x.Number, x.CustomerNameSnapshot
            }).ToListAsync(ct);
        return Results.Ok(new
        {
            date = day,
            unscheduled,
            pours = pours.Select(p => new { pour = p, trips = trips.Where(t => t.PourId == p.PourId) }),
            totals = new
            {
                scheduled = pours.Sum(x => x.ScheduledQuantity),
                delivered = pours.Sum(x => x.DeliveredQuantity),
                pending = pours.Sum(x => Math.Max(0m, x.ScheduledQuantity - x.DeliveredQuantity))
            },
            refreshAfterSeconds = 30,
            creditRecheckState = "M03 transaction-backed outstanding/overdue facts are not available in this build."
        });
    }

    private static async Task<IResult> CreatePour(
        PourWrite request, TenantAuthorizationService authorization, BerynxDbContext db,
        ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.pours.schedule", ct);
        ValidatePour(request);
        var schedule = await db.Set<M06DeliverySchedule>().SingleOrDefaultAsync(
            x => x.ScheduleId == request.ScheduleId, ct);
        if (schedule is null || schedule.Status != "Planned")
            return Results.Conflict(new { detail = "Only an approved, planned M06 delivery schedule can become a pour." });
        var setup = OnboardingStore.Decode(await db.Set<Tenant>().AsNoTracking().SingleAsync(ct));
        var tenantTimeZone = TimeZoneInfo.FindSystemTimeZoneById(setup.TimeZone);
        var localWindowStart = TimeZoneInfo.ConvertTime(request.WindowStart, tenantTimeZone);
        if (DateOnly.FromDateTime(localWindowStart.DateTime) != schedule.DeliveryDate)
            throw new ValidationException("Pour window must fall on the delivery date approved in M06.");
        var existing = await db.Set<M08Pour>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ScheduleId == schedule.ScheduleId, ct);
        if (existing is not null)
        {
            if (!await authorization.CanAccess("M08", actor.UserId, "m08.dispatch.view",
                    existing.BranchId, existing.PlantId, existing.CreatedByUserId, ct, write: false))
                return Results.NotFound();
            return Results.Ok(new { existing.PourId, existing.ScheduleId, existing.Status, duplicate = true });
        }
        var order = await db.Set<M06SalesOrder>()
            .SingleOrDefaultAsync(x => x.SalesOrderId == schedule.SalesOrderId, ct)
            ?? throw new ValidationException("The source M06 sales order was not found.");
        if (order.Status is not ("Confirmed" or "In fulfilment"))
            throw new ValidationException("The source sales order is not available for dispatch.");
        if (!await authorization.CanAccess("M06", actor.UserId, "m06.orders.view",
                order.BranchId, null, order.SalesOwnerUserId, ct, write: false))
            return Results.NotFound();
        if (!await authorization.CanAccess("M08", actor.UserId, "m08.pours.schedule",
                order.BranchId, request.PlantId ?? schedule.PreferredPlantId, actor.UserId, ct))
            return Results.NotFound();
        var line = await db.Set<M06SalesOrderLine>().AsNoTracking().SingleOrDefaultAsync(
            x => x.LineId == schedule.SalesOrderLineId &&
                 x.SalesOrderId == schedule.SalesOrderId, ct)
            ?? throw new ValidationException("The source delivery schedule line was not found.");
        if (line.ProductId != schedule.ProductId || order.BranchId is null)
            throw new ValidationException("The schedule product or branch no longer matches the sales order.");
        var customer = await db.Set<M03Party>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.PartyId == order.CustomerPartyId, ct)
            ?? throw new ValidationException("The source M03 customer was not found.");
        if (customer.CreditStatus == "Blocked")
            return Results.Conflict(new
            {
                detail = "Customer is blocked in M03; an authorized credit override and M02 approval are required.",
                creditState = "Blocked"
            });
        var product = await db.Set<M03Product>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == schedule.ProductId, ct)
            ?? throw new ValidationException("The scheduled M03 product was not found.");
        var site = schedule.SiteId is { } siteId
            ? await db.Set<M03Site>().AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == siteId, ct)
            : null;
        var plantId = request.PlantId ?? schedule.PreferredPlantId;
        M03Plant? plant = null;
        if (plantId is { } selectedPlant)
        {
            plant = await db.Set<M03Plant>().AsNoTracking().SingleOrDefaultAsync(
                x => x.PlantId == selectedPlant && x.IsActive, ct)
                ?? throw new ValidationException("Choose an active M03 plant.");
            if (plant.BranchId is { } plantBranch && order.BranchId != plantBranch)
                throw new ValidationException("The selected plant is outside the sales order branch.");
        }
        if (product.Category.Contains("RMC", StringComparison.OrdinalIgnoreCase) && plant is null)
            throw new ValidationException("RMC pours require an active plant assignment.");
        var overlappingCapacity = plant?.CapacityPerHour is { } capacity
            ? await db.Set<M08Pour>().AsNoTracking()
                .Where(x => x.PlantId == plant!.PlantId &&
                            x.WindowStart < request.WindowEnd && x.WindowEnd > request.WindowStart &&
                            (x.Status == "Scheduled" || x.Status == "In progress"))
                .SumAsync(x => (decimal?)x.ScheduledQuantity, ct) ?? 0m
            : 0m;
        var capacityWarning = plant?.CapacityPerHour is { } limit && overlappingCapacity + schedule.Quantity > limit;
        var pour = new M08Pour
        {
            TenantId = tenant.TenantId, PourId = Guid.NewGuid(), ScheduleId = schedule.ScheduleId,
            SalesOrderId = order.SalesOrderId, SalesOrderLineId = line.LineId, ProductId = product.ProductId,
            SiteId = schedule.SiteId, BranchId = order.BranchId.Value, PlantId = plant?.PlantId,
            ProductNameSnapshot = product.Name, CustomerNameSnapshot = order.CustomerNameSnapshot,
            SiteNameSnapshot = site?.Name ?? "", ProductCategorySnapshot = product.Category,
            WindowStart = request.WindowStart, WindowEnd = request.WindowEnd,
            ScheduledQuantity = schedule.Quantity, Slump = request.Slump?.Trim() ?? "",
            PlacementMethod = request.PlacementMethod, PumpType = request.PumpType?.Trim() ?? "",
            PumpQuantity = request.PumpQuantity, CustomerEngineer = request.CustomerEngineer?.Trim() ?? "",
            Coordinator = request.Coordinator?.Trim() ?? "",
            SpecialInstructions = request.SpecialInstructions?.Trim() ?? "",
            CreatedByUserId = actor.UserId, CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Add(pour);
        db.Add(new M02AuditEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(),
            ActorUserId = actor.UserId, ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString(),
            Action = "M08 pour scheduled", EntityType = "M08Pour", EntityId = pour.PourId,
            Before = "{}", After = JsonSerializer.Serialize(new
            {
                pour.ScheduleId, pour.SalesOrderId, pour.ScheduledQuantity, pour.PlantId,
                capacityWarning, overlappingCapacity
            }, Json), At = DateTimeOffset.UtcNow
        });
        schedule.Status = "Released";
        if (order.Status == "Confirmed")
        {
            M06Rules.RequireTransition(order.Status, "In fulfilment");
            order.Status = "In fulfilment";
        }
        order.Version++;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The schedule or sales order changed concurrently. Reload and retry." });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "A pour has already been created from this M06 schedule." });
        }
        return Results.Created($"/api/v1/m08/pours/{pour.PourId}", new
        {
            pour.PourId, pour.ScheduleId, pour.Status, pour.Version, capacityWarning,
            capacityPerHour = plant?.CapacityPerHour,
            creditState = customer.CreditStatus,
            creditFactsState = "Outstanding and overdue transactions are unavailable; M03 master status only.",
            notificationState = "External notification provider unavailable."
        });
    }

    private static async Task<IResult> GetPour(
        Guid pourId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.dispatch.view", ct);
        var pour = await db.Set<M08Pour>().AsNoTracking().SingleOrDefaultAsync(x => x.PourId == pourId, ct);
        if (pour is null || !await authorization.CanAccess("M08", actor.UserId,
                "m08.dispatch.view", pour.BranchId, pour.PlantId, pour.CreatedByUserId, ct, write: false))
            return Results.NotFound();
        var trips = await db.Set<M08Trip>().AsNoTracking().Where(x => x.PourId == pourId)
            .OrderBy(x => x.Number).ToListAsync(ct);
        var events = await (from evt in db.Set<M08TripEvent>().AsNoTracking()
                            join trip in db.Set<M08Trip>().AsNoTracking()
                                on new { evt.TenantId, evt.TripId } equals new { trip.TenantId, trip.TripId }
                            where trip.PourId == pourId
                            orderby evt.ReceivedAt
                            select evt).ToListAsync(ct);
        var canViewCost = await authorization.CanAccess("M08", actor.UserId,
            "global.view_cost", pour.BranchId, pour.PlantId, pour.CreatedByUserId, ct, write: false);
        var publicTrips = trips.Select(t => new
        {
            t.TripId, t.Number, t.Status, t.VehicleId, t.DriverId,
            t.PlannedQuantity, t.LoadedQuantity, t.DeliveredQuantity, t.ReturnedQuantity,
            t.BatchSlipNumber, t.LoadedAt, t.DispatchedAt, t.ArrivedAt,
            t.UnloadingStartedAt, t.CompletedAt, t.ReceiverName, t.ReceiverMobile,
            t.SignatureReference, t.PhotoReference, t.EvidenceState, t.ReceiverVerifiedAt,
            t.ReceiverVerifiedByUserId, t.EvidenceDecisionReason,
            purchaseCost = canViewCost ? t.PurchaseCostSnapshot : null,
            t.WaitingMinutes, waitingCharge = canViewCost ? t.WaitingCharge : (decimal?)null,
            partLoadCharge = canViewCost ? t.PartLoadCharge : (decimal?)null,
            t.Version
        }).ToList();
        return Results.Ok(new { pour, trips = publicTrips, events });
    }

    private static async Task<IResult> PlanTrips(
        Guid pourId, TripPlanWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.trips.plan", ct);
        if (request.Trips is null || request.Trips.Length is 0 or > 100 ||
            request.Trips.Any(x => x.PlannedQuantity <= 0 || decimal.Round(x.PlannedQuantity, 3) != x.PlannedQuantity))
            throw new ValidationException("Plan 1–100 trips with positive quantities to three decimals.");
        var pour = await db.Set<M08Pour>().SingleOrDefaultAsync(x => x.PourId == pourId, ct);
        if (pour is null || !await authorization.CanAccess("M08", actor.UserId, "m08.trips.plan",
                pour.BranchId, pour.PlantId, pour.CreatedByUserId, ct))
            return Results.NotFound();
        if (pour.Version != request.Version)
            return Results.Conflict(new { detail = "The pour changed. Reload and retry.", pour.Version });
        if (pour.Status is not ("Scheduled" or "In progress"))
            return Results.Conflict(new { detail = "Trips can only be planned for a scheduled or in-progress pour." });
        var existingTrips = await db.Set<M08Trip>().Where(x => x.PourId == pourId).ToListAsync(ct);
        if (existingTrips.Any(x => x.Status is not ("Cancelled" or "Breakdown")))
            return Results.Conflict(new { detail = "Trips already exist; use a reviewed trip amendment instead of replacing them." });
        if (request.Trips.Sum(x => x.PlannedQuantity) > pour.ScheduledQuantity)
            throw new ValidationException("Trip planned quantities exceed the pour quantity.");
        var now = DateTimeOffset.UtcNow;
        var newTrips = new List<M08Trip>();
        foreach (var input in request.Trips)
        {
            var vehicle = input.VehicleId is { } vehicleId
                ? await db.Set<M03Vehicle>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.VehicleId == vehicleId && x.IsActive, ct)
                  ?? throw new ValidationException("Choose an active M03 vehicle.")
                : null;
            var driver = input.DriverId is { } driverId
                ? await db.Set<M03Driver>().AsNoTracking().SingleOrDefaultAsync(
                    x => x.DriverId == driverId && x.IsActive, ct)
                  ?? throw new ValidationException("Choose an active M03 driver.")
                : null;
            if (pour.ProductCategorySnapshot.Contains("RMC", StringComparison.OrdinalIgnoreCase) &&
                (vehicle is null || driver is null))
                throw new ValidationException("RMC trips require both a transit mixer and driver.");
            if (vehicle is not null && vehicle.Capacity < input.PlannedQuantity)
                throw new ValidationException($"Trip load {input.PlannedQuantity} exceeds vehicle capacity {vehicle.Capacity}.");
            if (driver is not null && driver.LicenceExpiresOn < DateOnly.FromDateTime(pour.WindowStart.UtcDateTime))
                throw new ValidationException($"Driver licence for {driver.Name} expires before the pour.");
            if (vehicle is not null && new[]
                {
                    vehicle.InsuranceExpiresOn, vehicle.PucExpiresOn, vehicle.FitnessExpiresOn
                }.Any(expiry => expiry is { } date && date < DateOnly.FromDateTime(pour.WindowStart.UtcDateTime)))
                throw new ValidationException($"Vehicle {vehicle.RegistrationNumber} has expired compliance documents.");
            if (vehicle is not null)
                await EnsureNoAssignmentConflict(db, pour, vehicle.VehicleId, null, ct);
            if (driver is not null)
                await EnsureNoAssignmentConflict(db, pour, null, driver.DriverId, ct);
            if (newTrips.Any(x =>
                    vehicle is not null && x.VehicleId == vehicle.VehicleId ||
                    driver is not null && x.DriverId == driver.DriverId))
                throw new ValidationException("A vehicle or driver cannot be assigned to two trips in the same pour window.");
            var trip = new M08Trip
            {
                TenantId = tenant.TenantId, TripId = Guid.NewGuid(), PourId = pourId,
                BranchId = pour.BranchId, PlantId = pour.PlantId,
                VehicleId = vehicle?.VehicleId, DriverId = driver?.DriverId,
                Number = $"{pourId:N}"[..12].ToUpperInvariant() + $"-{existingTrips.Count + newTrips.Count + 1:D2}",
                Status = "Planned", PlannedQuantity = input.PlannedQuantity,
                LotOrHeatNumber = input.LotOrHeatNumber?.Trim() ?? "",
                MtcReference = input.MtcReference?.Trim() ?? "",
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                CreatedByUserId = actor.UserId, CreatedAt = now
            };
            newTrips.Add(trip);
        }
        pour.Version++;
        pour.UpdatedAt = now;
        db.AddRange(newTrips);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m08/pours/{pourId}",
            new { pour.PourId, pour.Status, pour.Version, trips = newTrips.Select(x => new { x.TripId, x.Number, x.PlannedQuantity }) });
    }

    private static async Task<IResult> RecordTripAction(
        Guid tripId, TripActionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, IM08SecureEvidenceProvider evidenceProvider, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.trips.update", ct);
        if (request.Action == "Completed")
            await authorization.RequireModule("M09", write: true, cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(request.ActionId) || request.ActionId.Length > 100)
            throw new ValidationException("A unique driver action ID is required.");
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.trips.update",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct))
            return Results.NotFound();
        var previousAction = await db.Set<M08DriverAction>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ActionId == request.ActionId, ct);
        if (previousAction is not null)
        {
            if (previousAction.TripId != tripId || previousAction.DriverId != trip.DriverId)
                return Results.Conflict(new { detail = "Action ID was already used for another trip." });
            return Results.Ok(new
            {
                previousAction.ActionId, previousAction.State, previousAction.RejectionReason,
                duplicate = true, trip.Status, trip.Version
            });
        }
        if (trip.Version != request.Version)
            return Results.Conflict(new { detail = "Trip changed. Reload its timeline and retry.", trip.Version });
        if (trip.DriverId is null)
            return Results.Conflict(new { detail = "Assign a driver before recording driver workflow actions." });
        if (request.DeviceAt > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new ValidationException("Device action time cannot be more than five minutes in the future.");
        var fromStatus = trip.Status;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var next = request.Action switch
        {
            "Loaded" => "Loading",
            "Dispatched" => "Dispatched",
            "At site" => "At site",
            "Unloading" => "Unloading",
            "Completed" => "Completed",
            "Rejected" => "Rejected",
            "Returned" => "Returned",
            "Breakdown" => "Breakdown",
            "Cancelled" => "Cancelled",
            _ => throw new ValidationException("Unknown trip action.")
        };
        M08Rules.RequireTripTransition(trip.Status, next);
        M08Rules.ValidateTimestampSequence(trip, request.Action, request.DeviceAt);
        if (request.Action == "Loaded")
        {
            if (request.LoadedQuantity is not { } load || load <= 0 || load > trip.PlannedQuantity ||
                string.IsNullOrWhiteSpace(request.BatchSlipNumber))
                throw new ValidationException("Loading requires a positive quantity within plan and a batch-slip number.");
            trip.LoadedQuantity = load;
            trip.BatchSlipNumber = request.BatchSlipNumber.Trim();
            trip.LoadedAt = request.DeviceAt;
        }
        if (request.Action == "Dispatched") trip.DispatchedAt = request.DeviceAt;
        if (request.Action == "At site") trip.ArrivedAt = request.DeviceAt;
        if (request.Action == "Unloading") trip.UnloadingStartedAt = request.DeviceAt;
        if (request.Action is "Rejected" or "Returned" or "Breakdown" or "Cancelled")
        {
            if (string.IsNullOrWhiteSpace(request.ExceptionReason))
                throw new ValidationException("An exception reason is required.");
            trip.ExceptionType = request.ExceptionType?.Trim() ?? request.Action;
            trip.ExceptionReason = request.ExceptionReason.Trim();
        }
        if (request.Action == "Completed")
        {
            if (string.IsNullOrWhiteSpace(request.ReceiverName))
                throw new ValidationException("Receiver name is required to complete a delivery.");
            var delivered = request.DeliveredQuantity ?? 0m;
            var returned = request.ReturnedQuantity ?? 0m;
            if (delivered < 0 || returned < 0 || delivered + returned > trip.LoadedQuantity)
                throw new ValidationException("Delivered and returned quantities cannot exceed the loaded quantity.");
            if (delivered + returned == 0m)
                throw new ValidationException("Completed trips must record delivered or returned quantity.");
            if (returned > 0 && string.IsNullOrWhiteSpace(request.ExceptionReason))
                throw new ValidationException("Returned quantity requires an exception reason.");
            var pour = await db.Set<M08Pour>().SingleAsync(x => x.PourId == trip.PourId, ct);
            var orderLine = await db.Set<M06SalesOrderLine>()
                .SingleAsync(x => x.LineId == pour.SalesOrderLineId, ct);
            var tenantRow = await db.Set<Tenant>().AsNoTracking().SingleAsync(ct);
            var setup = OnboardingStore.Decode(tenantRow);
            var tolerance = pour.ProductCategorySnapshot.Contains("RMC", StringComparison.OrdinalIgnoreCase)
                ? setup.M06RmcOverdeliveryTolerancePercent
                : pour.ProductCategorySnapshot.Contains("steel", StringComparison.OrdinalIgnoreCase)
                    ? setup.M06SteelOverdeliveryTolerancePercent : 0m;
            M08Rules.ValidateDeliveredQuantity(orderLine.OrderedQuantity,
                orderLine.DeliveredQuantity, delivered, tolerance);
            trip.DeliveredQuantity = delivered;
            trip.ReturnedQuantity = returned;
            trip.CompletedAt = request.DeviceAt;
            trip.ReceiverName = request.ReceiverName?.Trim();
            trip.ReceiverMobile = request.ReceiverMobile?.Trim();
            if (!string.IsNullOrWhiteSpace(request.SignatureReference) &&
                !string.Equals(request.SignatureReference.Trim(), trip.SignatureReference, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(request.PhotoReference) &&
                !string.Equals(request.PhotoReference.Trim(), trip.PhotoReference, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Evidence references must come from a completed M18 upload for this trip.");
            var evidenceReady = false;
            if (M08Rules.HasSubmittedEvidenceReference(trip) &&
                M08SecureEvidenceReviewRules.TryParseAssetIds(trip.SignatureReference, trip.PhotoReference, out var evidenceIds))
            {
                var scanned = await evidenceProvider.GetForReviewAsync(trip.TenantId, trip.TripId, evidenceIds, ct);
                evidenceReady = M08SecureEvidenceReviewRules.IsReviewable(
                    trip.TenantId, trip.TripId, trip.SignatureReference, trip.PhotoReference, scanned);
                if (!evidenceReady)
                    throw new ValidationException("Uploaded delivery evidence must finish a clean malware scan before it can be submitted for supervisor review.");
            }
            trip.EvidenceState = evidenceReady ? "Submitted" : "Missing";
            orderLine.DeliveredQuantity += delivered;
            pour.DeliveredQuantity += delivered;
            pour.ReturnedQuantity += returned;
            if (pour.Status == "Scheduled")
            {
                M08Rules.RequirePourTransition(pour.Status, "In progress");
                pour.Status = "In progress";
            }
            if (pour.DeliveredQuantity >= pour.ScheduledQuantity)
            {
                M08Rules.RequirePourTransition(pour.Status, "Completed");
                pour.Status = "Completed";
            }
            pour.Version++;
        }
        else if (request.Action == "Loaded")
        {
            var pour = await db.Set<M08Pour>().SingleAsync(x => x.PourId == trip.PourId, ct);
            if (pour.Status == "Scheduled")
            {
                M08Rules.RequirePourTransition(pour.Status, "In progress");
                pour.Status = "In progress";
                pour.Version++;
            }
        }
        trip.Status = next;
        trip.Version++;
        var receivedAt = DateTimeOffset.UtcNow;
        var action = new M08DriverAction
        {
            TenantId = tenant.TenantId, DriverActionId = Guid.NewGuid(), TripId = tripId,
            DriverId = trip.DriverId.Value, ActionId = request.ActionId,
            ActionType = request.Action, State = "Sent", DeviceAt = request.DeviceAt, ReceivedAt = receivedAt
        };
        db.Add(action);
        db.Add(TripEvent(tenant.TenantId, tripId, actor.UserId, request.Action,
            fromStatus, next, new
            {
                request.LoadedQuantity, request.DeliveredQuantity, request.ReturnedQuantity,
                request.ReceiverName, request.SignatureReference, request.PhotoReference, trip.EvidenceState, request.ExceptionType, request.ExceptionReason
            }, request.DeviceAt, request.ActionId));
        if (request.Action == "Completed")
        {
            var pour = await db.Set<M08Pour>().SingleAsync(x => x.PourId == trip.PourId, ct);
            var order = await db.Set<M06SalesOrder>().SingleAsync(x => x.SalesOrderId == pour.SalesOrderId, ct);
            var line = await db.Set<M06SalesOrderLine>().SingleAsync(x => x.LineId == pour.SalesOrderLineId, ct);
            await M09Endpoints.CreateFromCompletedTrip(
                db, tenant.TenantId, actor.UserId, trip, pour, order, line, ct);
        }
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "Trip or order quantities changed concurrently; reload and retry." });
        }
        return Results.Ok(new
        {
            trip.TripId, trip.Number, trip.Status, trip.Version, actionId = action.ActionId,
            actionState = action.State, trip.EvidenceState,
            billingEligibility = trip.EvidenceState == "Verified" ? "Eligible for M09 challan evaluation" : "Unverified; not invoice-eligible"
        });
    }

    private static async Task<IResult> GetEvidenceUploadPolicy(
        TenantAuthorizationService authorization, IM18DocumentService documents, CancellationToken ct)
    {
        await authorization.Require("M08", "m08.trips.update", ct);
        return Results.Ok(documents.UploadPolicy);
    }

    private static async Task<IResult> PendingEvidenceScans(
        Guid tripId, TenantAuthorizationService authorization, BerynxDbContext db,
        IM18DocumentService documents, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.trips.update", ct);
        var trip = await db.Set<M08Trip>().AsNoTracking().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.trips.update",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct))
            return Results.NotFound();
        var items = await documents.GetPendingScansAsync(tripId, actor.UserId, ct);
        return Results.Ok(new { items });
    }
    private static async Task<IResult> UploadTripEvidence(
        Guid tripId, string purpose, string? fileName, HttpRequest request,
        TenantAuthorizationService authorization, BerynxDbContext db, IM18DocumentService documents,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.trips.update", ct);
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.trips.update",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct))
            return Results.NotFound();
        if (request.ContentLength is > 10 * 1024 * 1024)
            return Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Evidence file is too large",
                detail: "The maximum evidence file size is 10 MB.");
        try
        {
            var result = await documents.UploadTripEvidenceAsync(trip, actor.UserId, purpose, fileName,
                request.ContentType, request.Body, ct);
            if (result.ScanStatus == M18DocumentRules.ScanRejected)
                return Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Evidence rejected by malware scan", detail: result.Detail);
            return result.ScanStatus == M18DocumentRules.ScanPending
                ? Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable)
                : Results.Ok(result);
        }
        catch (M18UploadRejectedException ex)
        {
            return Results.Problem(statusCode: ex.StatusCode, title: "Evidence upload rejected", detail: ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["evidence"] = [ex.Message] });
        }
    }

    private static async Task<IResult> RetryEvidenceScan(
        Guid tripId, Guid documentId, TenantAuthorizationService authorization,
        IM18DocumentService documents, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.trips.update", ct);
        var trip = await db.Set<M08Trip>().AsNoTracking().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.trips.update",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct))
            return Results.NotFound();
        try
        {
            var result = await documents.RetryScanAsync(tripId, documentId, actor.UserId, ct);
            if (result.ScanStatus == M18DocumentRules.ScanRejected)
                return Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Evidence rejected by malware scan", detail: result.Detail);
            return result.ScanStatus == M18DocumentRules.ScanPending
                ? Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable)
                : Results.Ok(result);
        }
        catch (M18UploadRejectedException ex)
        {
            return Results.Problem(statusCode: ex.StatusCode, title: "Evidence scan retry rejected", detail: ex.Message);
        }
    }

    private static Task<IResult> PreviewEvidence(
        Guid tripId, Guid documentId, TenantAuthorizationService authorization,
        IM18DocumentService documents, BerynxDbContext db, HttpContext http, CancellationToken ct) =>
        ServeEvidence(tripId, documentId, download: false, authorization, documents, db, http, ct);

    private static Task<IResult> DownloadEvidence(
        Guid tripId, Guid documentId, TenantAuthorizationService authorization,
        IM18DocumentService documents, BerynxDbContext db, HttpContext http, CancellationToken ct) =>
        ServeEvidence(tripId, documentId, download: true, authorization, documents, db, http, ct);

    private static async Task<IResult> ServeEvidence(
        Guid tripId, Guid documentId, bool download, TenantAuthorizationService authorization,
        IM18DocumentService documents, BerynxDbContext db, HttpContext http, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.evidence.review", ct);
        var trip = await db.Set<M08Trip>().AsNoTracking().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.evidence.review",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct, write: false))
            return Results.NotFound();
        var file = await documents.OpenForReviewAsync(tripId, documentId, actor.UserId, download, ct);
        if (file is null) return Results.NotFound();
        http.Response.Headers["Cache-Control"] = "no-store";
        http.Response.Headers["X-Content-Type-Options"] = "nosniff";
        http.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        return Results.File(file.Content, file.ContentType, download ? file.FileName : null);
    }
    private static async Task<IResult> PendingEvidence(
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.evidence.review", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m08.evidence.review", ct);
        var query = db.Set<M08Trip>().AsNoTracking()
            .Where(x => x.Status == "Completed" && x.EvidenceState == "Submitted");
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var plants = scopes.Where(x => x.ScopeType == "Plant" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            var own = scopes.Any(x => x.ScopeType == "Own");
            query = query.Where(x => own && x.CreatedByUserId == actor.UserId ||
                                     branches.Contains(x.BranchId) ||
                                     x.PlantId != null && plants.Contains(x.PlantId.Value));
        }
        var items = await query.OrderBy(x => x.CompletedAt)
            .Select(x => new
            {
                x.TripId, x.Number, x.PourId, x.BranchId, x.PlantId, x.Status,
                x.CompletedAt, x.DeliveredQuantity, x.ReceiverName, x.ReceiverMobile,
                x.SignatureReference, x.PhotoReference, x.EvidenceState, x.Version
            }).ToListAsync(ct);
        return Results.Ok(new { items, total = items.Count });
    }

    private static async Task<IResult> GetEvidenceReview(
        Guid tripId, TenantAuthorizationService authorization, BerynxDbContext db,
        IM08SecureEvidenceProvider evidenceProvider, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.evidence.review", ct);
        var trip = await db.Set<M08Trip>().AsNoTracking().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.evidence.review",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct, write: false))
            return Results.NotFound();
        var referencesValid = M08SecureEvidenceReviewRules.TryParseAssetIds(
            trip.SignatureReference, trip.PhotoReference, out var assetIds);
        var evidence = referencesValid
            ? await evidenceProvider.GetForReviewAsync(trip.TenantId, trip.TripId, assetIds, ct)
            : new M08SecureEvidenceReviewResult(false, [], "Evidence references are missing or invalid.");
        var history = await db.Set<M08TripEvent>().AsNoTracking()
            .Where(x => x.TripId == tripId &&
                (x.EventType == "Receiver evidence verified" || x.EventType == "Receiver evidence rejected" ||
                 x.EventType == "Receiver evidence resubmitted"))
            .OrderBy(x => x.ReceivedAt)
            .Select(x => new { x.ActorUserId, x.EventType, x.DetailJson, x.ReceivedAt })
            .ToListAsync(ct);
        return Results.Ok(new
        {
            trip.TripId, trip.Number, trip.Status, trip.Version, trip.CompletedAt,
            trip.DeliveredQuantity, trip.ReceiverName, trip.ReceiverMobile,
            trip.SignatureReference, trip.PhotoReference, trip.EvidenceState,
            trip.ReceiverVerifiedByUserId, trip.ReceiverVerifiedAt, trip.EvidenceDecisionReason,
            canReview = M08Rules.CanReviewReceiverEvidence(trip),
            canVerify = M08Rules.CanReviewReceiverEvidence(trip) &&
                M08SecureEvidenceReviewRules.IsReviewable(trip.TenantId, trip.TripId, trip.SignatureReference, trip.PhotoReference, evidence),
            attachmentServiceAvailable = evidence.IsAvailable,
            attachmentBlocker = evidence.IsAvailable ? null : evidence.Detail,
            assets = M08SecureEvidenceReviewRules.IsReviewable(trip.TenantId, trip.TripId, trip.SignatureReference, trip.PhotoReference, evidence) ? evidence.Assets : [], history
        });
    }

    private static async Task<IResult> ReviewEvidence(
        Guid tripId, EvidenceReviewWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant,
        IM08SecureEvidenceProvider evidenceProvider, IM18DocumentService documents, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.evidence.review", ct);
        if (request.Decision is not ("Verify" or "Reject"))
            throw new ValidationException("Choose Verify or Reject.");
        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 3 || reason.Length > 1000)
            throw new ValidationException("A decision reason of 3 to 1000 characters is required.");
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.evidence.review",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct))
            return Results.NotFound();
        if (trip.Version != request.Version)
            return Results.Conflict(new { detail = "Trip changed. Reload the evidence review.", trip.Version });
        if (!M08Rules.CanReviewReceiverEvidence(trip))
            return Results.Conflict(new { detail = "Only submitted evidence for a completed, unreviewed trip can be decided." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (request.Decision == "Verify")
        {
            if (!M08SecureEvidenceReviewRules.TryParseAssetIds(
                    trip.SignatureReference, trip.PhotoReference, out var assetIds))
                throw new ValidationException("Evidence must reference secure M18 document asset IDs.");
            var evidence = await evidenceProvider.GetForReviewAsync(tenant.TenantId, trip.TripId, assetIds, ct);
            if (!evidence.IsAvailable)
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Secure evidence review is unavailable",
                    detail: evidence.Detail ?? "Configure M18 secure storage and malware scanning before verification.");
            if (!M08SecureEvidenceReviewRules.IsReviewable(tenant.TenantId, trip.TripId,
                    trip.SignatureReference, trip.PhotoReference, evidence))
                return Results.Conflict(new { detail = "Evidence is missing, cross-tenant, not cleanly scanned, unavailable, or lacks a current, clean-scanned M18 evidence." });
            await authorization.RequireModule("M09", write: true, cancellationToken: ct);
        }

        var now = DateTimeOffset.UtcNow;
        var oldEvidenceState = trip.EvidenceState;
        trip.EvidenceState = request.Decision == "Verify" ? "Verified" : "Rejected";
        trip.ReceiverVerifiedAt = request.Decision == "Verify" ? now : null;
        trip.ReceiverVerifiedByUserId = request.Decision == "Verify" ? actor.UserId : null;
        trip.EvidenceDecisionReason = reason;
        trip.Version++;
        var actionId = "evidence-review-" + Guid.NewGuid().ToString("N");
        db.Add(TripEvent(tenant.TenantId, tripId, actor.UserId,
            request.Decision == "Verify" ? "Receiver evidence verified" : "Receiver evidence rejected",
            trip.Status, trip.Status,
            new
            {
                decision = request.Decision, reason, version = trip.Version,
                verifiedBy = trip.ReceiverVerifiedByUserId, verifiedAt = trip.ReceiverVerifiedAt,
                oldEvidenceState, trip.SignatureReference, trip.PhotoReference
            }, now, actionId));
        if (!await M09Endpoints.SyncReceiverVerification(db, tenant.TenantId, actor.UserId, trip,
                request.Decision == "Verify" ? "Receiver evidence verified" : "Receiver evidence rejected", ct))
            return Results.Conflict(new { detail = "The linked challan is missing or already invoiced; evidence cannot be re-reviewed." });
        if (M08SecureEvidenceReviewRules.TryParseAssetIds(trip.SignatureReference, trip.PhotoReference, out var reviewedAssetIds))
            await documents.RecordReviewAsync(trip.TripId, actor.UserId, trip.EvidenceState, reason, reviewedAssetIds, ct);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "Evidence was reviewed concurrently. Reload and decide once." });
        }
        return Results.Ok(new
        {
            trip.TripId, trip.EvidenceState, trip.ReceiverVerifiedByUserId,
            trip.ReceiverVerifiedAt, trip.EvidenceDecisionReason, trip.Version,
            challanEligibility = request.Decision == "Verify" ? "Delivered" : "Unverified"
        });
    }

    private static async Task<IResult> ResubmitEvidence(
        Guid tripId, EvidenceResubmissionWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, IM08SecureEvidenceProvider evidenceProvider, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M08", "m08.trips.update", ct);
        if (string.IsNullOrWhiteSpace(request.ReceiverName) || request.ReceiverName.Trim().Length > 160 ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 3 ||
            request.Reason.Trim().Length > 1000)
            throw new ValidationException("Receiver name and a resubmission reason are required.");
        if (!M08SecureEvidenceReviewRules.TryParseAssetIds(
                request.SignatureReference, request.PhotoReference, out _))
            throw new ValidationException("Provide at least one unique secure M18 signature or photo asset ID.");
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tripId, ct);
        if (trip is null || !await authorization.CanAccess("M08", actor.UserId, "m08.trips.update",
                trip.BranchId, trip.PlantId, trip.CreatedByUserId, ct))
            return Results.NotFound();
        if (trip.Status != "Completed" || trip.EvidenceState is not ("Rejected" or "Missing"))
            return Results.Conflict(new { detail = "Evidence can only be resubmitted for a completed trip whose evidence is missing or rejected." });
        if (trip.Version != request.Version)
            return Results.Conflict(new { detail = "Trip changed. Reload before resubmitting evidence.", trip.Version });
        if (!string.IsNullOrWhiteSpace(request.SignatureReference) &&
            !string.Equals(request.SignatureReference.Trim(), trip.SignatureReference, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrWhiteSpace(request.PhotoReference) &&
            !string.Equals(request.PhotoReference.Trim(), trip.PhotoReference, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Evidence references must come from a completed M18 upload for this trip.");
        if (!M08SecureEvidenceReviewRules.TryParseAssetIds(trip.SignatureReference, trip.PhotoReference, out var resubmittedIds))
            throw new ValidationException("Upload at least one signature or photo and wait for the clean scan before resubmitting.");
        var resubmittedEvidence = await evidenceProvider.GetForReviewAsync(trip.TenantId, trip.TripId, resubmittedIds, ct);
        if (!M08SecureEvidenceReviewRules.IsReviewable(trip.TenantId, trip.TripId,
                trip.SignatureReference, trip.PhotoReference, resubmittedEvidence))
            return Results.Conflict(new { detail = "Evidence must be current, trip-bound, and clean-scanned before resubmission." });
        var oldSignature = trip.SignatureReference;
        var oldPhoto = trip.PhotoReference;
        var fromState = trip.EvidenceState;
        trip.ReceiverName = request.ReceiverName.Trim();
        trip.ReceiverMobile = request.ReceiverMobile?.Trim();
        trip.EvidenceState = "Submitted";
        trip.ReceiverVerifiedAt = null;
        trip.ReceiverVerifiedByUserId = null;
        trip.EvidenceDecisionReason = null;
        trip.Version++;
        var now = DateTimeOffset.UtcNow;
        db.Add(TripEvent(tenant.TenantId, tripId, actor.UserId, "Receiver evidence resubmitted",
            trip.Status, trip.Status,
            new { fromState, reason = request.Reason.Trim(), oldSignature, oldPhoto,
                trip.SignatureReference, trip.PhotoReference, trip.ReceiverName }, now,
            "evidence-resubmit-" + Guid.NewGuid().ToString("N")));
        if (!await M09Endpoints.SyncReceiverVerification(db, tenant.TenantId, actor.UserId, trip, "Receiver evidence resubmitted", ct))
            return Results.Conflict(new { detail = "The linked challan is missing or already invoiced." });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "Trip changed concurrently. Reload and retry." });
        }
        return Results.Ok(new { trip.TripId, trip.EvidenceState, trip.Version });
    }
    private static async Task EnsureNoAssignmentConflict(
        BerynxDbContext db, M08Pour target, Guid? vehicleId, Guid? driverId, CancellationToken ct)
    {
        var assignments = await (
            from trip in db.Set<M08Trip>().AsNoTracking()
            join pour in db.Set<M08Pour>().AsNoTracking()
                on new { trip.TenantId, trip.PourId } equals new { pour.TenantId, pour.PourId }
            where trip.Status != "Completed" && trip.Status != "Cancelled" &&
                  trip.Status != "Rejected" && trip.Status != "Returned" &&
                  (vehicleId != null && trip.VehicleId == vehicleId ||
                   driverId != null && trip.DriverId == driverId) &&
                  pour.WindowStart < target.WindowEnd && pour.WindowEnd > target.WindowStart
            select new { pour.WindowStart, pour.WindowEnd }
        ).ToListAsync(ct);
        M08Rules.ValidateAssignment(target.WindowStart, target.WindowEnd,
            assignments.Select(x => (x.WindowStart, x.WindowEnd)));
    }

    private static M08TripEvent TripEvent(
        Guid tenantId, Guid tripId, Guid actorId, string eventType, string from,
        string to, object detail, DateTimeOffset deviceAt, string actionId) =>
        new()
        {
            TenantId = tenantId, EventId = Guid.NewGuid(), TripId = tripId,
            ActorUserId = actorId, EventType = eventType, FromStatus = from,
            ToStatus = to, DetailJson = JsonSerializer.Serialize(detail, Json),
            DeviceAt = deviceAt, ReceivedAt = DateTimeOffset.UtcNow, ActionId = actionId
        };

    private static void ValidatePour(PourWrite request)
    {
        if (request.ScheduleId == Guid.Empty || request.WindowEnd <= request.WindowStart ||
            request.PlacementMethod is not ("Pump" or "Boom" or "Crane" or "Direct") ||
            request.PumpQuantity < 0 || request.PumpQuantity > 10000 ||
            request.Slump?.Length > 40 || request.PumpType?.Length > 80 ||
            request.CustomerEngineer?.Length > 120 || request.Coordinator?.Length > 120 ||
            request.SpecialInstructions?.Length > 2000)
            throw new ValidationException("Pour schedule window, placement or details are invalid.");
    }
}
