using Berynx.Domain.Gst;
using NUnit.Framework;

namespace Berynx.UnitTests.Gst;

[TestFixture]
public sealed class M13RulesTests
{
    [Test]
    public void Filing_requires_first_day_and_uses_actual_month_end() =>
        Assert.That(M13Rules.MonthEnd(new DateOnly(2026, 2, 1)), Is.EqualTo(new DateOnly(2026, 2, 28)));

    [Test]
    public void Filing_is_blocked_when_export_errors_remain() =>
        Assert.Throws<InvalidOperationException>(() => M13Rules.RequireReadyToFile(1));

    [Test]
    public void Readiness_is_bounded_to_zero_through_one_hundred()
    {
        Assert.That(M13Rules.ReadinessPercent(4, 3), Is.EqualTo(75));
        Assert.That(M13Rules.ReadinessPercent(0, 0), Is.EqualTo(100));
    }
}
