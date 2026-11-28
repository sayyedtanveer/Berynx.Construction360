using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Api.Accounting;
using Berynx.Application.Accounting;
using Berynx.Domain.Accounting;
using Berynx.Application.Invoicing;
using Berynx.Application.Tenancy;
using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;
using Berynx.Domain.Invoicing;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Receivables;
using Berynx.Domain.SalesOrders;
using Berynx.Api.Receivables;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Invoicing;

public static class M10Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void MapM10(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m10")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M10",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/eligible-challans", EligibleChallans);
        api.MapGet("/invoices", List);
        api.MapPost("/invoices/drafts", CreateDraft);
        api.MapGet("/invoices/{invoiceId:guid}", Detail);
        api.MapPost("/invoices/{invoiceId:guid}/cancel-draft", CancelDraft);
        api.MapPost("/invoices/{invoiceId:guid}/post", PostInvoice);
        api.MapPost("/invoices/{invoiceId:guid}/cancel", CancelPostedInvoice);
    }

    private static async Task<IResult> EligibleChallans(
        Guid? branchId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M10", "m10.invoices.create", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m10.invoices.create", ct);
        var query = db.Set<M09DeliveryChallan>().AsNoTracking()
            .Where(x => x.Status == "Delivered" && x.EvidenceState == "Verified" && x.ReceiverVerifiedAt != null &&
                        (x.SignatureReference != null && x.SignatureReference != "" || x.PhotoReference != null && x.PhotoReference != "") &&
                        x.ReceiverName != null && x.ReceiverName != "" && x.InvoiceId == null &&
                        db.Set<M08Trip>().Any(t => t.TripId == x.TripId && t.Status == "Completed" &&
                            t.EvidenceState == "Verified" && t.ReceiverVerifiedAt != null && t.ReceiverVerifiedByUserId != null &&
                            t.ReceiverName == x.ReceiverName && t.SignatureReference == x.SignatureReference &&
                            t.PhotoReference == x.PhotoReference && t.ReceiverVerifiedAt == x.ReceiverVerifiedAt &&
                            (t.SignatureReference != null && t.SignatureReference != "" || t.PhotoReference != null && t.PhotoReference != "")) &&
                        !db.Set<M10InvoiceChallan>().Any(link =>
                            link.ChallanId == x.ChallanId && link.ReleasedAt == null));
        if (branchId is { } branch) query = query.Where(x => x.BranchId == branch);
        query = ApplyBranchScope(query, scopes);
        var items = await query
            .Join(db.Set<M03Party>().AsNoTracking(), challan => challan.CustomerPartyId,
                party => party.PartyId, (challan, party) => new
            {
                challan.ChallanId, challan.Number, challan.ChallanDate, challan.BranchId,
                challan.CustomerPartyId, party.BillingMode,
                challan.CustomerNameSnapshot, challan.SiteId, challan.SiteAddressSnapshot,
                challan.OrderNumberSnapshot, challan.ProductNameSnapshot, challan.DeliveredQuantity,
                challan.DeliveredValueSnapshot, challan.EvidenceState
            }).OrderBy(x => x.ChallanDate).ToListAsync(ct);
        var pendingQuery = ApplyBranchScope(
            db.Set<M09DeliveryChallan>().AsNoTracking().Where(x => x.Status == "Unverified"), scopes);
        return Results.Ok(new { items, pendingVerification = await pendingQuery.CountAsync(ct) });
    }

    private static async Task<IResult> List(
        string? status, Guid? branchId, int? page, int? pageSize,
        TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M10", "m10.invoices.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m10.invoices.view", ct);
        var query = db.Set<M10Invoice>().AsNoTracking().AsQueryable();
        if (branchId is { } branch) query = query.Where(x => x.BranchId == branch);
        query = ApplyBranchScope(query, scopes);

        var postedAllocations = PostedReceiptAllocationQuery.Build(db);
        var collectionQuery =
            from invoice in query
            join paid in postedAllocations on invoice.InvoiceId equals paid.InvoiceId into paidRows
            from paid in paidRows.DefaultIfEmpty()
            select new { Invoice = invoice, PaidAmount = paid == null ? 0m : paid.Amount };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (status == "Paid")
        {
            collectionQuery = collectionQuery.Where(x =>
                x.Invoice.Status == "Posted" && x.PaidAmount >= x.Invoice.TotalAmount);
        }
        else if (status == "Overdue")
        {
            collectionQuery = collectionQuery.Where(x =>
                x.Invoice.Status == "Posted" && x.Invoice.DueDate.HasValue &&
                x.Invoice.DueDate.Value < today && x.PaidAmount < x.Invoice.TotalAmount);
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            collectionQuery = collectionQuery.Where(x => x.Invoice.Status == status);
        }

        var total = await collectionQuery.CountAsync(ct);
        var safePage = Math.Max(1, page ?? 1);
        var safeSize = Math.Clamp(pageSize ?? 50, 1, 200);
        var invoiceRows = await collectionQuery.OrderByDescending(x => x.Invoice.InvoiceDate)
            .Skip((safePage - 1) * safeSize).Take(safeSize)
            .Select(x => new
            {
                x.Invoice.InvoiceId, x.Invoice.Number, x.Invoice.InvoiceDate, x.Invoice.DueDate,
                x.Invoice.Status, x.Invoice.InvoiceType, x.Invoice.CustomerNameSnapshot,
                x.Invoice.BillingModeSnapshot,
                x.Invoice.TaxableAmount, x.Invoice.TotalAmount, x.Invoice.TaxTreatment,
                x.Invoice.EInvoiceStatus, x.Invoice.DisputeHold, x.Invoice.Version,
                amountPaid = x.PaidAmount
            }).ToListAsync(ct);
        var invoices = invoiceRows.Select(x => new
        {
            x.InvoiceId, x.Number, x.InvoiceDate, x.DueDate, x.Status, x.InvoiceType,
            x.CustomerNameSnapshot, x.TaxableAmount, x.TotalAmount, x.TaxTreatment,
            x.BillingModeSnapshot,
            x.EInvoiceStatus, x.DisputeHold, x.Version, x.amountPaid,
            outstanding = M10Rules.Outstanding(x.TotalAmount, x.amountPaid),
            collectionStatus = M10Rules.CollectionStatus(
                x.Status, x.DueDate, x.TotalAmount, x.amountPaid, today)
        }).ToArray();
        return Results.Ok(new { items = invoices, total, page = safePage, pageSize = safeSize });
    }

    private static async Task<IResult> CreateDraft(
        M10CreateDraftRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M10", "m10.invoices.create", ct);
        if (request.ChallanIds is null || request.ChallanIds.Length == 0 || request.ChallanIds.Length > 200)
            throw new ValidationException("Select between one and 200 delivery challans.");
        if (request.ChallanIds.Distinct().Count() != request.ChallanIds.Length)
            throw new ValidationException("The selected challan list contains duplicates.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100)
            throw new ValidationException("An idempotency key of 1–100 characters is required.");
        var scopes = await authorization.Scopes(actor.UserId, "m10.invoices.create", ct);
        var prior = await db.Set<M10Invoice>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (prior is not null)
        {
            if (!scopes.Any(x => x.ScopeType == "All" ||
                                 x.ScopeType == "Branch" && x.ScopeId == prior.BranchId))
                return Results.Forbid();
            var priorChallans = await db.Set<M10InvoiceChallan>().AsNoTracking()
                .Where(x => x.InvoiceId == prior.InvoiceId).Select(x => x.ChallanId).ToArrayAsync(ct);
            if (prior.InvoiceDate != request.InvoiceDate ||
                !priorChallans.Order().SequenceEqual(request.ChallanIds.Order()))
                return Results.Conflict(new { detail = "Idempotency key was already used with a different draft request." });
            return Results.Created($"/api/v1/m10/invoices/{prior.InvoiceId}", new
            {
                prior.InvoiceId, prior.Status, prior.TaxTreatment, prior.TaxableAmount,
                prior.CgstAmount, prior.SgstAmount, prior.IgstAmount,
                prior.RoundOffAmount, prior.TotalAmount, duplicate = true
            });
        }
        var challans = await db.Set<M09DeliveryChallan>()
            .Where(x => request.ChallanIds.Contains(x.ChallanId)).ToListAsync(ct);
        if (challans.Count != request.ChallanIds.Length)
            return Results.NotFound(new { detail = "One or more selected challans do not exist in this tenant." });
        if (!scopes.Any(x => x.ScopeType == "All") &&
            challans.Any(x => !scopes.Any(s => s.ScopeType == "Branch" && s.ScopeId == x.BranchId)))
            return Results.Forbid();
        if (challans.Any(x => x.Status != "Delivered" || x.EvidenceState != "Verified"))
            return Results.Conflict(new
            {
                detail = "Only receiver-verified Delivered challans can be invoiced. Receiver evidence must be approved by an authorized M08 reviewer.",
                unverifiedChallanIds = challans.Where(x => x.Status != "Delivered" || x.EvidenceState != "Verified")
                    .Select(x => x.ChallanId)
            });
        if (challans.GroupBy(x => new { x.BranchId, x.CustomerPartyId, x.SiteId }).Count() != 1)
            throw new ValidationException("An invoice must use one branch, customer and delivery site.");
        if (challans.Any(x => x.InvoiceId is not null) ||
            await db.Set<M10InvoiceChallan>().AnyAsync(x =>
                request.ChallanIds.Contains(x.ChallanId) && x.ReleasedAt == null, ct))
            return Results.Conflict(new { detail = "A selected challan is already reserved by an invoice draft." });

        var tripIds = challans.Select(x => x.TripId).ToArray();
        var trips = await db.Set<M08Trip>().AsNoTracking().Where(x => tripIds.Contains(x.TripId)).ToListAsync(ct);
        if (trips.Count != tripIds.Distinct().Count() || challans.Any(challan => !trips.Any(trip => trip.TripId == challan.TripId && M10Rules.IsInvoiceEligible(challan, trip))))
            return Results.Conflict(new { detail = "M08 delivery facts do not show a completed receiver-verified delivery." });
        if (trips.Any(x => x.WaitingCharge > 0 || x.PartLoadCharge > 0))
            return Results.Conflict(new
            {
                detail = "M08 waiting/part-load charges are not yet integrated into M10 invoice lines; draft generation is blocked to prevent underbilling."
            });

        var first = challans[0];
        var orderIds = challans.Select(x => x.SalesOrderId).Distinct().ToArray();
        var orderLineIds = challans.Select(x => x.SalesOrderLineId).Distinct().ToArray();
        var orders = await db.Set<M06SalesOrder>().AsNoTracking()
            .Where(x => orderIds.Contains(x.SalesOrderId)).ToDictionaryAsync(x => x.SalesOrderId, ct);
        var orderLines = await db.Set<M06SalesOrderLine>().AsNoTracking()
            .Where(x => orderLineIds.Contains(x.LineId)).ToDictionaryAsync(x => x.LineId, ct);
        if (challans.Any(c => !orders.ContainsKey(c.SalesOrderId) || !orderLines.ContainsKey(c.SalesOrderLineId)))
            return Results.Conflict(new { detail = "A source sales-order line is missing; invoice draft was not created." });

        var site = first.SiteId is { } siteId
            ? await db.Set<M03Site>().AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == siteId, ct)
            : null;
        if (site is null)
            return Results.Conflict(new { detail = "A delivery-site state is required to determine place of supply." });
        var tenantRow = await db.Set<Tenant>().AsNoTracking().SingleAsync(ct);
        var setup = OnboardingStore.Decode(tenantRow);
        var branch = setup.Branches.SingleOrDefault(x => x.Id == first.BranchId);
        if (branch is null || string.IsNullOrWhiteSpace(branch.Gstin))
            return Results.Conflict(new { detail = "Configure the branch GSTIN before generating an invoice draft." });
        string taxTreatment;
        try
        {
            taxTreatment = M10Rules.TaxTreatment(site.StateCode, branch.Gstin[..2]);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { detail = ex.Message });
        }

        var customer = await db.Set<M03Party>().AsNoTracking()
            .SingleAsync(x => x.PartyId == first.CustomerPartyId, ct);
        string billingMode;
        try
        {
            billingMode = M10Rules.ResolveBillingMode(
                customer.BillingMode, request.BillingModeOverride, request.BillingModeOverrideReason);
            M10Rules.ValidateBillingGrouping(billingMode,
                challans.Select(x => (x.SalesOrderId, x.ChallanDate)).ToArray());
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { detail = ex.Message });
        }
        var invoiceId = Guid.NewGuid();
        var invoice = new M10Invoice
        {
            TenantId = tenant.TenantId,
            InvoiceId = invoiceId,
            BranchId = first.BranchId,
            CustomerPartyId = first.CustomerPartyId,
            SiteId = first.SiteId,
            InvoiceDate = request.InvoiceDate,
            IdempotencyKey = request.IdempotencyKey,
            Status = "Draft",
            InvoiceType = M10Rules.InvoiceType(branch.GstRegistrationType),
            TaxTreatment = branch.GstRegistrationType == "Composition" ? "Bill of supply" : taxTreatment,
            BillingModeSnapshot = billingMode,
            PlaceOfSupplyStateCode = site.StateCode,
            CustomerNameSnapshot = customer.TradeName ?? customer.LegalName,
            CustomerGstinSnapshot = customer.Gstin ?? "",
            BranchGstinSnapshot = branch.Gstin,
            Notes = request.Notes?.Trim() ?? "",
            EInvoiceStatus = "Not prepared",
            PostingSnapshotJson = "{}",
            CreatedByUserId = actor.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var productIds = orderLines.Values.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Set<M03Product>().AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId)).ToDictionaryAsync(x => x.ProductId, ct);
        var lineGroups = challans.GroupBy(x => x.SalesOrderLineId);
        foreach (var group in lineGroups)
        {
            var sourceLine = orderLines[group.Key];
            var totalQuantity = group.Sum(x => x.DeliveredQuantity);
            M10Rules.ValidateDeliveredQuantity(
                sourceLine.DeliveredQuantity, sourceLine.InvoicedQuantity, totalQuantity);
            var taxable = sourceLine.OrderedQuantity > 0
                ? decimal.Round(totalQuantity * sourceLine.TaxableAmount / sourceLine.OrderedQuantity, 2, MidpointRounding.AwayFromZero)
                : 0m;
            var tax = branch.GstRegistrationType == "Composition" ? 0m : decimal.Round(taxable * sourceLine.GstPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var cgst = taxTreatment == "IGST" ? 0m : decimal.Round(tax / 2m, 2, MidpointRounding.AwayFromZero);
            var sgst = taxTreatment == "IGST" ? 0m : tax - cgst;
            var igst = taxTreatment == "IGST" ? tax : 0m;
            var product = products[sourceLine.ProductId];
            var line = new M10InvoiceLine
            {
                TenantId = tenant.TenantId,
                InvoiceLineId = Guid.NewGuid(),
                InvoiceId = invoiceId,
                SalesOrderId = group.First().SalesOrderId,
                SalesOrderLineId = sourceLine.LineId,
                ProductId = sourceLine.ProductId,
                ProductNameSnapshot = sourceLine.ProductNameSnapshot,
                HsnCodeSnapshot = product.HsnCode,
                UnitCodeSnapshot = sourceLine.UnitCode,
                ChallanReferencesJson = JsonSerializer.Serialize(group.Select(x => x.Number), Json),
                Quantity = totalQuantity,
                Rate = sourceLine.Rate,
                TaxableAmount = taxable,
                TaxPercent = sourceLine.GstPercent,
                CgstAmount = cgst,
                SgstAmount = sgst,
                IgstAmount = igst,
                TotalAmount = taxable + tax,
                TaxSnapshotJson = JsonSerializer.Serialize(new
                {
                    sourceLine.GstPercent, taxTreatment,
                    source = "Frozen M06 order-line rate and tax snapshot"
                }, Json)
            };
            db.Add(line);
            invoice.TaxableAmount += taxable;
            invoice.CgstAmount += cgst;
            invoice.SgstAmount += sgst;
            invoice.IgstAmount += igst;
        }
        var preRoundTotal = invoice.TaxableAmount + invoice.CgstAmount + invoice.SgstAmount + invoice.IgstAmount;
        invoice.RoundOffAmount = M10Rules.RoundOff(preRoundTotal);
        invoice.TotalAmount = preRoundTotal + invoice.RoundOffAmount;
        db.Add(invoice);
        db.Add(new M10InvoiceEvent
        {
            TenantId = tenant.TenantId,
            EventId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            ActorUserId = actor.UserId,
            EventType = "Draft created from delivered challans",
            DetailJson = JsonSerializer.Serialize(new
            {
                challanIds = request.ChallanIds,
                billingMode,
                billingModeOverridden = billingMode != customer.BillingMode,
                billingModeOverrideReason = billingMode != customer.BillingMode
                    ? request.BillingModeOverrideReason?.Trim() : null
            }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        foreach (var challan in challans)
        {
            db.Add(new M10InvoiceChallan
            {
                TenantId = tenant.TenantId,
                InvoiceChallanId = Guid.NewGuid(),
                InvoiceId = invoiceId,
                ChallanId = challan.ChallanId,
                Quantity = challan.DeliveredQuantity,
                DeliveredValueSnapshot = challan.DeliveredValueSnapshot
            });
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "One or more challans were reserved concurrently; reload the unbilled list." });
        }
        return Results.Created($"/api/v1/m10/invoices/{invoiceId}", new
        {
            invoice.InvoiceId, invoice.Status, invoice.TaxTreatment,
            invoice.TaxableAmount, invoice.CgstAmount, invoice.SgstAmount,
            invoice.IgstAmount, invoice.RoundOffAmount, invoice.TotalAmount,
            message = "Draft saved. Posting will revalidate verified delivery, tenant mappings, numbering and accounting-period controls."
        });
    }

    private static async Task<IResult> Detail(
        Guid invoiceId, TenantAuthorizationService authorization, BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M10", "m10.invoices.view", ct);
        var invoice = await db.Set<M10Invoice>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
        if (invoice is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m10.invoices.view", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == invoice.BranchId))
            return Results.Forbid();
        var lines = await db.Set<M10InvoiceLine>().AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId).ToListAsync(ct);
        var challans = await db.Set<M10InvoiceChallan>().AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId).Join(
                db.Set<M09DeliveryChallan>().AsNoTracking(),
                link => new { link.TenantId, link.ChallanId },
                challan => new { challan.TenantId, challan.ChallanId },
                (link, challan) => new
                {
                    challan.ChallanId, challan.Number, link.Quantity, challan.Status,
                    link.ReleasedAt
                })
            .ToListAsync(ct);
        var events = await db.Set<M10InvoiceEvent>().AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId).OrderBy(x => x.OccurredAt)
            .Select(x => new { x.EventType, x.DetailJson, x.OccurredAt }).ToListAsync(ct);
        return Results.Ok(new { invoice, lines, challans, events });
    }

    private static async Task<IResult> CancelDraft(
        Guid invoiceId, M10InvoiceActionRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M10", "m10.invoices.create", ct);
        var invoice = await db.Set<M10Invoice>().SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
        if (invoice is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m10.invoices.create", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == invoice.BranchId))
            return Results.Forbid();
        if (invoice.Status != "Draft")
            return Results.Conflict(new { detail = "Only a draft can be cancelled here; posted invoices require controlled reversal." });
        if (invoice.Version != request.Version)
            return Results.Conflict(new { detail = "Invoice draft changed. Reload before cancelling." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A cancellation reason is required.");
        var links = await db.Set<M10InvoiceChallan>().Where(x => x.InvoiceId == invoiceId).ToListAsync(ct);
        db.RemoveRange(links);
        invoice.Status = "Cancelled";
        invoice.Version++;
        db.Add(new M10InvoiceEvent
        {
            TenantId = tenant.TenantId,
            EventId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            ActorUserId = actor.UserId,
            EventType = "Draft cancelled",
            DetailJson = JsonSerializer.Serialize(new { reason = request.Reason.Trim() }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "Invoice draft changed concurrently; reload and retry." });
        }
        return Results.Ok(new { invoice.InvoiceId, invoice.Status, releasedChallanCount = links.Count });
    }

    private static async Task<IResult> PostInvoice(
        Guid invoiceId, M10InvoiceActionRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, IM12PostingEngine posting,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M10", "m10.invoices.post", ct);
        var invoice = await db.Set<M10Invoice>().SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
        if (invoice is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m10.invoices.post", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == invoice.BranchId))
            return Results.Forbid();
        if (invoice.Status == "Posted" && invoice.VoucherId is { } alreadyPosted)
            return Results.Ok(new { invoice.InvoiceId, invoice.Number, invoice.Status, voucherId = alreadyPosted, idempotent = true });
        if (invoice.Version != request.Version || invoice.Status != "Draft")
            return Results.Conflict(new { detail = "The invoice changed or is no longer a draft." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A posting confirmation reason is required.");
        try
        {
            M10Rules.RequirePostableInvoice(invoice.Status, invoice.InvoiceType, invoice.DisputeHold,
                invoice.TotalAmount, invoice.TaxableAmount, invoice.CgstAmount, invoice.SgstAmount,
                invoice.IgstAmount, invoice.TcsAmount);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { detail = ex.Message });
        }
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            await db.Entry(invoice).ReloadAsync(ct);
            if (invoice.Status == "Posted" && invoice.VoucherId is { } concurrentPost)
                return Results.Ok(new { invoice.InvoiceId, invoice.Number, invoice.Status, voucherId = concurrentPost, idempotent = true });
            if (invoice.Version != request.Version || invoice.Status != "Draft")
                return Results.Conflict(new { detail = "The invoice changed during posting; reload and retry." });

            var links = await db.Set<M10InvoiceChallan>().Where(x => x.InvoiceId == invoice.InvoiceId).ToListAsync(ct);
            if (links.Count == 0)
                throw new ValidationException("An invoice must contain at least one verified delivery challan.");
            var challanIds = links.Select(x => x.ChallanId).ToArray();
            var challans = await db.Set<M09DeliveryChallan>().Where(x => challanIds.Contains(x.ChallanId)).ToListAsync(ct);
            if (challans.Count != links.Count || challans.Any(x => x.Status != "Delivered" || x.EvidenceState != "Verified" ||
                    x.ReceiverVerifiedAt is null || string.IsNullOrWhiteSpace(x.ReceiverName) ||
                    (string.IsNullOrWhiteSpace(x.SignatureReference) && string.IsNullOrWhiteSpace(x.PhotoReference)) ||
                    x.InvoiceId is not null && x.InvoiceId != invoice.InvoiceId))
                throw new ValidationException("Every invoice challan must retain verified receiver evidence and be unbilled.");
            var tripIds = challans.Select(x => x.TripId).Distinct().ToArray();
            var persistedTrips = await db.Set<M08Trip>().Where(x => tripIds.Contains(x.TripId)).ToListAsync(ct);
            if (persistedTrips.Count != tripIds.Length || challans.Any(challan =>
                    !persistedTrips.Any(trip => M10Rules.IsInvoiceEligible(challan, trip, invoice.InvoiceId))))
                throw new ValidationException("The persisted M08 delivery and M09 challan must retain matching completed receiver-verification facts.");

            var lines = await db.Set<M10InvoiceLine>().Where(x => x.InvoiceId == invoice.InvoiceId).ToListAsync(ct);
            if (lines.Count == 0 || lines.Any(x => x.Quantity <= 0 || x.TaxableAmount < 0 || x.TotalAmount < 0))
                throw new ValidationException("Invoice lines must contain positive delivered quantities and non-negative amounts.");
            var orderLineIds = lines.Select(x => x.SalesOrderLineId).Distinct().ToArray();
            var orderLines = await db.Set<M06SalesOrderLine>().Where(x => orderLineIds.Contains(x.LineId)).ToListAsync(ct);
            if (orderLines.Count != orderLineIds.Length)
                throw new ValidationException("An invoice source sales-order line is missing.");
            foreach (var line in lines)
            {
                var source = orderLines.Single(x => x.LineId == line.SalesOrderLineId);
                if (source.DeliveredQuantity - source.InvoicedQuantity < line.Quantity)
                    throw new ValidationException("Invoice quantity exceeds the unbilled delivered quantity.");
                source.InvoicedQuantity += line.Quantity;
            }

            var setup = OnboardingStore.Decode(await db.Set<Tenant>().AsNoTracking().SingleAsync(ct));
            var required = new List<string> { M12MappingCodes.Receivables };
            if (invoice.TaxableAmount > 0) required.Add(M12MappingCodes.Revenue);
            if (invoice.CgstAmount != 0) required.Add(M12MappingCodes.OutputCgst);
            if (invoice.SgstAmount != 0) required.Add(M12MappingCodes.OutputSgst);
            if (invoice.IgstAmount != 0) required.Add(M12MappingCodes.OutputIgst);
            if (invoice.TcsAmount != 0) required.Add(M12MappingCodes.TcsPayable);
            if (invoice.RoundOffAmount != 0) required.Add(M12MappingCodes.RoundOff);
            var mapped = await M12LedgerMappingResolver.Resolve(db, setup, invoice.BranchId,
                invoice.BranchGstinSnapshot, invoice.InvoiceDate, required, ct);

            var fyStart = invoice.InvoiceDate.Year - (invoice.InvoiceDate.Month < (setup.StartMonth ?? 4) ? 1 : 0);
            var series = setup.Series.SingleOrDefault(x => x.BranchId == invoice.BranchId &&
                x.Document.Equals("Invoice", StringComparison.OrdinalIgnoreCase))
                ?? throw new ValidationException("Configure an Invoice numbering series for this branch before posting.");
            var sequence = await db.Database.SqlQuery<long>($"""
                INSERT INTO m09_challan_number_counter
                    ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart", "NextNumber")
                VALUES ({tenant.TenantId}, {invoice.BranchId}, {"Invoice"}, {fyStart}, 2)
                ON CONFLICT ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart")
                DO UPDATE SET "NextNumber" = m09_challan_number_counter."NextNumber" + 1
                RETURNING "NextNumber" - 1 AS "Value"
                """).SingleAsync(ct);
            invoice.Number = SetupRules.Number(series, setup.StartMonth ?? 4, invoice.InvoiceDate, sequence);

            var journal = new List<M12LineDraft>
            {
                new(mapped[M12MappingCodes.Receivables], invoice.CustomerPartyId, invoice.BranchId, invoice.SiteId,
                    null, null, $"Invoice {invoice.Number} receivable", invoice.TotalAmount, 0)
            };
            void Credit(string code, decimal amount, string label)
            {
                if (amount != 0) journal.Add(new M12LineDraft(mapped[code], null, invoice.BranchId,
                    invoice.SiteId, null, null, $"Invoice {invoice.Number} {label}", 0, amount));
            }
            Credit(M12MappingCodes.Revenue, invoice.TaxableAmount, "revenue");
            Credit(M12MappingCodes.OutputCgst, invoice.CgstAmount, "output CGST");
            Credit(M12MappingCodes.OutputSgst, invoice.SgstAmount, "output SGST");
            Credit(M12MappingCodes.OutputIgst, invoice.IgstAmount, "output IGST");
            Credit(M12MappingCodes.TcsPayable, invoice.TcsAmount, "TCS payable");
            if (invoice.RoundOffAmount > 0)
                journal.Add(new M12LineDraft(mapped[M12MappingCodes.RoundOff], null, invoice.BranchId,
                    invoice.SiteId, null, null, $"Invoice {invoice.Number} round off", 0, invoice.RoundOffAmount));
            else if (invoice.RoundOffAmount < 0)
                journal.Add(new M12LineDraft(mapped[M12MappingCodes.RoundOff], null, invoice.BranchId,
                    invoice.SiteId, null, null, $"Invoice {invoice.Number} round off", -invoice.RoundOffAmount, 0));

            var now = DateTimeOffset.UtcNow;
            var total = await db.Set<M12FinancialPeriod>().AsNoTracking().AnyAsync(x =>
                invoice.InvoiceDate >= x.PeriodStart && invoice.InvoiceDate <= x.PeriodEnd && x.IsLocked, ct);
            if (total)
                throw new ValidationException("The invoice date falls in a locked accounting period.");
            var customer = await db.Set<M03Party>().AsNoTracking().SingleAsync(x => x.PartyId == invoice.CustomerPartyId, ct);
            invoice.DueDate = invoice.InvoiceDate.AddDays(Math.Max(0, customer.CreditDays));
            invoice.Status = "Posting";
            invoice.Version++;
            var posted = await posting.PostAsync(new M12PostCommand(
                tenant.TenantId, actor.UserId, invoice.BranchId, invoice.InvoiceDate,
                "Sales", "M10", invoice.InvoiceId, $"M10:{invoice.InvoiceId}:post",
                $"Sales invoice {invoice.Number}", journal, false), ct);
            invoice.VoucherId = posted.VoucherId;
            invoice.Status = "Posted";
            invoice.PostedAt = now;
            invoice.PostedByUserId = actor.UserId;
            invoice.PostingSnapshotJson = JsonSerializer.Serialize(new
            {
                posted.VoucherId, posted.Number, posted.TotalDebit, posted.TotalCredit,
                reason = request.Reason.Trim(), mappingCodes = required
            }, Json);
            foreach (var challan in challans)
            {
                challan.InvoiceId = invoice.InvoiceId;
                challan.Version++;
                db.Add(new M09ChallanEvent
                {
                    TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ChallanId = challan.ChallanId,
                    ActorUserId = actor.UserId, EventType = "Invoiced",
                    DetailJson = JsonSerializer.Serialize(new { invoice.InvoiceId, invoice.Number }, Json), OccurredAt = now
                });
            }
            db.Add(new M10InvoiceEvent
            {
                TenantId = tenant.TenantId, EventId = Guid.NewGuid(), InvoiceId = invoice.InvoiceId,
                ActorUserId = actor.UserId, EventType = "Posted",
                DetailJson = JsonSerializer.Serialize(new { request.Reason, posted.VoucherId, posted.Number }, Json), OccurredAt = now
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { invoice.InvoiceId, invoice.Number, invoice.Status, voucherId = posted.VoucherId, voucherNumber = posted.Number, invoice.DueDate });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The invoice, delivery or order changed concurrently; no posting was committed." });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "A concurrent posting or numbering conflict occurred; no posting was committed." });
        }
    }
    private static async Task<IResult> CancelPostedInvoice(
        Guid invoiceId, M10InvoiceActionRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, IM12PostingEngine posting,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M10", "m10.invoices.post", ct);
        var invoice = await db.Set<M10Invoice>().SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
        if (invoice is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m10.invoices.post", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == invoice.BranchId))
            return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
            throw new ValidationException("A cancellation reason of 1–1000 characters is required.");
        if (invoice.Version != request.Version)
            return Results.Conflict(new { detail = "Invoice changed. Reload before cancelling." });

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);
        try
        {
            await db.Entry(invoice).ReloadAsync(ct);
            if (invoice.Version != request.Version)
                return Results.Conflict(new { detail = "Invoice changed during cancellation. Reload and retry." });

            var hasAllocations = await db.Set<M11ReceiptAllocation>().AsNoTracking()
                .AnyAsync(x => x.InvoiceId == invoiceId, ct);
            try
            {
                M10Rules.RequireCancellablePostedInvoice(
                    invoice.Status, invoice.VoucherId is not null, hasAllocations,
                    !string.IsNullOrWhiteSpace(invoice.Irn));
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { detail = ex.Message });
            }

            if (invoice.VoucherId is not { } originalVoucherId)
                return Results.Conflict(new { detail = "The posted invoice has no linked accounting voucher." });
            var originalVoucher = await db.Set<M12Voucher>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.VoucherId == originalVoucherId &&
                                           x.Status == "Posted" && x.SourceModule == "M10" &&
                                           x.SourceEntityId == invoiceId, ct);
            if (originalVoucher is null)
                return Results.Conflict(new { detail = "The linked M12 voucher is missing or does not belong to this invoice." });

            var invoiceLinks = await db.Set<M10InvoiceChallan>()
                .Where(x => x.InvoiceId == invoiceId && x.ReleasedAt == null)
                .ToListAsync(ct);
            if (invoiceLinks.Count == 0)
                return Results.Conflict(new { detail = "No active delivery-challan reservations were found for this invoice." });
            var challanIds = invoiceLinks.Select(x => x.ChallanId).ToArray();
            var challans = await db.Set<M09DeliveryChallan>()
                .Where(x => challanIds.Contains(x.ChallanId)).ToListAsync(ct);
            if (challans.Count != invoiceLinks.Count ||
                challans.Any(x => x.InvoiceId != invoiceId || x.Status != "Delivered"))
                return Results.Conflict(new { detail = "The linked delivery challans changed; cancellation was not applied." });

            var invoiceLines = await db.Set<M10InvoiceLine>()
                .Where(x => x.InvoiceId == invoiceId).ToListAsync(ct);
            if (invoiceLines.Count == 0)
                return Results.Conflict(new { detail = "The posted invoice has no source lines; cancellation cannot restore delivered quantities safely." });
            var lineQuantities = invoiceLines.GroupBy(x => x.SalesOrderLineId)
                .Select(group => new { LineId = group.Key, Quantity = group.Sum(x => x.Quantity) })
                .ToArray();
            var orderLineIds = lineQuantities.Select(x => x.LineId).ToArray();
            var orderLines = await db.Set<M06SalesOrderLine>()
                .Where(x => orderLineIds.Contains(x.LineId)).ToListAsync(ct);
            if (orderLines.Count != orderLineIds.Length ||
                lineQuantities.Any(quantity =>
                {
                    var source = orderLines.SingleOrDefault(x => x.LineId == quantity.LineId);
                    return source is null || source.InvoicedQuantity < quantity.Quantity;
                }))
                return Results.Conflict(new { detail = "Order invoiced quantities no longer reconcile to this invoice." });

            var originalLines = await db.Set<M12VoucherLine>().AsNoTracking()
                .Where(x => x.VoucherId == originalVoucherId)
                .OrderBy(x => x.VoucherLineId).ToListAsync(ct);
            IReadOnlyList<M12LineDraft> reversalLines;
            try
            {
                reversalLines = M12Rules.ReversalLines(originalLines);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { detail = $"The source voucher cannot be reversed safely: {ex.Message}" });
            }

            var now = DateTimeOffset.UtcNow;
            var reversed = await posting.PostAsync(new M12PostCommand(
                tenant.TenantId, actor.UserId, invoice.BranchId,
                DateOnly.FromDateTime(now.UtcDateTime), "Sales", "M10", invoiceId,
                $"M10:{invoiceId}:cancel", $"Cancellation of sales invoice {invoice.Number}: {request.Reason.Trim()}",
                reversalLines, false, originalVoucherId), ct);

            foreach (var link in invoiceLinks) link.ReleasedAt = now;
            foreach (var challan in challans)
            {
                challan.InvoiceId = null;
                challan.Version++;
                db.Add(new M09ChallanEvent
                {
                    TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ChallanId = challan.ChallanId,
                    ActorUserId = actor.UserId, EventType = "Invoice cancellation released",
                    DetailJson = JsonSerializer.Serialize(new
                    {
                        invoiceId, invoice.Number, reversalVoucherId = reversed.VoucherId
                    }, Json),
                    OccurredAt = now
                });
            }
            foreach (var quantity in lineQuantities)
                orderLines.Single(x => x.LineId == quantity.LineId).InvoicedQuantity -= quantity.Quantity;

            invoice.Status = "Cancelled";
            invoice.Version++;
            db.Add(new M10InvoiceEvent
            {
                TenantId = tenant.TenantId, EventId = Guid.NewGuid(), InvoiceId = invoiceId,
                ActorUserId = actor.UserId, EventType = "Posted invoice cancelled",
                DetailJson = JsonSerializer.Serialize(new
                {
                    reason = request.Reason.Trim(), originalVoucherId,
                    reversalVoucherId = reversed.VoucherId, reversalVoucherNumber = reversed.Number,
                    releasedChallanCount = challans.Count
                }, Json),
                OccurredAt = now
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new
            {
                invoice.InvoiceId, invoice.Status, originalVoucherId,
                reversalVoucherId = reversed.VoucherId, reversalVoucherNumber = reversed.Number,
                releasedChallanCount = challans.Count
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "Invoice, challan or order data changed concurrently; cancellation was not committed." });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "A concurrent cancellation or accounting conflict occurred; cancellation was not committed." });
        }
    }

    private static IQueryable<M09DeliveryChallan> ApplyBranchScope(
        IQueryable<M09DeliveryChallan> query, IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        return query.Where(x => branches.Contains(x.BranchId));
    }

    private static IQueryable<M10Invoice> ApplyBranchScope(
        IQueryable<M10Invoice> query, IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        return query.Where(x => branches.Contains(x.BranchId));
    }
}
