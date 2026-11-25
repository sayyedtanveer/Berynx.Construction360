using System.ComponentModel.DataAnnotations;
using Berynx.Domain.IdentityAccess;
using NUnit.Framework;

namespace Berynx.UnitTests.IdentityAccess;

[TestFixture]
public sealed class M02RulesTests
{
    [Test]
    public void InvitationSupportsOneVerifiedIdentityAndExpiresAfterSeventyTwoHours()
    {
        M02Rules.ValidateInvitation(null, "+919876543210", ["TenantAdmin"]);
        var sentAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.That(M02Rules.InvitationExpiry(sentAt), Is.EqualTo(sentAt.AddHours(72)));
    }

    [TestCase(null, null)]
    [TestCase("not-an-email", null)]
    [TestCase(null, "9876543210")]
    public void InvitationRejectsMissingOrInvalidContact(string? email, string? mobile)
    {
        Assert.Throws<ValidationException>(() => M02Rules.ValidateInvitation(email, mobile, ["TenantAdmin"]));
    }

    [Test]
    public void PermissionGridAllowsOnlySupportedActions()
    {
        M02Rules.ValidatePermission("sales.approve");
        M02Rules.ValidatePermission("inventory.view_cost");
        Assert.Throws<ValidationException>(() => M02Rules.ValidatePermission("sales.delete"));
        Assert.Throws<ValidationException>(() => M02Rules.ValidatePermission("view_cost"));
    }

    [Test]
    public void ScopesRequireIdentifiersOnlyForBranchAndPlant()
    {
        M02Rules.ValidateScope("All", null);
        M02Rules.ValidateScope("Own", null);
        M02Rules.ValidateScope("Branch", Guid.NewGuid());
        Assert.Throws<ValidationException>(() => M02Rules.ValidateScope("Branch", null));
        Assert.Throws<ValidationException>(() => M02Rules.ValidateScope("All", Guid.NewGuid()));
    }

    [Test]
    public void ApprovalConditionsRequireAllConfiguredPredicatesAndUseStrictThresholds()
    {
        var rule = new M02ApprovalRule
        {
            AmountGreaterThan = 1000m,
            DiscountPercentGreaterThan = 5m
        };
        Assert.That(M02Rules.Matches(rule, 1000m, 6m, null, null), Is.False);
        Assert.That(M02Rules.Matches(rule, 1001m, 5m, null, null), Is.False);
        Assert.That(M02Rules.Matches(rule, 1001m, 6m, null, null), Is.True);
    }

    [Test]
    public void ApprovalRequiresOrderedNonSelfDecisionAndRejectionReason()
    {
        var requester = Guid.NewGuid();
        var approver = Guid.NewGuid();
        Assert.Throws<UnauthorizedAccessException>(() =>
            M02Rules.ValidateDecision(requester, requester, "Approve", null));
        Assert.Throws<ValidationException>(() =>
            M02Rules.ValidateDecision(requester, approver, "Reject", " "));
        Assert.Throws<ValidationException>(() =>
            M02Rules.ValidateDecision(requester, approver, "Edit", null));
        Assert.DoesNotThrow(() =>
            M02Rules.ValidateDecision(requester, approver, "Approve", null));
        Assert.DoesNotThrow(() =>
            M02Rules.ValidateDecision(requester, approver, "Reject", "Insufficient documentation"));
    }

    [Test]
    public void DelegationCannotExpandPermissionOrBranchScope()
    {
        Assert.DoesNotThrow(() => M02Rules.ValidateDelegatedApproval(true, true));
        Assert.Throws<UnauthorizedAccessException>(() => M02Rules.ValidateDelegatedApproval(false, true));
        Assert.Throws<UnauthorizedAccessException>(() => M02Rules.ValidateDelegatedApproval(true, false));
    }

    [Test]
    public void DelegationUsesAnExclusiveDateBoundedWindow()
    {
        var now = DateTimeOffset.UtcNow;
        var delegation = new M02Delegation
        {
            StartsAt = now,
            EndsAt = now.AddDays(1)
        };
        Assert.That(M02Rules.IsDelegationActive(delegation, now), Is.True);
        Assert.That(M02Rules.IsDelegationActive(delegation, delegation.EndsAt), Is.False);
        delegation.RevokedAt = now;
        Assert.That(M02Rules.IsDelegationActive(delegation, now), Is.False);
    }
}
