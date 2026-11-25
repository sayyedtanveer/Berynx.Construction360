using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Onboarding;
public sealed class Tenant : ITenantOwned
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public string Plan { get; set; } = "";
    public string Status { get; set; } = "Trial";
    public Guid? CurrentSubscriptionId { get; set; }
    public long Version { get; set; }
    public DateTimeOffset? LiveAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string Configuration { get; set; } = "{}";
}

public sealed class OpeningItem : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public string Ledger { get; set; } = "";
    public string? Bill { get; set; }

    [System.Text.Json.Serialization.JsonNumberHandlingAttribute(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString)]
    public decimal Debit { get; set; }

    [System.Text.Json.Serialization.JsonNumberHandlingAttribute(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString)]
    public decimal Credit { get; set; }

    public void Validate()
    {
        if (Kind is not ("Cash" or "Bank" or "Customer" or "Supplier" or "Ledger") || string.IsNullOrWhiteSpace(Ledger) || Ledger.Length > 150 || (Kind is "Customer" or "Supplier" && string.IsNullOrWhiteSpace(Bill)) || Bill?.Length > 100 || Debit < 0 || Credit < 0 || (Debit == 0) == (Credit == 0) || Math.Max(Debit, Credit) >= 10000000000000000m || decimal.Round(Debit, 2) != Debit || decimal.Round(Credit, 2) != Credit)
            throw new ValidationException("Enter a ledger, a bill for customer/supplier items and exactly one positive debit or credit with up to two decimals.");
    }
}

public sealed class SetupEvent : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string Origin { get; set; } = "{}";
    public string Before { get; set; } = "{}";
    public string After { get; set; } = "{}";
    public DateTimeOffset At { get; set; }
}

public sealed record Company(string LegalName, string TradeName, string Pan, string Address, string? Logo, string? Udyam, bool Msme, bool EInvoice, bool EWayBill, string? PaymentTerms);
public sealed record Branch(Guid Id, string Name, string Address, string Gstin);
public sealed record Plant(Guid Id, Guid BranchId, string Name, string Address);
public sealed record Account(string Name, string Kind, string? Upi, string? AccountNumber = null, string? Ifsc = null);
public sealed record TaxDefault(string Name, decimal Rate, string Unit);
public sealed record Series(Guid BranchId, string Document, string Prefix, string BranchCode, string Pattern);
public sealed record Invitation(string Email, string Role, DateTimeOffset At);
public sealed record LedgerDefinition(string Code, string Name, string Group);
public sealed record MasterSeed(string Kind, string Reference, string Name);
public sealed record Limits(long Users, long Storage, long Documents, long Messages, long ApiRequests = 0);
public sealed class TenantSecurityPolicy
{
    public bool RequireMfaForTenantAdmins { get; set; }
    public bool RequireMfaForAccounts { get; set; }
}

public sealed class Setup
{
    public string Currency { get; set; } = "INR";
    public string TimeZone { get; set; } = "Asia/Kolkata";
    public Company? Company { get; set; }
    public List<string> Registrations { get; set; } = [];
    public List<Branch> Branches { get; set; } = [];
    public List<Plant> Plants { get; set; } = [];
    public int? StartMonth { get; set; }
    public List<Series> Series { get; set; } = [];
    public Dictionary<string, string> NumberingDefaults { get; set; } = [];
    public string[] TaxMaster { get; set; } = ["CGST", "SGST", "IGST"];
    public List<Account> Accounts { get; set; } = [];
    public List<TaxDefault> Taxes { get; set; } = [];
    public List<Invitation> Invitations { get; set; } = [];
    public bool SkipInvite { get; set; }
    public List<MasterSeed> MasterSeeds { get; set; } = [];
    public HashSet<int> Steps { get; set; } = [];
    public string[] Modules { get; set; } = [];
    public TenantSecurityPolicy Security { get; set; } = new();
    public int M04FirstResponseSlaMinutes { get; set; } = 30;
    public Dictionary<string, string[]> Roles { get; set; } = new()
    {
        ["TenantAdmin"] = ["onboarding.manage", "opening.manage", "usage.view"],
        ["Accountant"] = ["opening.manage"]
    };
    public LedgerDefinition[] Ledgers { get; set; } = [
        new("cash", "Cash", "Assets"), new("bank", "Bank", "Assets"),
        new("receivables", "Receivables", "Assets"), new("payables", "Payables", "Liabilities"),
        new("opening-equity", "Opening equity", "Equity")
    ];
    public Limits Limits { get; set; } = new(0, 0, 0, 0);
}

public static class SetupRules
{
    public static readonly string[] Documents = ["Quotation", "SalesOrder", "PurchaseOrder", "Challan", "Invoice", "Receipt"];
    public static string Gstin(string? input)
    {
        var value = input?.Trim().ToUpperInvariant() ?? "";
        if (!Regex.IsMatch(value, @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$"))
            throw new ValidationException("Check the 15-character GSTIN format.");
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var sum = 0;
        for (var i = 0; i < 14; i++)
        {
            int product = digits.IndexOf(value[i]) * (1 + i % 2);
            sum += product / 36 + product % 36;
        }

        if (digits[(36 - sum % 36) % 36] != value[14])
            throw new ValidationException("GSTIN checksum does not match.");
        return value;
    }

    public static string Number(Series series, int month, DateOnly date, long sequence)
    {
        if (!Documents.Contains(series.Document) || month is < 1 or > 12 || sequence < 1 || string.IsNullOrWhiteSpace(series.Pattern) || series.Pattern.Length > 100)
            throw new ValidationException("Check the document type, financial year and pattern.");
        var year = date.Year - (date.Month < month ? 1 : 0);
        var token = Regex.Matches(series.Pattern, @"\{SEQ:([1-9])\}");
        if (token.Count != 1)
            throw new ValidationException("Include exactly one {SEQ:1} through {SEQ:9} token.");
        var width = int.Parse(token[0].Groups[1].Value);
        var number = series.Pattern.Replace("{FY}", $"{year % 100:00}{(year + 1) % 100:00}").Replace("{PREFIX}", series.Prefix).Replace("{BRANCH}", series.BranchCode).Replace(token[0].Value, sequence.ToString("D" + width));
        if (!Regex.IsMatch(number, @"^[A-Za-z0-9/-]+$") || (series.Document == "Invoice" && number.Length > 16))
            throw new ValidationException("Use letters, digits, slash or hyphen. Invoice numbers must fit 16 characters.");
        return number;
    }

    public static bool Warning(long used, long limit) => limit > 0 && used >= limit * 0.8m;
    public static void Capacity(long used, long increment, long limit)
    {
        if (increment < 0 || used > limit - increment)
            throw new ValidationException("Plan limit reached. Upgrade your plan to continue.");
    }

    public static void Transition(Tenant tenant, string next, DateTimeOffset now)
    {
        if (tenant.Status == next)
            return;
        if ((tenant.Status, next)is not (("Trial", "Active" or "Grace" or "Suspended" or "Cancelled") or ("Active", "Grace" or "Suspended" or "Cancelled") or ("Grace", "Active" or "Suspended" or "Cancelled") or ("Suspended", "Active" or "Cancelled")))
            throw new ValidationException("Invalid tenant lifecycle transition.");
        tenant.Status = next;
        if (next == "Cancelled")
            tenant.CancelledAt = now;
    }
}
