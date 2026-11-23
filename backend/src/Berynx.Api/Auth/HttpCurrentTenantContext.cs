using System.Security.Claims;
using Berynx.Application.Tenancy;

namespace Berynx.Api.Auth;

/// <summary>
/// Resolves tenant identity only from the authenticated principal established
/// by JWT validation. Request headers, query strings, and route values are not
/// considered trusted tenant selectors.
/// </summary>
public sealed class HttpCurrentTenantContext(
    IHttpContextAccessor httpContextAccessor) : ICurrentTenantContext
{
    public Guid TenantId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var claimValue = principal?.FindFirstValue("tenant_id");

            if (principal?.Identity?.IsAuthenticated != true ||
                !Guid.TryParse(claimValue, out var tenantId) ||
                tenantId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "An authenticated tenant claim is required for tenant-owned data.");
            }

            return tenantId;
        }
    }
}
