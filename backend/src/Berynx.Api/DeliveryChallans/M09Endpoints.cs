using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.SalesOrders;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.DeliveryChallans;

public static class M09Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapM09(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m09")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M09",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/challans", List);
        api.MapGet("/challans/{challanId:guid}", Detail);
        api.MapGet("/challans/{challanId:guid}/print", Print);
    }

    public static async Task<bool> SyncReceiverVerification(
        BerynxDbContext db, Guid tenantId, Guid actorUserId, M08Trip trip, string eventType, CancellationToken ct)
    {
        var challan = await db.Set<M09DeliveryChallan>()
            .SingleOrDefaultAsync(x => x.TripId == trip.TripId, ct);
        if (challan is null || challan.InvoiceId is not null) return false;

        var nextStatus = M09Rules.StatusForReceiverVerification(M08Rules.IsReceiverVerified(trip));
        if (challan.Status != nextStatus) M09Rules.RequireTransition(challan.Status, nextStatus);
        challan.Status = nextStatus;
        challan.EvidenceState = trip.EvidenceState;
        challan.ReceiverName = trip.ReceiverName;
        challan.ReceiverMobile = trip.ReceiverMobile;
        challan.SignatureReference = trip.SignatureReference;
        challan.PhotoReference = trip.PhotoReference;
        challan.ReceiverVerifiedAt = trip.ReceiverVerifiedAt;
        challan.Version++;
        db.Add(new M09ChallanEvent
        {
            TenantId = tenantId, EventId = Guid.NewGuid(), ChallanId = challan.ChallanId,
            ActorUserId = actorUserId,
            EventType = eventType,
            DetailJson = JsonSerializer.Serialize(new
            {
                tripId = trip.TripId, trip.EvidenceState, trip.ReceiverVerifiedByUserId,
                trip.ReceiverVerifiedAt, trip.EvidenceDecisionReason
            }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        return true;
    }
    public static async Task<M09DeliveryChallan> CreateFromCompletedTrip(
        BerynxDbContext db, Guid tenantId, Guid actorUserId,
        M08Trip trip, M08Pour pour, M06SalesOrder order, M06SalesOrderLine line,
        CancellationToken ct)
    {
        if (trip.Status != "Completed")
            throw new ValidationException("Only a completed M08 trip can generate a challan.");
        var existing = await db.Set<M09DeliveryChallan>()
            .SingleOrDefaultAsync(x => x.TripId == trip.TripId, ct);
        if (existing is not null) return existing;
        if (order.BranchId is not { } branchId)
            throw new ValidationException("A branch is required to number a delivery challan.");
        var tenantRow = await db.Set<Tenant>().AsNoTracking().SingleAsync(ct);
        var setup = OnboardingStore.Decode(tenantRow);
        var series = setup.Series.SingleOrDefault(x =>
            x.BranchId == branchId && x.Document.Equals("Challan", StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException("Configure a Challan numbering series for this branch before completing delivery.");
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(setup.TimeZone);
        var localDeliveredAt = TimeZoneInfo.ConvertTime(trip.CompletedAt!.Value, timeZone);
        var challanDate = DateOnly.FromDateTime(localDeliveredAt.DateTime);
        var startMonth = setup.StartMonth ?? 4;
        var financialYear = challanDate.Year - (challanDate.Month < startMonth ? 1 : 0);
        var sequence = await db.Database.SqlQuery<long>($"""
            INSERT INTO m09_challan_number_counter
                ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart", "NextNumber")
            VALUES ({tenantId}, {branchId}, {"Challan"}, {financialYear}, 2)
            ON CONFLICT ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart")
            DO UPDATE SET "NextNumber" = m09_challan_number_counter."NextNumber" + 1
            RETURNING "NextNumber" - 1 AS "Value"
            """).SingleAsync(ct);
        var vehicle = trip.VehicleId is { } vehicleId
            ? await db.Set<M03Vehicle>().AsNoTracking().SingleOrDefaultAsync(x => x.VehicleId == vehicleId, ct)
            : null;
        var driver = trip.DriverId is { } driverId
            ? await db.Set<M03Driver>().AsNoTracking().SingleOrDefaultAsync(x => x.DriverId == driverId, ct)
            : null;
        var plant = pour.PlantId is { } plantId
            ? await db.Set<M03Plant>().AsNoTracking().SingleOrDefaultAsync(x => x.PlantId == plantId, ct)
            : null;
        var grossValue = line.OrderedQuantity > 0
            ? decimal.Round(trip.DeliveredQuantity * line.Total / line.OrderedQuantity, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var challan = new M09DeliveryChallan
        {
            TenantId = tenantId,
            ChallanId = Guid.NewGuid(),
            TripId = trip.TripId,
            PourId = pour.PourId,
            SalesOrderId = order.SalesOrderId,
            SalesOrderLineId = line.LineId,
            BranchId = branchId,
            CustomerPartyId = order.CustomerPartyId,
            SiteId = order.SiteId,
            ProductId = line.ProductId,
            Number = SetupRules.Number(series, startMonth, challanDate, sequence),
            ChallanDate = challanDate,
            DeliveredAt = trip.CompletedAt.Value,
            Status = M09Rules.StatusForReceiverVerification(M08Rules.IsReceiverVerified(trip)),
            CustomerNameSnapshot = order.CustomerNameSnapshot,
            SiteAddressSnapshot = order.SiteAddressSnapshot,
            OrderNumberSnapshot = order.Number,
            CustomerPoNumberSnapshot = order.CustomerPoNumber,
            ProductNameSnapshot = line.ProductNameSnapshot,
            UnitCodeSnapshot = line.UnitCode,
            DeliveredQuantity = trip.DeliveredQuantity,
            ReturnedQuantity = trip.ReturnedQuantity,
            UnitRateSnapshot = line.Rate,
            TaxPercentSnapshot = line.GstPercent,
            DeliveredValueSnapshot = grossValue,
            VehicleNumberSnapshot = vehicle?.RegistrationNumber ?? "",
            DriverNameSnapshot = driver?.Name ?? "",
            PlantNameSnapshot = plant?.Name ?? "",
            BatchSlipNumber = trip.BatchSlipNumber ?? "",
            ReceiverName = trip.ReceiverName,
            ReceiverMobile = trip.ReceiverMobile,
            EvidenceState = trip.EvidenceState,
            ReceiverVerifiedAt = trip.ReceiverVerifiedAt,
            SignatureReference = trip.SignatureReference,
            PhotoReference = trip.PhotoReference,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        db.Add(challan);
        db.Add(new M09ChallanEvent
        {
            TenantId = tenantId,
            EventId = Guid.NewGuid(),
            ChallanId = challan.ChallanId,
            ActorUserId = actorUserId,
            EventType = "Generated from completed trip",
            DetailJson = JsonSerializer.Serialize(new
            {
                tripId = trip.TripId, tripNumber = trip.Number, deliveredQuantity = trip.DeliveredQuantity,
                trip.EvidenceState, challan.Status, challan.Number
            }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        return challan;
    }

    private static async Task<IResult> List(
        string? status, Guid? branchId, Guid? customerId, DateOnly? from, DateOnly? to,
        int? page, int? pageSize, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M09", "m09.challans.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m09.challans.view", ct);
        var query = db.Set<M09DeliveryChallan>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        if (branchId is { } branch) query = query.Where(x => x.BranchId == branch);
        if (customerId is { } customer) query = query.Where(x => x.CustomerPartyId == customer);
        if (from is { } start) query = query.Where(x => x.ChallanDate >= start);
        if (to is { } end) query = query.Where(x => x.ChallanDate <= end);
        if (!scopes.Any(x => x.ScopeType == "All"))
        {
            var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
                .Select(x => x.ScopeId!.Value).ToArray();
            query = query.Where(x => branches.Contains(x.BranchId));
        }
        var total = await query.CountAsync(ct);
        var safePage = Math.Max(1, page ?? 1);
        var safeSize = Math.Clamp(pageSize ?? 50, 1, 200);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await query.OrderByDescending(x => x.ChallanDate).ThenBy(x => x.Number)
            .Skip((safePage - 1) * safeSize).Take(safeSize)
            .Select(x => new
            {
                x.ChallanId, x.Number, x.ChallanDate, x.Status, x.CustomerNameSnapshot,
                x.SiteAddressSnapshot, x.OrderNumberSnapshot, x.ProductNameSnapshot,
                x.DeliveredQuantity, x.DeliveredValueSnapshot, x.EvidenceState,
                ageDays = today.DayNumber - x.ChallanDate.DayNumber
            }).ToListAsync(ct);
        return Results.Ok(new
        {
            items = rows,
            total,
            page = safePage,
            pageSize = safeSize,
            groups = rows.GroupBy(x => new { x.CustomerNameSnapshot, x.SiteAddressSnapshot })
                .Select(g => new { g.Key.CustomerNameSnapshot, g.Key.SiteAddressSnapshot, Count = g.Count() })
        });
    }

    private static async Task<IResult> Detail(
        Guid challanId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M09", "m09.challans.view", ct);
        var challan = await db.Set<M09DeliveryChallan>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ChallanId == challanId, ct);
        if (challan is null) return Results.NotFound();
        await RequireBranchScope(authorization, actor.UserId, challan.BranchId, ct);
        var history = await db.Set<M09ChallanEvent>().AsNoTracking()
            .Where(x => x.ChallanId == challanId).OrderBy(x => x.OccurredAt)
            .Select(x => new { x.EventType, x.DetailJson, x.OccurredAt })
            .ToListAsync(ct);
        var revisions = await db.Set<M09ChallanRevision>().AsNoTracking()
            .Where(x => x.ChallanId == challanId).OrderBy(x => x.RevisionNumber)
            .Select(x => new
            {
                x.RevisionNumber, x.Status, x.Reason, x.OriginalQuantity,
                x.CorrectedQuantity, x.CreatedAt, x.AppliedAt
            }).ToListAsync(ct);
        return Results.Ok(new { challan, history, revisions });
    }

    private static async Task<IResult> Print(
        Guid challanId, HttpContext http, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M09", "m09.challans.view", ct);
        var row = await db.Set<M09DeliveryChallan>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ChallanId == challanId, ct);
        if (row is null) return Results.NotFound();
        await RequireBranchScope(authorization, actor.UserId, row.BranchId, ct);
        var safeCustomer = System.Net.WebUtility.HtmlEncode(row.CustomerNameSnapshot);
        var safeSite = System.Net.WebUtility.HtmlEncode(row.SiteAddressSnapshot);
        var safeProduct = System.Net.WebUtility.HtmlEncode(row.ProductNameSnapshot);
        var safeReceiver = System.Net.WebUtility.HtmlEncode(row.ReceiverName ?? "");
        var html = $$"""
            <!doctype html><html><head><meta charset="utf-8"><title>Delivery challan {{System.Net.WebUtility.HtmlEncode(row.Number)}}</title>
            <style>body{font:14px Arial,sans-serif;margin:24px;color:#292723}header{border-bottom:2px solid #a65d3b;padding-bottom:12px}table{width:100%;border-collapse:collapse;margin-top:20px}td,th{border:1px solid #bbb;padding:10px;text-align:left}.copies{margin-top:30px;color:#555}@media print { button { display: none } }</style></head>
            <body><button onclick="window.print()">Print</button><header><h1>Delivery Challan</h1>
            <strong>{{System.Net.WebUtility.HtmlEncode(row.Number)}}</strong> · {{row.ChallanDate:dd-MMM-yyyy}} · {{System.Net.WebUtility.HtmlEncode(row.Status)}}</header>
            <p><b>Customer:</b> {{safeCustomer}}<br><b>Delivery site:</b> {{safeSite}}<br>
            <b>Sales order:</b> {{System.Net.WebUtility.HtmlEncode(row.OrderNumberSnapshot)}} · PO {{System.Net.WebUtility.HtmlEncode(row.CustomerPoNumberSnapshot ?? "—")}}</p>
            <table><thead><tr><th>Product</th><th>Quantity</th><th>Unit</th><th>Batch slip</th></tr></thead>
            <tbody><tr><td>{{safeProduct}}</td><td>{{row.DeliveredQuantity:0.###}}</td><td>{{System.Net.WebUtility.HtmlEncode(row.UnitCodeSnapshot)}}</td><td>{{System.Net.WebUtility.HtmlEncode(row.BatchSlipNumber)}}</td></tr></tbody></table>
            <p>Vehicle {{System.Net.WebUtility.HtmlEncode(row.VehicleNumberSnapshot)}} · Driver {{System.Net.WebUtility.HtmlEncode(row.DriverNameSnapshot)}} · Plant {{System.Net.WebUtility.HtmlEncode(row.PlantNameSnapshot)}}</p>
            <p>Receiver: {{safeReceiver}} · Evidence: {{System.Net.WebUtility.HtmlEncode(row.EvidenceState)}}</p>
            <p>Signature reference: {{System.Net.WebUtility.HtmlEncode(row.SignatureReference ?? "—")}} · Photo reference: {{System.Net.WebUtility.HtmlEncode(row.PhotoReference ?? "—")}}</p>
            <div class="copies">Customer copy · Accounts copy · Driver copy</div></body></html>
            """;
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task RequireBranchScope(
        TenantAuthorizationService authorization, Guid userId, Guid branchId, CancellationToken ct)
    {
        var scopes = await authorization.Scopes(userId, "m09.challans.view", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == branchId))
            throw new UnauthorizedAccessException("No access to this challan branch.");
    }
}
