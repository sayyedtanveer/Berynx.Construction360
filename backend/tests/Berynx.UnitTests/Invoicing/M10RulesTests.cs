using Berynx.Domain.Invoicing;
using NUnit.Framework;

namespace Berynx.UnitTests.Invoicing;

[TestFixture]
public sealed class M10RulesTests
{
    [TestCase("27", "27", "CGST+SGST")]
    [TestCase("27", "29", "IGST")]
    public void Tax_treatment_uses_delivery_state_and_branch_state(string site, string branch, string expected) =>
        Assert.That(M10Rules.TaxTreatment(site, branch), Is.EqualTo(expected));

    [Test]
    public void Missing_place_of_supply_state_fails_closed() =>
        Assert.Throws<InvalidOperationException>(() => M10Rules.TaxTreatment("", "27"));

    [Test]
    public void Invoice_quantity_cannot_exceed_delivered_unbilled_quantity() =>
        Assert.Throws<InvalidOperationException>(() => M10Rules.ValidateDeliveredQuantity(10m, 8m, 3m));

    [Test]
    public void Draft_round_off_is_nearest_rupee_and_under_one_rupee() =>
        Assert.That(Math.Abs(M10Rules.RoundOff(999.60m)), Is.LessThanOrEqualTo(1m));

    [Test]
    public void Collection_status_uses_posted_allocations_and_due_date_only_for_posted_invoices()
    {
        var today = new DateOnly(2026, 6, 10);
        var due = today.AddDays(-1);
        Assert.That(M10Rules.CollectionStatus("Draft", due, 100m, 0m, today), Is.EqualTo("Draft"));
        Assert.That(M10Rules.CollectionStatus("Posted", null, 100m, 0m, today), Is.EqualTo("Open"));
        Assert.That(M10Rules.CollectionStatus("Posted", due, 100m, 25m, today), Is.EqualTo("Overdue"));
        Assert.That(M10Rules.CollectionStatus("Posted", due, 100m, 100m, today), Is.EqualTo("Paid"));
        Assert.That(M10Rules.CollectionStatus("Posted", due, 100m, 110m, today), Is.EqualTo("Paid"));
        Assert.That(M10Rules.Outstanding(100m, 25m), Is.EqualTo(75m));
        Assert.That(M10Rules.Outstanding(100m, 110m), Is.Zero);
    }

    [Test]
    public void Outstanding_rejects_negative_invoice_or_allocation_values()
    {
        Assert.Throws<InvalidOperationException>(() => M10Rules.Outstanding(-1m, 0m));
        Assert.Throws<InvalidOperationException>(() => M10Rules.Outstanding(1m, -1m));
    }

    [Test]
    public void Unverified_challan_is_not_invoiceable_without_approval_flow() =>
        Assert.Throws<InvalidOperationException>(() => M10Rules.RequireInvoiceableChallan("Unverified", false));

    [Test]
    public void Invoice_posting_rejects_unresolved_tax_type_and_non_draft_state()
    {
        Assert.Throws<InvalidOperationException>(() => M10Rules.RequirePostableInvoice(
            "Draft", "Unresolved", false, 100m, 100m, 0m, 0m, 0m, 0m));
        Assert.Throws<InvalidOperationException>(() => M10Rules.RequirePostableInvoice(
            "Cancelled", "Tax invoice", false, 100m, 100m, 0m, 0m, 0m, 0m));
    }

    [Test]
    public void Bill_of_supply_cannot_post_output_tax()
    {
        Assert.Throws<InvalidOperationException>(() => M10Rules.RequirePostableInvoice(
            "Draft", "Bill of supply", false, 118m, 100m, 9m, 9m, 0m, 0m));
        Assert.DoesNotThrow(() => M10Rules.RequirePostableInvoice(
            "Draft", "Bill of supply", false, 100m, 100m, 0m, 0m, 0m, 0m));
    }

    [Test]
    public void Posted_invoice_cancellation_requires_voucher_and_blocks_allocations_or_irn()
    {
        Assert.DoesNotThrow(() => M10Rules.RequireCancellablePostedInvoice("Posted", true, false, false));
        Assert.Throws<InvalidOperationException>(() =>
            M10Rules.RequireCancellablePostedInvoice("Draft", true, false, false));
        Assert.Throws<InvalidOperationException>(() =>
            M10Rules.RequireCancellablePostedInvoice("Posted", false, false, false));
        Assert.Throws<InvalidOperationException>(() =>
            M10Rules.RequireCancellablePostedInvoice("Posted", true, true, false));
        Assert.Throws<InvalidOperationException>(() =>
            M10Rules.RequireCancellablePostedInvoice("Posted", true, false, true));
    }

    [TestCase("Regular", "Tax invoice")]
    [TestCase("Composition", "Bill of supply")]
    [TestCase("Unspecified", "Unresolved")]
    public void Branch_registration_selects_invoice_type(string registration, string expected) =>
        Assert.That(M10Rules.InvoiceType(registration), Is.EqualTo(expected));

    [Test]
    public void Billing_mode_requires_customer_configuration_and_reason_for_a_real_override()
    {
        Assert.That(M10Rules.ResolveBillingMode("Monthly", null, null), Is.EqualTo("Monthly"));
        Assert.That(M10Rules.ResolveBillingMode("Monthly", "Monthly", null), Is.EqualTo("Monthly"));
        Assert.That(M10Rules.ResolveBillingMode("Monthly", "PerChallan", "Customer request"), Is.EqualTo("PerChallan"));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ResolveBillingMode(null, null, null));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ResolveBillingMode("Unknown", null, null));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ResolveBillingMode("Monthly", "PerChallan", null));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ResolveBillingMode("Monthly", "Unknown", "reason"));
    }

    [Test]
    public void Billing_modes_enforce_challan_date_and_order_grouping()
    {
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var sameDay = new[]
        {
            (orderA, new DateOnly(2026, 5, 14)),
            (orderB, new DateOnly(2026, 5, 14))
        };
        var sameOrder = new[]
        {
            (orderA, new DateOnly(2026, 5, 14)),
            (orderA, new DateOnly(2026, 5, 15))
        };

        Assert.DoesNotThrow(() => M10Rules.ValidateBillingGrouping("Daily", sameDay));
        Assert.DoesNotThrow(() => M10Rules.ValidateBillingGrouping("PerOrder", sameOrder));
        Assert.DoesNotThrow(() => M10Rules.ValidateBillingGrouping("Monthly", sameOrder));
        Assert.DoesNotThrow(() => M10Rules.ValidateBillingGrouping("PerChallan", sameOrder.Take(1).ToArray()));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ValidateBillingGrouping("PerChallan", sameOrder));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ValidateBillingGrouping("Daily", sameOrder));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ValidateBillingGrouping("PerOrder", sameDay));
        Assert.Throws<InvalidOperationException>(() => M10Rules.ValidateBillingGrouping("Monthly",
            new[] { (orderA, new DateOnly(2026, 5, 31)), (orderA, new DateOnly(2026, 6, 1)) }));
    }
}
