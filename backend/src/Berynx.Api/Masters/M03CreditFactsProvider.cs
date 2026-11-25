using Berynx.Application.Masters;

namespace Berynx.Api.Masters;

/// <summary>
/// M03 has no accounting or sales transaction source. Keep this explicit adapter until
/// a future module registers a provider backed by persisted receivables and orders.
/// </summary>
public sealed class M03UnavailableCreditFactsProvider : IM03CreditFactsProvider
{
    public Task<M03CreditFacts> GetFacts(Guid customerPartyId, CancellationToken cancellationToken) =>
        Task.FromResult(new M03CreditFacts(
            Available: false,
            Outstanding: null,
            Overdue: null,
            OldestOverdueDays: null,
            UnavailableReason: "Not available until accounting and sales transaction modules are activated."));
}
