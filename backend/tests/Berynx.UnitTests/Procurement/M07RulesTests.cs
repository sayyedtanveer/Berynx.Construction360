using Berynx.Domain.Procurement;
using NUnit.Framework;

namespace Berynx.UnitTests.Procurement;

[TestFixture]
public sealed class M07RulesTests
{
    [TestCase("Draft", "Pending approval", true)]
    [TestCase("Approved", "Sent", true)]
    [TestCase("Sent", "Partially received", true)]
    [TestCase("Received", "Closed", true)]
    [TestCase("Draft", "Sent", false)]
    [TestCase("Closed", "Sent", false)]
    public void Purchase_order_lifecycle_allows_only_defined_transitions(
        string from, string to, bool expected) =>
        Assert.That(M07Rules.CanTransitionPurchaseOrder(from, to), Is.EqualTo(expected));

    [TestCase(100, 1, 101)]
    [TestCase(50, 2, 51)]
    public void Receipt_limit_applies_configured_tolerance(
        decimal ordered, decimal tolerancePercent, decimal expected) =>
        Assert.That(M07Rules.MaximumReceiptQuantity(ordered, tolerancePercent), Is.EqualTo(expected));

    [Test]
    public void Receipt_rejects_quantity_beyond_tolerance() =>
        Assert.Throws<InvalidOperationException>(() =>
            M07Rules.ValidateReceiptQuantity(100, 99, 2.001m, 1));

    [Test]
    public void Invoice_quantity_cannot_exceed_received_tolerance() =>
        Assert.Throws<InvalidOperationException>(() =>
            M07Rules.ValidateInvoiceQuantity(103, 100, 2));

    [Test]
    public void Supplier_payment_uses_gross_less_tds_and_charges()
    {
        Assert.That(M07Rules.TdsAmount(100_000m, 2m), Is.EqualTo(2_000m));
        Assert.That(M07Rules.NetSupplierPayment(100_000m, 2_000m, 100m), Is.EqualTo(97_900m));
    }

    [Test]
    public void Supplier_payment_rejects_over_allocation() =>
        Assert.Throws<InvalidOperationException>(() =>
            M07Rules.ValidatePaymentAllocations(1000m, 100m, 20m, [901m]));
}
