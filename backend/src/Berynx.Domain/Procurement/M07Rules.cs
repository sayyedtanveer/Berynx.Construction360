namespace Berynx.Domain.Procurement;

public static class M07Rules
{
    private static readonly IReadOnlyDictionary<string, string[]> PurchaseOrderTransitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Draft"] = ["Pending approval", "Approved", "Cancelled"],
            ["Pending approval"] = ["Approved", "Draft", "Cancelled"],
            ["Approved"] = ["Sent", "Partially received", "Received", "Closed", "Cancelled"],
            ["Sent"] = ["Partially received", "Received", "Closed", "Cancelled"],
            ["Partially received"] = ["Received", "Closed"],
            ["Received"] = ["Closed"],
            ["Closed"] = [],
            ["Cancelled"] = []
        };

    public static bool CanTransitionPurchaseOrder(string from, string to) =>
        PurchaseOrderTransitions.TryGetValue(from, out var allowed) &&
        allowed.Contains(to, StringComparer.Ordinal);

    public static void RequirePurchaseOrderTransition(string from, string to)
    {
        if (!CanTransitionPurchaseOrder(from, to))
            throw new InvalidOperationException($"Purchase order transition '{from}' → '{to}' is not allowed.");
    }

    public static decimal MaximumReceiptQuantity(decimal orderedQuantity, decimal tolerancePercent)
    {
        if (orderedQuantity <= 0 || tolerancePercent < 0 || tolerancePercent > 100)
            throw new ArgumentOutOfRangeException(nameof(orderedQuantity));
        return decimal.Round(orderedQuantity * (1m + tolerancePercent / 100m), 3,
            MidpointRounding.AwayFromZero);
    }

    public static void ValidateReceiptQuantity(decimal orderedQuantity, decimal alreadyReceived,
        decimal receiptQuantity, decimal tolerancePercent)
    {
        if (alreadyReceived < 0 || receiptQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(receiptQuantity));
        if (alreadyReceived + receiptQuantity > MaximumReceiptQuantity(orderedQuantity, tolerancePercent))
            throw new InvalidOperationException("Goods receipt exceeds the approved purchase-order tolerance.");
    }

    public static decimal TdsAmount(decimal taxableAmount, decimal ratePercent)
    {
        if (taxableAmount < 0 || ratePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(taxableAmount));
        return decimal.Round(taxableAmount * ratePercent / 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal NetSupplierPayment(decimal grossAmount, decimal tdsAmount, decimal bankCharges)
    {
        if (grossAmount <= 0 || tdsAmount < 0 || bankCharges < 0 || tdsAmount + bankCharges > grossAmount)
            throw new InvalidOperationException("Supplier payment deductions cannot exceed the gross amount.");
        return decimal.Round(grossAmount - tdsAmount - bankCharges, 2, MidpointRounding.AwayFromZero);
    }

    public static void ValidatePaymentAllocations(decimal grossAmount, decimal tdsAmount,
        decimal bankCharges, IEnumerable<decimal> allocations)
    {
        var allocationTotal = allocations.Sum();
        if (allocationTotal < 0 || allocationTotal > grossAmount - tdsAmount)
            throw new InvalidOperationException("Supplier payment allocations exceed the net payable amount.");
        _ = NetSupplierPayment(grossAmount, tdsAmount, bankCharges);
    }

    public static void ValidateInvoiceQuantity(decimal invoiceQuantity, decimal receivedQuantity,
        decimal tolerancePercent)
    {
        if (invoiceQuantity <= 0 || receivedQuantity < 0 || tolerancePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(invoiceQuantity));
        if (invoiceQuantity > decimal.Round(receivedQuantity * (1m + tolerancePercent / 100m),
                3, MidpointRounding.AwayFromZero))
            throw new InvalidOperationException("Supplier invoice quantity exceeds received quantity tolerance.");
    }
}
