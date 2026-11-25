using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using Berynx.Domain.Onboarding;

namespace Berynx.Application.Onboarding;
public record Locations(string[] Registrations, Branch[] Branches, Plant[] Plants);
public record Fiscal(int StartMonth, Series[] Series);
public record Invite(string? Email, bool Skip);
public record Check(string Label, string Route, bool Passed);
public record Command(long Version, JsonElement Data);
public record Provision(Guid RequestId, string Name, string Plan, string AdminEmail, string[] Modules, Limits Limits);
public static class Workflow
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static T Read<T>(this JsonElement data) => data.Deserialize<T>(Json) ?? throw new ValidationException("Missing form data.");
    public static void Require(bool ok, string message)
    {
        if (!ok)
            throw new ValidationException(message);
    }

    public static void Save(Setup s, int step, JsonElement data)
    {
        switch (step)
        {
            case 1:
                var c = data.Read<Company>();
                Require(!string.IsNullOrWhiteSpace(c.LegalName) && !string.IsNullOrWhiteSpace(c.Address) && Regex.IsMatch(c.Pan ?? "", "^[A-Z]{5}[0-9]{4}[A-Z]$") && s.Registrations.All(g => g.Substring(2, 10) == c.Pan), "Check company name, address and PAN.");
                Require(string.IsNullOrEmpty(c.Logo) || Uri.TryCreate(c.Logo, UriKind.Absolute, out var logo) && logo.Scheme == "https", "Enter a secure logo URL.");
                s.Company = c;
                break;
            case 2:
                var l = data.Read<Locations>();
                var gs = l.Registrations.Select(SetupRules.Gstin).ToList();
                Require(gs.Count > 0 && gs.Distinct().Count() == gs.Count && gs.All(g => g.Substring(2, 10) == s.Company?.Pan), "Enter unique registrations matching the company PAN.");
                Require(l.Branches.Length > 0 && l.Branches.DistinctBy(b => b.Id).Count() == l.Branches.Length && l.Branches.All(b => b.Id != Guid.Empty && gs.Contains(b.Gstin) && Filled(b.Name, b.Address)), "Each branch needs a name, address and registration.");
                var ids = l.Branches.Select(b => b.Id).ToHashSet();
                Require(l.Plants.DistinctBy(p => p.Id).Count() == l.Plants.Length && l.Plants.All(p => p.Id != Guid.Empty && ids.Contains(p.BranchId) && Filled(p.Name, p.Address)) && s.Series.All(x => ids.Contains(x.BranchId)), "Check plant and numbering branch assignments.");
                foreach (var branch in l.Branches.Where(b => s.Branches.All(old => old.Id != b.Id)))
                    foreach (var template in s.NumberingDefaults)
                        s.Series.Add(new(branch.Id, template.Key, "", "", template.Value));
                s.Registrations = gs;
                s.Branches = [..l.Branches];
                s.Plants = [..l.Plants];
                break;
            case 3:
                var f = data.Read<Fiscal>();
                Require(f.StartMonth is >= 1 and <= 12 && f.Series.All(x => s.Branches.Any(b => b.Id == x.BranchId)) && f.Series.DistinctBy(x => (x.BranchId, x.Document)).Count() == f.Series.Length, "Check fiscal month and series assignments.");
                foreach (var x in f.Series)
                    SetupRules.Number(x, f.StartMonth, DateOnly.FromDateTime(DateTime.UtcNow), 1);
                s.StartMonth = f.StartMonth;
                s.Series = [..f.Series];
                break;
            case 4:
                var accounts = data.Read<Account[]>();
                Require(accounts.Length > 0 && accounts.All(a => Filled(a.Name) && a.Kind is "Cash" or "Bank"), "Add a named cash or bank account.");
                s.Accounts = [..accounts];
                break;
            case 5:
                var taxes = data.Read<TaxDefault[]>();
                Require(taxes.Length > 0 && taxes.All(t => Filled(t.Name, t.Unit) && t.Rate is >= 0 and <= 100 && decimal.Round(t.Rate, 4) == t.Rate), "Check tax defaults and units.");
                s.Taxes = [..taxes];
                break;
            case 6:
                var invite = data.Read<Invite>();
                if (!string.IsNullOrWhiteSpace(invite.Email))
                {
                    Require(MailAddress.TryCreate(invite.Email, out var email) && email.Address == invite.Email, "Enter a valid email.");
                    if (!s.Invitations.Any(i => i.Email.Equals(invite.Email, StringComparison.OrdinalIgnoreCase)))
                    {
                        SetupRules.Capacity(s.Invitations.Count, 1, s.Limits.Users);
                        SetupRules.Capacity(s.Invitations.Count, 1, s.Limits.Messages);
                        s.Invitations.Add(new(invite.Email, "Accountant", DateTimeOffset.UtcNow));
                    }
                }

                Require(invite.Skip || s.Invitations.Count > 1, "Invite someone or explicitly skip.");
                s.SkipInvite = invite.Skip;
                break;
            case 7:
                var masters = data.Read<MasterSeed[]>();
                Require(masters.Length <= 10000 && masters.All(m => m.Kind is "Customer" or "Supplier" or "Site" or "Product" && Filled(m.Reference, m.Name)) && masters.DistinctBy(m => (m.Kind, m.Reference.Trim().ToUpperInvariant())).Count() == masters.Length, "Each master needs a supported kind, unique reference and name.");
                s.MasterSeeds = [..masters];
                break;
            default:
                throw new ValidationException("Unknown step.");
        }

        s.Steps.Add(step);
    }

    private static bool SeriesReady(Setup s)
    {
        if (s.StartMonth is not (>= 1 and <= 12) || s.Branches.Count == 0)
            return false;
        if (!s.Branches.All(b => SetupRules.Documents.All(d => s.Series.Any(x => x.Document == d && x.BranchId == b.Id))))
            return false;
        try
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            return s.Series.DistinctBy(x => (x.Document, SetupRules.Number(x, s.StartMonth.Value, date, 1))).Count() == s.Series.Count;
        }
        catch (ValidationException)
        {
            return false;
        }
    }

    public static Check[] Checklist(Setup s, decimal difference) => [new("Company profile", "company", s.Company != null), new("Registrations and branches", "locations", s.Registrations.Count > 0 && s.Branches.Count > 0), new("Financial year", "numbering", s.StartMonth is >= 1 and <= 12), new("Six distinct document series per branch", "numbering", SeriesReady(s)), new("Bank or cash account", "accounts", s.Accounts.Count > 0), new("Tax defaults", "taxes", s.Taxes.Count > 0), new("Additional user or explicit skip", "invites", s.SkipInvite || s.Invitations.Count > 1), new("Balanced opening trial balance", "openings", difference == 0)];
    private static bool Filled(params string[] fields) => fields.All(v => !string.IsNullOrWhiteSpace(v));
}
