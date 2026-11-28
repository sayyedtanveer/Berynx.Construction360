using Berynx.Domain.Receivables;
using Berynx.Domain.Invoicing;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Receivables;

public sealed record PostedReceiptAllocationTotal(Guid InvoiceId, decimal Amount);

/// <summary>
/// Shared relational query for balances derived only from posted receipts.
/// Keeping this query in one place keeps invoice collection filters and open
/// receivables aligned with the same accounting source of truth.
/// </summary>
public static class PostedReceiptAllocationQuery
{
    public static IQueryable<PostedReceiptAllocationTotal> Build(BerynxDbContext db) =>
        from allocation in db.Set<M11ReceiptAllocation>().AsNoTracking()
        join receipt in db.Set<M11Receipt>().AsNoTracking()
            on new { allocation.TenantId, allocation.ReceiptId }
            equals new { receipt.TenantId, receipt.ReceiptId }
        where receipt.Status == "Posted"
        group allocation by allocation.InvoiceId into grouped
        select new PostedReceiptAllocationTotal(grouped.Key, grouped.Sum(x => x.Amount));
}

public static class M11OpenInvoiceQuery
{
    public static IQueryable<M10Invoice> Build(
        BerynxDbContext db,
        Guid customerId,
        Guid? branchId,
        IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes)
    {
        var query = db.Set<M10Invoice>().AsNoTracking()
            .Where(invoice => invoice.CustomerPartyId == customerId && invoice.Status == "Posted");
        if (branchId is { } branch)
            query = query.Where(invoice => invoice.BranchId == branch);

        if (!scopes.Any(scope => scope.ScopeType == "All"))
        {
            var branches = scopes
                .Where(scope => scope.ScopeType == "Branch" && scope.ScopeId is not null)
                .Select(scope => scope.ScopeId!.Value)
                .ToArray();
            query = query.Where(invoice => branches.Contains(invoice.BranchId));
        }

        return query;
    }
}
