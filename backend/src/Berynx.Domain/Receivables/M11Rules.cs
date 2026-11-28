namespace Berynx.Domain.Receivables;

public sealed record M11InvoiceBalance(Guid InvoiceId, DateOnly InvoiceDate, decimal Outstanding);
public sealed record M11Allocation(Guid InvoiceId, decimal Amount);

public static class M11Rules
{
    public static (IReadOnlyList<M11Allocation> Allocations, decimal Advance) AutoAllocateOldestFirst(
        IEnumerable<M11InvoiceBalance> invoiceBalances, decimal receiptAmount)
    {
        if (receiptAmount < 0) throw new ArgumentOutOfRangeException(nameof(receiptAmount));
        var remaining = receiptAmount;
        var allocations = new List<M11Allocation>();
        foreach (var invoice in invoiceBalances
                     .Where(x => x.Outstanding > 0)
                     .OrderBy(x => x.InvoiceDate).ThenBy(x => x.InvoiceId))
        {
            if (remaining == 0) break;
            var applied = Math.Min(remaining, invoice.Outstanding);
            allocations.Add(new M11Allocation(invoice.InvoiceId, applied));
            remaining -= applied;
        }
        return (allocations, remaining);
    }

    public static decimal ValidateAndGetAdvance(decimal receiptAmount, IEnumerable<decimal> allocationAmounts)
    {
        if (receiptAmount <= 0) throw new ArgumentOutOfRangeException(nameof(receiptAmount));
        var allocations = allocationAmounts.ToArray();
        var allocated = allocations.Sum();
        if (allocations.Any(x => x <= 0) || allocated > receiptAmount)
            throw new InvalidOperationException("Allocated total must be positive and cannot exceed receipt amount.");
        return receiptAmount - allocated;
    }

    public static void RequirePostableReceipt(string paymentMethod, decimal amount, decimal tds,
        decimal bankCharges, decimal allocated, decimal advance)
    {
        if (paymentMethod is "Cheque" or "Post-dated cheque")
            throw new InvalidOperationException("Cheque receipts cannot post until M11 records and verifies clearance.");
        if (paymentMethod == "Adjustment")
            throw new InvalidOperationException("Adjustment receipts require an approved adjustment workflow before posting.");
        if (paymentMethod is not ("Cash" or "Bank transfer" or "UPI"))
            throw new InvalidOperationException("This receipt payment method is not enabled for posting.");
        if (amount <= 0 || tds < 0 || bankCharges < 0 || tds + bankCharges > amount ||
            allocated < 0 || advance < 0 || allocated + advance != amount)
            throw new InvalidOperationException("Receipt amount, allocations, advance and deductions do not reconcile.");
    }
    public static void RequirePostedInvoice(string status, decimal outstanding, decimal allocation)
    {
        if (status != "Posted" || outstanding <= 0 || allocation <= 0 || allocation > outstanding)
            throw new InvalidOperationException("Allocation must target an invoice with sufficient posted outstanding.");
    }
}
