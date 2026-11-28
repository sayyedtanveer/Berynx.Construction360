namespace Berynx.Domain.SalesOrders;

public static class M06Rules
{
    private static readonly IReadOnlyDictionary<string, string[]> Transitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Draft"] = ["Pending approval", "Confirmed", "Cancelled"],
            ["Pending approval"] = ["Confirmed", "Draft", "Cancelled"],
            ["Confirmed"] = ["In fulfilment", "Completed", "On hold", "Cancelled", "Short-closed"],
            ["In fulfilment"] = ["Completed", "On hold", "Cancelled", "Short-closed"],
            ["On hold"] = ["Confirmed", "In fulfilment", "Cancelled", "Short-closed"],
            ["Completed"] = [],
            ["Short-closed"] = ["Confirmed"],
            ["Cancelled"] = []
        };

    public static bool CanTransition(string from, string to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to, StringComparer.Ordinal);

    public static void RequireTransition(string from, string to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Sales order transition '{from}' → '{to}' is not allowed.");
    }

    public static decimal MaximumScheduleQuantity(decimal orderedQuantity, decimal tolerancePercent)
    {
        if (orderedQuantity <= 0 || tolerancePercent < 0)
            throw new ArgumentOutOfRangeException(nameof(orderedQuantity));
        return decimal.Round(orderedQuantity * (1m + tolerancePercent / 100m), 3,
            MidpointRounding.AwayFromZero);
    }

    public static void ValidateScheduleTotal(decimal orderedQuantity, decimal scheduledQuantity,
        decimal tolerancePercent)
    {
        if (scheduledQuantity < 0 ||
            scheduledQuantity > MaximumScheduleQuantity(orderedQuantity, tolerancePercent))
            throw new InvalidOperationException("Scheduled quantity exceeds the configured order tolerance.");
    }
}
