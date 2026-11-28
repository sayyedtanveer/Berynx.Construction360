using Berynx.Domain.Accounting;
using NUnit.Framework;

namespace Berynx.UnitTests.Accounting;

[TestFixture]
public sealed class M12RulesTests
{
    [Test]
    public void Balanced_voucher_accepts_exact_decimal_debits_and_credits()
    {
        var total = M12Rules.ValidateBalancedVoucher([
            new("BANK", null, null, null, null, null, "", 100.00m, 0m),
            new("REVENUE", null, null, null, null, null, "", 0m, 100.00m)
        ]);
        Assert.That(total, Is.EqualTo((100m, 100m)));
    }

    [Test]
    public void Reversal_lines_swap_debit_and_credit_and_preserve_accounting_dimensions()
    {
        var reversed = M12Rules.ReversalLines([
            new M12VoucherLine
            {
                LedgerCode = "RECEIVABLES", PartyId = Guid.NewGuid(), BranchId = Guid.NewGuid(),
                SiteId = Guid.NewGuid(), SalesOrderId = Guid.NewGuid(), TripId = Guid.NewGuid(),
                Narration = "Invoice receivable", Debit = 118m
            },
            new M12VoucherLine
            {
                LedgerCode = "REVENUE", BranchId = Guid.NewGuid(),
                Narration = "Invoice revenue", Credit = 100m
            },
            new M12VoucherLine
            {
                LedgerCode = "GST", BranchId = Guid.NewGuid(),
                Narration = "Output GST", Credit = 18m
            }
        ]);

        Assert.That(reversed.Sum(x => x.Debit), Is.EqualTo(118m));
        Assert.That(reversed.Sum(x => x.Credit), Is.EqualTo(118m));
        Assert.That(reversed[0].Debit, Is.Zero);
        Assert.That(reversed[0].Credit, Is.EqualTo(118m));
        Assert.That(reversed[0].PartyId, Is.Not.Null);
        Assert.That(reversed[0].TripId, Is.Not.Null);
    }

    [Test]
    public void Reversal_lines_reject_an_unbalanced_original_voucher()
    {
        Assert.Throws<InvalidOperationException>(() => M12Rules.ReversalLines([
            new M12VoucherLine { LedgerCode = "BANK", Debit = 10m },
            new M12VoucherLine { LedgerCode = "REVENUE", Credit = 9m }
        ]));
    }

    [Test]
    public void Unbalanced_or_single_sided_line_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => M12Rules.ValidateBalancedVoucher([
            new("BANK", null, null, null, null, null, "", 100m, 0m),
            new("REVENUE", null, null, null, null, null, "", 0m, 99m)
        ]));
        Assert.Throws<InvalidOperationException>(() => M12Rules.ValidateBalancedVoucher([
            new("BANK", null, null, null, null, null, "", 100m, 100m),
            new("REVENUE", null, null, null, null, null, "", 0m, 0m)
        ]));
    }

    [Test]
    public void Manual_control_account_needs_party_dimension() =>
        Assert.Throws<InvalidOperationException>(() => M12Rules.RequireManualAccountAllowed("Debtors", null));

    [Test]
    public void Locked_period_detects_date_range() =>
        Assert.That(M12Rules.IsPeriodLocked(new DateOnly(2026, 1, 31), [
            new M12FinancialPeriod { PeriodStart = new(2026, 1, 1), PeriodEnd = new(2026, 1, 31), IsLocked = true }
        ]), Is.True);

    [Test]
    public void Effective_mapping_ranges_detect_overlap_and_adjacency()
    {
        var start = new DateOnly(2026, 4, 1);
        var next = new DateOnly(2027, 4, 1);
        Assert.That(M12LedgerMappingRules.Overlaps(start, next.AddDays(-1), next, null), Is.False);
        Assert.That(M12LedgerMappingRules.Overlaps(start, null, next, null), Is.True);
    }

    [Test]
    public void Mapping_requires_known_role_and_valid_date_range()
    {
        Assert.DoesNotThrow(() => M12LedgerMappingRules.RequireValidRange(
            M12MappingCodes.Receivables, new DateOnly(2026, 4, 1), null));
        Assert.Throws<InvalidOperationException>(() => M12LedgerMappingRules.RequireValidRange(
            "Unknown", new DateOnly(2026, 4, 1), null));
        Assert.Throws<InvalidOperationException>(() => M12LedgerMappingRules.RequireValidRange(
            M12MappingCodes.Revenue, new DateOnly(2026, 4, 2), new DateOnly(2026, 4, 1)));
    }

    [Test]
    public void Every_supported_posting_role_including_tcs_is_configurable()
    {
        Assert.That(M12MappingCodes.All, Does.Contain(M12MappingCodes.TcsPayable));
        Assert.That(M12MappingCodes.All.Distinct(StringComparer.Ordinal).Count(),
            Is.EqualTo(M12MappingCodes.All.Length));
    }

    [Test]
    public void Ledger_mapping_role_requires_compatible_account_group()
    {
        Assert.DoesNotThrow(() => M12LedgerMappingRules.RequireLedgerGroup(M12MappingCodes.Revenue, "Income"));
        Assert.DoesNotThrow(() => M12LedgerMappingRules.RequireLedgerGroup(M12MappingCodes.OutputCgst, "Liabilities"));
        Assert.Throws<InvalidOperationException>(() => M12LedgerMappingRules.RequireLedgerGroup(M12MappingCodes.Revenue, "Assets"));
    }}
