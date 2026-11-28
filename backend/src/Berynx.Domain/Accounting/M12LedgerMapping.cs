using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Accounting;

public static class M12MappingCodes
{
    public const string Receivables = "Receivables";
    public const string Revenue = "Revenue";
    public const string OutputCgst = "OutputCgst";
    public const string OutputSgst = "OutputSgst";
    public const string OutputIgst = "OutputIgst";
    public const string RoundOff = "RoundOff";
    public const string ReceiptCash = "ReceiptCash";
    public const string ReceiptBank = "ReceiptBank";
    public const string ReceiptUpi = "ReceiptUpi";
    public const string ReceiptCheque = "ReceiptCheque";
    public const string ReceiptAdjustment = "ReceiptAdjustment";
    public const string TdsReceivable = "TdsReceivable";
    public const string TcsPayable = "TcsPayable";
    public const string BankCharges = "BankCharges";
    public const string CustomerAdvances = "CustomerAdvances";

    public static readonly string[] All =
    [
        Receivables, Revenue, OutputCgst, OutputSgst, OutputIgst, RoundOff,
        ReceiptCash, ReceiptBank, ReceiptUpi, ReceiptCheque, ReceiptAdjustment,
        TdsReceivable, TcsPayable, BankCharges, CustomerAdvances
    ];
}

public sealed class M12LedgerMapping : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid MappingId { get; set; }
    public string MappingCode { get; set; } = "";
    public Guid BranchId { get; set; }
    public string Gstin { get; set; } = "";
    public string LedgerCode { get; set; } = "";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class M12LedgerMappingEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid EventId { get; set; }
    public Guid MappingId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = "";
    public string DetailJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}
public static class M12LedgerMappingRules
{
    public static bool Overlaps(DateOnly firstStart, DateOnly? firstEnd, DateOnly secondStart, DateOnly? secondEnd) =>
        (!firstEnd.HasValue || firstEnd.Value >= secondStart) &&
        (!secondEnd.HasValue || secondEnd.Value >= firstStart);

    public static void RequireLedgerGroup(string mappingCode, string group)
    {
        var value = group.ToLowerInvariant();
        var valid = mappingCode switch
        {
            M12MappingCodes.Receivables or M12MappingCodes.TdsReceivable => value.Contains("asset") || value.Contains("debtor") || value.Contains("receivable"),
            M12MappingCodes.Revenue => value.Contains("income") || value.Contains("revenue"),
            M12MappingCodes.OutputCgst or M12MappingCodes.OutputSgst or M12MappingCodes.OutputIgst or M12MappingCodes.TcsPayable => value.Contains("liabilit") || value.Contains("tax"),
            M12MappingCodes.ReceiptCash or M12MappingCodes.ReceiptBank or M12MappingCodes.ReceiptUpi or M12MappingCodes.ReceiptCheque or M12MappingCodes.ReceiptAdjustment => value.Contains("asset") || value.Contains("cash") || value.Contains("bank"),
            M12MappingCodes.CustomerAdvances => value.Contains("liabilit") || value.Contains("payable"),
            M12MappingCodes.BankCharges => value.Contains("expense"),
            M12MappingCodes.RoundOff => value.Contains("income") || value.Contains("expense"),
            _ => false
        };
        if (!valid) throw new InvalidOperationException($"Ledger group '{group}' is not valid for mapping role '{mappingCode}'.");
    }
    public static void RequireValidRange(string mappingCode, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (!M12MappingCodes.All.Contains(mappingCode, StringComparer.Ordinal) ||
            effectiveFrom == default || effectiveTo < effectiveFrom)
            throw new InvalidOperationException("Mapping role and effective-date range must be valid.");
    }
}