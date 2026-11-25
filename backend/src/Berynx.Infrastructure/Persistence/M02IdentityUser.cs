using Microsoft.AspNetCore.Identity;

namespace Berynx.Infrastructure.Persistence;

public sealed class M02IdentityUser : IdentityUser<Guid>
{
    public string? TotpSecretProtected { get; set; }
    public string? TotpPendingSecretProtected { get; set; }
}
