using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using NUnit.Framework;

namespace Berynx.UnitTests.IdentityAccess;

[TestFixture]
public sealed class M02MfaPolicyTests
{
    [TestCase("TenantAdmin", true, false, true)]
    [TestCase("Tenant Administrator", true, false, true)]
    [TestCase("Accountant", false, true, true)]
    [TestCase("Accounts", false, true, true)]
    [TestCase("SalesExecutive", true, true, false)]
    [TestCase("Accountant", true, false, false)]
    public void AppliesTenantMfaPolicyOnlyToConfiguredPrivilegedRoles(
        string role, bool requireAdmins, bool requireAccounts, bool expected)
    {
        var policy = new TenantSecurityPolicy
        {
            RequireMfaForTenantAdmins = requireAdmins,
            RequireMfaForAccounts = requireAccounts
        };

        Assert.That(M02MfaPolicy.IsRequired(policy, [role]), Is.EqualTo(expected));
    }

    [Test]
    public void PolicyWithMultipleTenantRolesRequiresMfaIfAnyRoleMatches()
    {
        var policy = new TenantSecurityPolicy { RequireMfaForAccounts = true };

        Assert.That(M02MfaPolicy.IsRequired(policy, ["SalesExecutive", "Accountant"]), Is.True);
    }
}
