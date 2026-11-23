namespace Berynx.Application.Tenancy;

/// <summary>
/// Supplies the tenant established by trusted, authenticated server context.
/// Implementations must not derive tenant identity from request parameters.
/// </summary>
public interface ICurrentTenantContext
{
    Guid TenantId { get; }
}
