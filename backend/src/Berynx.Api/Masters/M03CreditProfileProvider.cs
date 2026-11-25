using Berynx.Application.Masters;
using Berynx.Domain.Masters;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Masters;

public sealed class M03CreditProfileProvider(BerynxDbContext db) : IM03CreditProfileProvider
{
    public async Task<M03CreditProfile?> GetProfile(
        Guid customerPartyId, CancellationToken cancellationToken)
    {
        var party = await db.Set<M03Party>().AsNoTracking().SingleOrDefaultAsync(
            x => x.PartyId == customerPartyId && x.IsActive, cancellationToken);
        if (party is null || !await db.Set<M03PartyRole>().AsNoTracking().AnyAsync(
                x => x.PartyId == customerPartyId && x.RoleCode == "Customer", cancellationToken))
            return null;

        return new M03CreditProfile(
            party.PartyId, party.CreditLimit, party.CreditDays, party.CreditPolicy, party.CreditStatus);
    }
}
