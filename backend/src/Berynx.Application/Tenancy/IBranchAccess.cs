namespace Berynx.Application.Tenancy;
public interface IBranchAccess
{
    void RequireBranch(Guid branchId);
}
