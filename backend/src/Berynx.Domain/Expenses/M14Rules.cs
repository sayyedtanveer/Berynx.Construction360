namespace Berynx.Domain.Expenses;

public static class M14Rules
{
    public static bool IsCategoryType(string type) =>
        type is "Direct" or "Indirect" or "Capital" or "Other income";

    public static string InitialStatus(decimal amount, decimal? approvalThreshold) =>
        approvalThreshold is { } threshold && amount > threshold ? "Pending approval" : "Approved";

    public static void RequirePostable(M14Expense expense)
    {
        if (expense.Status != "Approved")
            throw new InvalidOperationException("Only approved expenses can be posted.");
        if (expense.Amount <= 0 || decimal.Round(expense.Amount, 2) != expense.Amount)
            throw new InvalidOperationException("Expense amount must be positive and rounded to two decimals.");
        if (expense.GstAmount < 0 || expense.GstAmount > expense.Amount)
            throw new InvalidOperationException("GST amount must be within the expense amount.");
        if (expense.ReverseCharge && expense.ItcEligible)
            throw new InvalidOperationException("Reverse-charge ITC treatment requires a separate approved tax workflow.");
    }
}
