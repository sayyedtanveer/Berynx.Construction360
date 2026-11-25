namespace Berynx.Application.Onboarding;
public sealed record OpeningBill(Guid Id, string Kind, string Party, string Bill, decimal Debit, decimal Credit);
/// <summary>Immutable opening sources for later tenant-scoped receipt and payment allocation.</summary>
public interface IOpeningItems
{
    Task<IReadOnlyList<OpeningBill>> ForParty(string kind, string party, CancellationToken cancellationToken);
}
