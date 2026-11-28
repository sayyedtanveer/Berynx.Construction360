namespace Berynx.Domain.Accounting;

public sealed record M12LineDraft(
    string LedgerCode, Guid? PartyId, Guid? BranchId, Guid? SiteId,
    Guid? SalesOrderId, Guid? TripId, string Narration, decimal Debit, decimal Credit);

public static class M12Rules
{
    public static IReadOnlyList<M12LineDraft> ReversalLines(IEnumerable<M12VoucherLine> originalLines)
    {
        var lines = originalLines.Select(x => new M12LineDraft(
            x.LedgerCode, x.PartyId, x.BranchId, x.SiteId, x.SalesOrderId, x.TripId,
            x.Narration, x.Credit, x.Debit)).ToArray();
        ValidateBalancedVoucher(lines);
        return lines;
    }

    public static (decimal Debit, decimal Credit) ValidateBalancedVoucher(
        IReadOnlyCollection<M12LineDraft> lines)
    {
        if (lines.Count < 2)
            throw new InvalidOperationException("A voucher needs at least two lines.");
        if (lines.Any(x => string.IsNullOrWhiteSpace(x.LedgerCode) ||
                           x.Debit < 0 || x.Credit < 0 ||
                           (x.Debit == 0) == (x.Credit == 0) ||
                           decimal.Round(x.Debit, 2) != x.Debit ||
                           decimal.Round(x.Credit, 2) != x.Credit))
            throw new InvalidOperationException("Each voucher line must have one positive debit or credit rounded to two decimals.");
        var debit = lines.Sum(x => x.Debit);
        var credit = lines.Sum(x => x.Credit);
        if (debit <= 0 || debit != credit)
            throw new InvalidOperationException("Voucher debits and credits must balance exactly.");
        return (debit, credit);
    }

    public static bool IsControlAccount(string group) =>
        group.Contains("debtor", StringComparison.OrdinalIgnoreCase) ||
        group.Contains("creditor", StringComparison.OrdinalIgnoreCase) ||
        group.Contains("gst", StringComparison.OrdinalIgnoreCase) ||
        group.Contains("tds", StringComparison.OrdinalIgnoreCase);

    public static void RequireManualAccountAllowed(string group, Guid? partyId)
    {
        if (IsControlAccount(group) && partyId is null)
            throw new InvalidOperationException("Control accounts require a party dimension in manual vouchers.");
    }

    public static bool IsPeriodLocked(DateOnly date, IEnumerable<M12FinancialPeriod> periods) =>
        periods.Any(x => x.IsLocked && x.PeriodStart <= date && x.PeriodEnd >= date);
}
