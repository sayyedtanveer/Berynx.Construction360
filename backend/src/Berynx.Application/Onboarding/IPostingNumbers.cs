namespace Berynx.Application.Onboarding;
public sealed record PostedNumber(string Number, string Gstin, Guid BranchId, int FiscalYear);
// Invoked by the posting handler inside its database transaction. Draft creation never calls this contract.
public interface IPostingNumbers
{
    Task<PostedNumber> Assign(Guid postingId, Guid branchId, string document, DateOnly date, CancellationToken cancellationToken);
}
