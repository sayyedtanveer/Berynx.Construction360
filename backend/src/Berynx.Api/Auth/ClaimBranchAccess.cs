using Berynx.Application.Tenancy;

namespace Berynx.Api.Auth;
public sealed class ClaimBranchAccess(IHttpContextAccessor http) : IBranchAccess
{
    public void RequireBranch(Guid branchId)
    {
        var user = http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException();
        var scopes = user.FindAll("branch_id").ToList();
        if (scopes.Count > 0 && !scopes.Any(c => Guid.TryParse(c.Value, out var id) && id == branchId))
            throw new UnauthorizedAccessException();
    }
}
