using Berynx.Domain.Quotations;
using NUnit.Framework;

namespace Berynx.UnitTests.Quotations;

[TestFixture]
public sealed class M05QuotationCalculatorTests
{
    [Test]
    public void CalculateLine_RoundsDiscountAndSplitsIntraStateTax()
    {
        var result = M05QuotationCalculator.CalculateLine(3m, 125.50m, 10m, 18m, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.GrossAmount, Is.EqualTo(376.50m));
            Assert.That(result.DiscountAmount, Is.EqualTo(37.65m));
            Assert.That(result.TaxableAmount, Is.EqualTo(338.85m));
            Assert.That(result.CgstAmount, Is.EqualTo(30.50m));
            Assert.That(result.SgstAmount, Is.EqualTo(30.50m));
            Assert.That(result.IgstAmount, Is.Zero);
            Assert.That(result.Total, Is.EqualTo(399.85m));
        });
    }

    [Test]
    public void CalculateLine_UsesIgstForInterStateSupply()
    {
        var result = M05QuotationCalculator.CalculateLine(1m, 100m, 0m, 18m, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.CgstAmount, Is.Zero);
            Assert.That(result.SgstAmount, Is.Zero);
            Assert.That(result.IgstAmount, Is.EqualTo(18m));
            Assert.That(result.Total, Is.EqualTo(118m));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CalculateLine_RejectsNonPositiveQuantity(decimal quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            M05QuotationCalculator.CalculateLine(quantity, 10m, 0m, 0m, true));
    }

    [Test]
    public void CalculateLine_RejectsExcessDiscountPrecision()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            M05QuotationCalculator.CalculateLine(1m, 10m, 1.23456m, 0m, true));
    }
}
