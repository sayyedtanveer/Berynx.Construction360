using System.ComponentModel.DataAnnotations;
using Berynx.Domain.Onboarding;
using NUnit.Framework;

namespace Berynx.UnitTests.Onboarding;

[TestFixture]
public sealed class SaaSLicensingTests
{
    [TestCase("Trial", 0, "Trial")]
    [TestCase("Active", 0, "Active")]
    [TestCase("Active", 2, "Grace")]
    [TestCase("Trial", 2, "Grace")]
    [TestCase("Grace", 11, "Suspended")]
    public void EffectiveLifecycleUsesDatesAndGraceWindow(
        string state, int daysAfterEnd, string expected)
    {
        var now = DateTimeOffset.UtcNow;
        var subscription = new TenantSubscription
        {
            Status = state,
            StartsAt = now.AddDays(-20),
            EndsAt = now.AddDays(1),
            GraceEndsAt = now.AddDays(10)
        };

        Assert.That(
            SubscriptionLifecycle.EffectiveStatus(subscription, now.AddDays(daysAfterEnd)),
            Is.EqualTo(expected));
    }

    [Test]
    public void SuspendedAndCancelledDoNotBecomeActiveBecauseDatesAreExtended()
    {
        var now = DateTimeOffset.UtcNow;
        var subscription = new TenantSubscription
        {
            Status = SubscriptionLifecycle.Cancelled,
            StartsAt = now.AddDays(-10),
            EndsAt = now.AddDays(10),
            GraceEndsAt = now.AddDays(20)
        };

        Assert.That(SubscriptionLifecycle.EffectiveStatus(subscription, now), Is.EqualTo("Cancelled"));
        Assert.Throws<ValidationException>(() =>
            SubscriptionLifecycle.Transition(subscription, SubscriptionLifecycle.Active));
    }

    [Test]
    public void PlanRequiresExplicitPositiveQuotasAndConfigurableTrialTerms()
    {
        var plan = new PlatformPlan
        {
            PlanCode = "launch",
            Name = "Launch",
            MaxUsers = 20,
            MaxStorageBytes = 1_000_000,
            MaxDocumentsPerMonth = 1_000,
            MaxMessagesPerMonth = 500,
            MaxApiRequestsPerMonth = 100_000,
            TrialDays = 14,
            GracePeriodDays = 0,
            Currency = "INR"
        };

        Assert.DoesNotThrow(plan.Validate);
        plan.MaxUsers = 0;
        Assert.Throws<ValidationException>(plan.Validate);
    }
}
