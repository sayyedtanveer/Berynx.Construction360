using Berynx.Domain.Onboarding;

namespace Berynx.Domain.IdentityAccess;

public static class M02MfaPolicy
{
    public static bool IsRequired(TenantSecurityPolicy policy, IEnumerable<string> roleNames)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(roleNames);

        static string Normalize(string value) =>
            new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        return roleNames.Any(role =>
        {
            var normalized = Normalize(role);
            var tenantAdmin = normalized is "tenantadmin" or "tenantadministrator" or "admin";
            var accounts = normalized is "accounts" or "accountant" or "accounting";
            return policy.RequireMfaForTenantAdmins && tenantAdmin ||
                   policy.RequireMfaForAccounts && accounts;
        });
    }
}
