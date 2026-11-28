using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Berynx.Api.Accounting;
using Berynx.Application.Accounting;
using Berynx.Domain.Accounting;
using Berynx.Api.Onboarding;
using Berynx.Application.Receivables;
using Berynx.Application.Tenancy;
using Berynx.Domain.Invoicing;
using Berynx.Domain.Masters;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Receivables;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Receivables;

public static class M11Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] PaymentMethods =
        ["Cash", "Bank transfer", "UPI", "Cheque", "Post-dated cheque", "Adjustment"];

    public static void MapM11(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/m11")
            .RequireAuthorization(policy => policy.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            await authorization.RequireModule("M11",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                cancellationToken: context.HttpContext.RequestAborted);
            return await next(context);
        });
        api.MapGet("/receipt-options", ReceiptOptions);
        api.MapGet("/customers/{customerId:guid}/open-invoices", OpenInvoices);
        api.MapPost("/receipts/drafts", CreateReceiptDraft);
        api.MapGet("/receipts", ListReceipts);
        api.MapPost("/receipts/{receiptId:guid}/post", PostReceipt);
        api.MapPost("/promises", CreatePromise);
    }

    private static async Task<IResult> ReceiptOptions(
        TenantAuthorizationService authorization, OnboardingStore onboarding,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M11", "m11.receipts.create", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m11.receipts.create", ct);
        var setup = OnboardingStore.Decode(await onboarding.Current());
        var branches = setup.Branches
            .Where(branch => scopes.Any(scope => scope.ScopeType == "All" ||
                scope.ScopeType == "Branch" && scope.ScopeId == branch.Id))
            .Select(branch => new { branch.Id, branch.Name, branch.Gstin })
            .ToArray();
        var customers = await (
            from party in db.Set<M03Party>().AsNoTracking()
            join role in db.Set<M03PartyRole>().AsNoTracking()
                on new { party.TenantId, party.PartyId } equals new { role.TenantId, role.PartyId }
            where party.IsActive && role.RoleCode == "Customer"
            orderby party.TradeName ?? party.LegalName
            select new
            {
                party.PartyId,
                party.LegalName,
                party.TradeName,
                party.Gstin
            }).Take(500).ToListAsync(ct);
        return Results.Ok(new { branches, customers });
    }

    private static async Task<IResult> OpenInvoices(
        Guid customerId, Guid? branchId, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M11", "m11.receipts.create", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m11.receipts.create", ct);
        var query = M11OpenInvoiceQuery.Build(db, customerId, branchId, scopes);
        var invoices = await query.Select(x => new
        {
            x.InvoiceId, x.Number, x.InvoiceDate, x.DueDate, x.BranchId,
            x.CustomerPartyId, x.CustomerNameSnapshot, x.TotalAmount
        }).ToListAsync(ct);
        var invoiceIds = invoices.Select(x => x.InvoiceId).ToArray();
        var allocations = await PostedReceiptAllocationQuery.Build(db)
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, ct);
        var result = invoices.Select(x => new
        {
            x.InvoiceId, x.Number, x.InvoiceDate, x.DueDate, x.BranchId,
            x.CustomerNameSnapshot, x.TotalAmount,
            outstanding = x.TotalAmount - allocations.GetValueOrDefault(x.InvoiceId)
        }).Where(x => x.outstanding > 0).OrderBy(x => x.InvoiceDate).ToArray();
        return Results.Ok(new { items = result });
    }

    private static async Task<IResult> CreateReceiptDraft(
        M11ReceiptDraftRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M11", "m11.receipts.create", ct);
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100)
            throw new ValidationException("An idempotency key of 1–100 characters is required.");
        if (request.Allocations is null || request.Allocations.Any(x => x.Amount <= 0))
            throw new ValidationException("Receipt allocations must be positive.");
        if (request.Allocations.Select(x => x.InvoiceId).Distinct().Count() != request.Allocations.Length)
            throw new ValidationException("Each invoice may appear only once in a receipt allocation.");
        if (!PaymentMethods.Contains(request.PaymentMethod, StringComparer.Ordinal))
            throw new ValidationException("Select a supported payment method.");
        if (request.Amount <= 0 || request.TdsAmount < 0 || request.BankCharges < 0 ||
            request.TdsAmount + request.BankCharges > request.Amount)
            throw new ValidationException("Receipt, TDS and bank-charge amounts are invalid.");
        var scopes = await authorization.Scopes(actor.UserId, "m11.receipts.create", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == request.BranchId))
            return Results.Forbid();
        var previous = await db.Set<M11Receipt>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (previous is not null)
        {
            var priorAllocations = await db.Set<M11ReceiptAllocation>().AsNoTracking()
                .Where(x => x.ReceiptId == previous.ReceiptId)
                .Select(x => new { x.InvoiceId, x.Amount }).ToListAsync(ct);
            var requestedAllocations = request.Allocations
                .Select(x => new { x.InvoiceId, x.Amount }).OrderBy(x => x.InvoiceId).ToArray();
            if (previous.CustomerPartyId != request.CustomerPartyId || previous.Amount != request.Amount ||
                previous.TdsAmount != request.TdsAmount || previous.BankCharges != request.BankCharges ||
                previous.BranchId != request.BranchId || previous.ReceiptDate != request.ReceiptDate ||
                previous.PaymentMethod != request.PaymentMethod ||
                !string.Equals(previous.ReferenceNumber, request.ReferenceNumber?.Trim() ?? "", StringComparison.Ordinal) ||
                !string.Equals(previous.Notes, request.Notes?.Trim() ?? "", StringComparison.Ordinal) ||
                !priorAllocations.OrderBy(x => x.InvoiceId).SequenceEqual(requestedAllocations))
                return Results.Conflict(new { detail = "Idempotency key was used for another receipt draft." });
            return Results.Created($"/api/v1/m11/receipts/{previous.ReceiptId}",
                new { previous.ReceiptId, previous.Status, duplicate = true });
        }
        var tenantRow = await db.Set<Tenant>().AsNoTracking().SingleAsync(ct);
        var setup = OnboardingStore.Decode(tenantRow);
        if (!setup.Branches.Any(x => x.Id == request.BranchId))
            return Results.Conflict(new { detail = "Receipt branch is not configured for this tenant." });
        var customer = await db.Set<M03Party>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.PartyId == request.CustomerPartyId, ct);
        if (customer is null) return Results.NotFound(new { detail = "Customer not found." });
        if (!await db.Set<M03PartyRole>().AnyAsync(x =>
                x.PartyId == request.CustomerPartyId && x.RoleCode == "Customer", ct))
            return Results.Conflict(new { detail = "Select an active M03 customer." });

        var allocated = M11Rules.ValidateAndGetAdvance(request.Amount, request.Allocations.Select(x => x.Amount));
        var invoices = await db.Set<M10Invoice>().AsNoTracking()
            .Where(x => request.Allocations.Select(a => a.InvoiceId).Contains(x.InvoiceId)).ToDictionaryAsync(x => x.InvoiceId, ct);
        if (invoices.Count != request.Allocations.Select(x => x.InvoiceId).Distinct().Count())
            return Results.NotFound(new { detail = "One or more invoices were not found in this tenant." });
        foreach (var allocation in request.Allocations)
        {
            var invoice = invoices[allocation.InvoiceId];
            if (invoice.CustomerPartyId != request.CustomerPartyId || invoice.BranchId != request.BranchId)
                return Results.Conflict(new { detail = "Receipt allocations must match customer and branch." });
            var priorPosted = await (from link in db.Set<M11ReceiptAllocation>().AsNoTracking()
                                     join postedReceipt in db.Set<M11Receipt>().AsNoTracking()
                                         on new { link.TenantId, link.ReceiptId } equals new { postedReceipt.TenantId, postedReceipt.ReceiptId }
                                     where link.InvoiceId == invoice.InvoiceId && postedReceipt.Status == "Posted"
                                     select link.Amount).SumAsync(ct);
            try
            {
                M11Rules.RequirePostedInvoice(invoice.Status, invoice.TotalAmount - priorPosted, allocation.Amount);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { detail = ex.Message, invoiceId = invoice.InvoiceId });
            }
        }
        var receipt = new M11Receipt
        {
            TenantId = tenant.TenantId,
            ReceiptId = Guid.NewGuid(),
            CustomerPartyId = request.CustomerPartyId,
            BranchId = request.BranchId,
            ReceiptDate = request.ReceiptDate,
            Status = "Draft",
            PaymentMethod = request.PaymentMethod,
            ReferenceNumber = request.ReferenceNumber?.Trim() ?? "",
            Amount = request.Amount,
            TdsAmount = request.TdsAmount,
            BankCharges = request.BankCharges,
            AllocatedAmount = request.Allocations.Sum(x => x.Amount),
            AdvanceAmount = allocated,
            Notes = request.Notes?.Trim() ?? "",
            IdempotencyKey = request.IdempotencyKey,
            CreatedByUserId = actor.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Add(receipt);
        foreach (var allocation in request.Allocations)
            db.Add(new M11ReceiptAllocation
            {
                TenantId = tenant.TenantId, AllocationId = Guid.NewGuid(),
                ReceiptId = receipt.ReceiptId, InvoiceId = allocation.InvoiceId, Amount = allocation.Amount
            });
        db.Add(new M11ReceivableEvent
        {
            TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ReceiptId = receipt.ReceiptId,
            ActorUserId = actor.UserId, EventType = "Receipt draft created",
            DetailJson = JsonSerializer.Serialize(new
            {
                receipt.Amount, receipt.AllocatedAmount, receipt.AdvanceAmount,
                request.TdsAmount, request.BankCharges, request.Allocations
            }, Json),
            OccurredAt = DateTimeOffset.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "Receipt draft was submitted concurrently; reload before retrying." });
        }
        return Results.Created($"/api/v1/m11/receipts/{receipt.ReceiptId}", new
        {
            receipt.ReceiptId, receipt.Status, receipt.Amount, receipt.AllocatedAmount,
            receipt.AdvanceAmount
        });
    }

    private static async Task<IResult> ListReceipts(
        Guid? customerId, string? status, TenantAuthorizationService authorization,
        BerynxDbContext db, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M11", "m11.receipts.view", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m11.receipts.view", ct);
        var query = db.Set<M11Receipt>().AsNoTracking().AsQueryable();
        if (customerId is { } customer) query = query.Where(x => x.CustomerPartyId == customer);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        query = ApplyBranchScope(query, scopes);
        return Results.Ok(await query.OrderByDescending(x => x.ReceiptDate)
            .Select(x => new
            {
                x.ReceiptId, x.Number, x.ReceiptDate, x.Status, x.PaymentMethod,
                x.ReferenceNumber, x.Amount, x.AllocatedAmount, x.AdvanceAmount, x.Version
            }).ToListAsync(ct));
    }

    private static async Task<IResult> PostReceipt(
        Guid receiptId, M11ReceiptPostRequest request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, IM12PostingEngine posting,
        CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M11", "m11.receipts.post", ct);
        var receipt = await db.Set<M11Receipt>().SingleOrDefaultAsync(x => x.ReceiptId == receiptId, ct);
        if (receipt is null) return Results.NotFound();
        var scopes = await authorization.Scopes(actor.UserId, "m11.receipts.post", ct);
        if (!scopes.Any(x => x.ScopeType == "All" || x.ScopeType == "Branch" && x.ScopeId == receipt.BranchId))
            return Results.Forbid();
        if (receipt.Status == "Posted" && receipt.VoucherId is { } voucherId)
            return Results.Ok(new { receipt.ReceiptId, receipt.Number, receipt.Status, voucherId, idempotent = true });
        if (receipt.Version != request.Version || receipt.Status != "Draft")
            return Results.Conflict(new { detail = "The receipt changed or is no longer a draft." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException("A posting confirmation reason is required.");
        try
        {
            M11Rules.RequirePostableReceipt(receipt.PaymentMethod, receipt.Amount, receipt.TdsAmount,
                receipt.BankCharges, receipt.AllocatedAmount, receipt.AdvanceAmount);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { detail = ex.Message });
        }
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try
        {
            await db.Entry(receipt).ReloadAsync(ct);
            if (receipt.Status == "Posted" && receipt.VoucherId is { } concurrentlyPosted)
                return Results.Ok(new { receipt.ReceiptId, receipt.Number, receipt.Status, voucherId = concurrentlyPosted, idempotent = true });
            if (receipt.Version != request.Version || receipt.Status != "Draft")
                return Results.Conflict(new { detail = "The receipt changed during posting; reload and retry." });

            var links = await db.Set<M11ReceiptAllocation>().Where(x => x.ReceiptId == receipt.ReceiptId).ToListAsync(ct);
            if (links.Sum(x => x.Amount) != receipt.AllocatedAmount)
                throw new ValidationException("Receipt allocation rows do not match the saved allocated total.");
            var invoiceIds = links.Select(x => x.InvoiceId).Distinct().ToArray();
            var invoices = await db.Set<M10Invoice>().Where(x => invoiceIds.Contains(x.InvoiceId)).ToListAsync(ct);
            if (invoices.Count != invoiceIds.Length)
                throw new ValidationException("A receipt allocation references a missing invoice.");
            var invoicesById = invoices.ToDictionary(x => x.InvoiceId);
            foreach (var allocation in links)
            {
                var invoice = invoicesById[allocation.InvoiceId];
                if (invoice.Status != "Posted" || invoice.CustomerPartyId != receipt.CustomerPartyId || invoice.BranchId != receipt.BranchId)
                    throw new ValidationException("Receipt allocations must reference a posted invoice for the same customer and branch.");
                var prior = await (from link in db.Set<M11ReceiptAllocation>()
                                   join priorReceipt in db.Set<M11Receipt>()
                                       on new { link.TenantId, link.ReceiptId } equals new { priorReceipt.TenantId, priorReceipt.ReceiptId }
                                   where link.InvoiceId == invoice.InvoiceId && priorReceipt.Status == "Posted"
                                   select link.Amount).SumAsync(ct);
                M11Rules.RequirePostedInvoice(invoice.Status, invoice.TotalAmount - prior, allocation.Amount);
            }

            var setup = OnboardingStore.Decode(await db.Set<Tenant>().AsNoTracking().SingleAsync(ct));
            var branch = setup.Branches.SingleOrDefault(x => x.Id == receipt.BranchId);
            if (branch is null || string.IsNullOrWhiteSpace(branch.Gstin))
                throw new ValidationException("Configure a branch GSTIN before posting the receipt.");
            var cashMap = receipt.PaymentMethod switch
            {
                "Cash" => M12MappingCodes.ReceiptCash,
                "Bank transfer" => M12MappingCodes.ReceiptBank,
                "UPI" => M12MappingCodes.ReceiptUpi,
                _ => throw new ValidationException("Unsupported receipt payment method.")
            };
            var required = new List<string>();
            if (receipt.Amount - receipt.TdsAmount - receipt.BankCharges > 0) required.Add(cashMap);
            if (receipt.TdsAmount > 0) required.Add(M12MappingCodes.TdsReceivable);
            if (receipt.BankCharges > 0) required.Add(M12MappingCodes.BankCharges);
            if (receipt.AllocatedAmount > 0) required.Add(M12MappingCodes.Receivables);
            if (receipt.AdvanceAmount > 0) required.Add(M12MappingCodes.CustomerAdvances);
            var mapped = await M12LedgerMappingResolver.Resolve(db, setup, receipt.BranchId,
                branch.Gstin, receipt.ReceiptDate, required, ct);
            var series = setup.Series.SingleOrDefault(x => x.BranchId == receipt.BranchId &&
                x.Document.Equals("Receipt", StringComparison.OrdinalIgnoreCase))
                ?? throw new ValidationException("Configure a Receipt numbering series for this branch before posting.");
            var fyStart = receipt.ReceiptDate.Year - (receipt.ReceiptDate.Month < (setup.StartMonth ?? 4) ? 1 : 0);
            var sequence = await db.Database.SqlQuery<long>($"""
                INSERT INTO m09_challan_number_counter
                    ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart", "NextNumber")
                VALUES ({tenant.TenantId}, {receipt.BranchId}, {"Receipt"}, {fyStart}, 2)
                ON CONFLICT ("TenantId", "BranchId", "DocumentCode", "FinancialYearStart")
                DO UPDATE SET "NextNumber" = m09_challan_number_counter."NextNumber" + 1
                RETURNING "NextNumber" - 1 AS "Value"
                """).SingleAsync(ct);
            receipt.Number = SetupRules.Number(series, setup.StartMonth ?? 4, receipt.ReceiptDate, sequence);

            var journal = new List<M12LineDraft>();
            void Debit(string code, decimal amount, string label)
            {
                if (amount != 0) journal.Add(new M12LineDraft(mapped[code], null, receipt.BranchId,
                    null, null, null, $"Receipt {receipt.Number} {label}", amount, 0));
            }
            void Credit(string code, Guid? partyId, decimal amount, string label)
            {
                if (amount != 0) journal.Add(new M12LineDraft(mapped[code], partyId, receipt.BranchId,
                    null, null, null, $"Receipt {receipt.Number} {label}", 0, amount));
            }
            Debit(cashMap, receipt.Amount - receipt.TdsAmount - receipt.BankCharges, "cash/bank settlement");
            Debit(M12MappingCodes.TdsReceivable, receipt.TdsAmount, "TDS receivable");
            Debit(M12MappingCodes.BankCharges, receipt.BankCharges, "bank charges");
            Credit(M12MappingCodes.Receivables, receipt.CustomerPartyId, receipt.AllocatedAmount, "invoice allocation");
            Credit(M12MappingCodes.CustomerAdvances, receipt.CustomerPartyId, receipt.AdvanceAmount, "customer advance");
            if (journal.Count < 2)
                throw new ValidationException("Receipt posting must contain balanced debit and credit legs.");

            var now = DateTimeOffset.UtcNow;
            receipt.Status = "Posting";
            receipt.Version++;
            foreach (var invoice in invoices) invoice.Version++;
            var posted = await posting.PostAsync(new M12PostCommand(
                tenant.TenantId, actor.UserId, receipt.BranchId, receipt.ReceiptDate,
                "Receipt", "M11", receipt.ReceiptId, $"M11:{receipt.ReceiptId}:post",
                $"Customer receipt {receipt.Number}", journal, false), ct);
            receipt.VoucherId = posted.VoucherId;
            receipt.Status = "Posted";
            receipt.PostedAt = now;
            receipt.PostedByUserId = actor.UserId;
            db.Add(new M11ReceivableEvent
            {
                TenantId = tenant.TenantId, EventId = Guid.NewGuid(), ReceiptId = receipt.ReceiptId,
                ActorUserId = actor.UserId, EventType = "Receipt posted",
                DetailJson = JsonSerializer.Serialize(new
                {
                    posted.VoucherId, posted.Number, posted.TotalDebit, posted.TotalCredit,
                    reason = request.Reason.Trim(), mappings = required, allocationCount = links.Count
                }, Json), OccurredAt = now
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new { receipt.ReceiptId, receipt.Number, receipt.Status, voucherId = posted.VoucherId, voucherNumber = posted.Number });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { detail = "The receipt or invoice balance changed concurrently; no posting was committed." });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { detail = "A concurrent receipt posting or numbering conflict occurred; no posting was committed." });
        }
    }
    private static async Task<IResult> CreatePromise(
        M11PromiseWrite request, TenantAuthorizationService authorization,
        BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct)
    {
        var actor = await authorization.CurrentUser(ct);
        await authorization.Require("M11", "m11.collections.manage", ct);
        var scopes = await authorization.Scopes(actor.UserId, "m11.collections.manage", ct);
        if (!scopes.Any(x => x.ScopeType == "All" ||
                             x.ScopeType == "Branch" && x.ScopeId == request.BranchId))
            return Results.Forbid();
        if (request.Amount <= 0 || request.PromiseDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ValidationException("Promise date and amount must be valid.");
        var customer = await db.Set<M03Party>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.PartyId == request.CustomerPartyId, ct);
        if (customer is null) return Results.NotFound();
        if (request.InvoiceId is { } invoiceId)
        {
            var invoice = await db.Set<M10Invoice>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, ct);
            if (invoice is null || invoice.CustomerPartyId != request.CustomerPartyId ||
                invoice.BranchId != request.BranchId || invoice.Status != "Posted")
                return Results.Conflict(new { detail = "Promise must reference a posted invoice for this customer." });
        }
        var promise = new M11PromiseToPay
        {
            TenantId = tenant.TenantId, PromiseId = Guid.NewGuid(),
            CustomerPartyId = request.CustomerPartyId, BranchId = request.BranchId,
            InvoiceId = request.InvoiceId,
            PromiseDate = request.PromiseDate, Amount = request.Amount,
            Outcome = "Open", Notes = request.Notes?.Trim() ?? "",
            CreatedByUserId = actor.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.Add(promise);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/m11/promises/{promise.PromiseId}", promise);
    }

    private static IQueryable<M10Invoice> ApplyBranchScope(
        IQueryable<M10Invoice> query, IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        return query.Where(x => branches.Contains(x.BranchId));
    }

    private static IQueryable<M11Receipt> ApplyBranchScope(
        IQueryable<M11Receipt> query, IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes)
    {
        if (scopes.Any(x => x.ScopeType == "All")) return query;
        var branches = scopes.Where(x => x.ScopeType == "Branch" && x.ScopeId is not null)
            .Select(x => x.ScopeId!.Value).ToArray();
        return query.Where(x => branches.Contains(x.BranchId));
    }
}
