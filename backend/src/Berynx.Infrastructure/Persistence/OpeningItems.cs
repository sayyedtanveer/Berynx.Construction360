using Berynx.Application.Onboarding;
using Berynx.Domain.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Persistence;
public sealed class OpeningItems(BerynxDbContext db) : IOpeningItems
{
    public async Task<IReadOnlyList<OpeningBill>> ForParty(string kind, string party, CancellationToken cancellationToken)
    {
        Workflow.Require(kind is "Customer" or "Supplier", "Choose customer or supplier opening items.");
        return await (
            from row in db.Set<OpeningItem>().AsNoTracking()join tenant in db.Set<Tenant>()on row.TenantId equals tenant.TenantId
            where tenant.LiveAt != null && tenant.Status != "Cancelled" && row.Kind == kind && row.Ledger == party
            orderby row.Bill, row.Id
            select new OpeningBill(row.Id, row.Kind, row.Ledger, row.Bill!, row.Debit, row.Credit)).ToListAsync(cancellationToken);
    }
}
