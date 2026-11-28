namespace Berynx.Domain.DeliveryChallans;

public static class M09Rules
{
    public static readonly IReadOnlyDictionary<string, string[]> Transitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Unverified"] = ["Delivered", "Cancelled"],
            ["Delivered"] = ["Invoiced", "Cancelled"],
            ["Invoiced"] = [],
            ["Cancelled"] = []
        };

    public static void RequireTransition(string from, string to)
    {
        if (!Transitions.TryGetValue(from, out var allowed) || !allowed.Contains(to, StringComparer.Ordinal))
            throw new InvalidOperationException($"Invalid challan transition: {from} → {to}.");
    }

    public static string StatusForReceiverVerification(bool receiverVerified) =>
        receiverVerified ? "Delivered" : "Unverified";
    public static void ValidateCorrection(bool invoiced, decimal original, decimal revised, string? reason)
    {
        if (invoiced)
            throw new InvalidOperationException("An invoiced challan cannot be corrected; reverse the invoice and issue an approved credit/debit note.");
        if (original <= 0 || revised <= 0 || string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A correction needs positive quantities and a reason.");
    }

}
