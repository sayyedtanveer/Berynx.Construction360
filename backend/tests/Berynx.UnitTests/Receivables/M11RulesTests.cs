using Berynx.Domain.Receivables;
using NUnit.Framework;

namespace Berynx.UnitTests.Receivables;

[TestFixture]
public sealed class M11RulesTests
{
    [Test]
    public void Auto_allocate_applies_oldest_first_and_preserves_excess_as_advance()
    {
        var oldest = Guid.NewGuid();
        var newer = Guid.NewGuid();
        var result = M11Rules.AutoAllocateOldestFirst(
            [new(newer, new DateOnly(2026, 2, 1), 40m), new(oldest, new DateOnly(2026, 1, 1), 30m)],
            100m);
        Assert.That(result.Allocations, Is.EqualTo(new[] { new M11Allocation(oldest, 30m), new M11Allocation(newer, 40m) }));
        Assert.That(result.Advance, Is.EqualTo(30m));
    }

    [Test]
    public void Allocations_cannot_exceed_receipt_amount() =>
        Assert.Throws<InvalidOperationException>(() => M11Rules.ValidateAndGetAdvance(100m, [60m, 50m]));

    [Test]
    public void Empty_receipt_allocations_remain_an_advance_and_invalid_amounts_are_rejected()
    {
        Assert.That(M11Rules.ValidateAndGetAdvance(100m, []), Is.EqualTo(100m));
        Assert.Throws<ArgumentOutOfRangeException>(() => M11Rules.ValidateAndGetAdvance(0m, []));
        Assert.Throws<InvalidOperationException>(() => M11Rules.ValidateAndGetAdvance(100m, [-1m]));
    }

    [Test]
    public void Allocation_requires_posted_invoice_and_sufficient_balance() =>
        Assert.Throws<InvalidOperationException>(() => M11Rules.RequirePostedInvoice("Draft", 100m, 50m));

    [Test]
    public void Receipt_posting_requires_a_supported_settled_method_and_exact_allocation_reconciliation()
    {
        Assert.DoesNotThrow(() => M11Rules.RequirePostableReceipt(
            "Bank transfer", 100m, 5m, 2m, 90m, 10m));
        Assert.Throws<InvalidOperationException>(() => M11Rules.RequirePostableReceipt(
            "Bank transfer", 100m, 5m, 2m, 80m, 10m));
        Assert.Throws<InvalidOperationException>(() => M11Rules.RequirePostableReceipt(
            "Cheque", 100m, 0m, 0m, 100m, 0m));
    }

    [Test]
    public void Receipt_advance_is_amount_less_than_allocations()
    {
        Assert.That(M11Rules.ValidateAndGetAdvance(100m, [70m]), Is.EqualTo(30m));
    }}
