namespace Berynx.Domain.Tenancy;

/// <summary>
/// Marks persistence entities whose reads and writes must be scoped to a
/// server-authenticated tenant.
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
