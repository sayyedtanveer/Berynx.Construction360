using Berynx.Domain.Quality;
using NUnit.Framework;

namespace Berynx.UnitTests.Quality;

[TestFixture]
public sealed class M19RulesTests
{
    [Test]
    public void DueAt_UsesConfiguredTestAgeWithoutInventingAcceptanceThresholds()
    {
        var cast = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        Assert.That(M19Rules.DueAt(cast, 7), Is.EqualTo(cast.AddDays(7)));
        Assert.That(M19Rules.Evaluate(35m, null), Is.EqualTo(M19Rules.ReviewRequired));
        Assert.That(M19Rules.Evaluate(34.99m, 35m), Is.EqualTo(M19Rules.Fail));
        Assert.That(M19Rules.Evaluate(35m, 35m), Is.EqualTo(M19Rules.Pass));
    }

    [TestCase(0)]
    [TestCase(366)]
    public void DueAt_RejectsOutOfRangeAge(int ageDays) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => M19Rules.DueAt(DateTimeOffset.UtcNow, ageDays));

    [Test]
    public void Criteria_RequiresARealReferenceAndPositiveConfiguredThreshold()
    {
        Assert.Throws<ArgumentException>(() => M19Rules.ValidateCriteria("Concrete", "M30", 28, 0m, "IS 516", "approved"));
        Assert.Throws<ArgumentException>(() => M19Rules.ValidateCriteria("Concrete", "M30", 28, 30m, "", "approved"));
        Assert.DoesNotThrow(() => M19Rules.ValidateCriteria("Concrete", "M30", 28, 30m, "Project QA-01", "approved by quality manager"));
    }

    [Test]
    public void SampleStatus_PrioritizesReviewAndFailureOverAnIncompletePassSet()
    {
        var passed = new M19QualityTest { Status = "Tested", Outcome = M19Rules.Pass };
        var failed = new M19QualityTest { Status = "Tested", Outcome = M19Rules.Fail };
        var review = new M19QualityTest { Status = M19Rules.ReviewRequired, Outcome = M19Rules.ReviewRequired };

        Assert.That(M19Rules.RecomputeSampleStatus([passed]), Is.EqualTo("Passed"));
        Assert.That(M19Rules.RecomputeSampleStatus([passed, failed]), Is.EqualTo("Failed"));
        Assert.That(M19Rules.RecomputeSampleStatus([passed, review]), Is.EqualTo(M19Rules.ReviewRequired));
        Assert.That(M19Rules.RecomputeSampleStatus([]), Is.EqualTo(M19Rules.ReviewRequired));
    }

    [Test]
    public void MaterialType_RejectsUnknownTypesInsteadOfApplyingGenericQualityRules()
    {
        Assert.That(M19Rules.NormalizeMaterialType("Concrete"), Is.EqualTo("Concrete"));
        Assert.Throws<ArgumentException>(() => M19Rules.NormalizeMaterialType("Concrete-ish"));
    }
}