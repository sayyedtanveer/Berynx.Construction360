using Berynx.Domain.SalesOrders;
using NUnit.Framework;

namespace Berynx.UnitTests.SalesOrders;

[TestFixture]
public sealed class M06RulesTests
{
    [TestCase("Draft", "Pending approval", true)]
    [TestCase("Draft", "Confirmed", true)]
    [TestCase("Confirmed", "Cancelled", true)]
    [TestCase("In fulfilment", "Short-closed", true)]
    [TestCase("Completed", "Cancelled", false)]
    [TestCase("Cancelled", "Confirmed", false)]
    [TestCase("Draft", "Short-closed", false)]
    public void Lifecycle_allows_only_defined_transitions(string from, string to, bool expected) =>
        Assert.That(M06Rules.CanTransition(from, to), Is.EqualTo(expected));

    [TestCase(100, 2, 102)]
    [TestCase(50, 1, 50.5)]
    public void Schedule_limit_applies_configured_overdelivery_tolerance(
        decimal ordered, decimal tolerancePercent, decimal expected) =>
        Assert.That(M06Rules.MaximumScheduleQuantity(ordered, tolerancePercent), Is.EqualTo(expected));

    [Test]
    public void Schedule_limit_rejects_totals_beyond_tolerance() =>
        Assert.Throws<InvalidOperationException>(() => M06Rules.ValidateScheduleTotal(100, 102.001m, 2));
}
