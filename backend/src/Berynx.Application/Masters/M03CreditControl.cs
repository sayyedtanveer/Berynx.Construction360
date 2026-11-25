using System.Text.RegularExpressions;
using Berynx.Domain.Onboarding;

namespace Berynx.Application.Masters;

public sealed record M03CreditProfile(
    Guid PartyId,
    decimal CreditLimit,
    int CreditDays,
    string CreditPolicy,
    string CreditStatus);

/// <summary>
/// Facts must come from persisted accounting/sales transactions. M03 deliberately has
/// no fallback that estimates exposure when the source module is unavailable.
/// </summary>
public sealed record M03CreditFacts(
    bool Available,
    decimal? Outstanding,
    decimal? Overdue,
    int? OldestOverdueDays,
    string? UnavailableReason = null)
{
    /// <summary>Confirmed, undelivered sales-order value from the transaction source.</summary>
    public decimal? UndeliveredConfirmedOrders { get; init; }

    /// <summary>Unallocated customer advances from the receipt/accounting source.</summary>
    public decimal? UnallocatedAdvances { get; init; }
}

public interface IM03CreditFactsProvider
{
    Task<M03CreditFacts> GetFacts(Guid customerPartyId, CancellationToken cancellationToken);
}

public interface IM03CreditProfileProvider
{
    Task<M03CreditProfile?> GetProfile(Guid customerPartyId, CancellationToken cancellationToken);
}

public sealed record M03CreditDecision(
    string Status,
    IReadOnlyList<string> Reasons,
    decimal? Outstanding,
    decimal? ProjectedExposure,
    decimal? Overdue,
    bool OverridePermissionRequired,
    bool CanRequestOverride);

public sealed record M03CreditAssessment(
    M03CreditProfile Profile,
    M03CreditFacts Facts,
    M03CreditDecision Decision);

public sealed class M03CreditEvaluationService(
    IM03CreditProfileProvider profiles,
    IM03CreditFactsProvider factsProvider)
{
    public async Task<M03CreditAssessment> Evaluate(
        Guid customerPartyId,
        decimal proposedAmount,
        bool hasOverridePermission,
        CancellationToken cancellationToken)
    {
        var profile = await profiles.GetProfile(customerPartyId, cancellationToken)
            ?? throw new KeyNotFoundException("Customer was not found.");
        var facts = await factsProvider.GetFacts(customerPartyId, cancellationToken);
        return new M03CreditAssessment(
            profile, facts, M03CreditControl.Evaluate(profile, facts, proposedAmount, hasOverridePermission));
    }
}

public static class M03CreditControl
{
    public static M03CreditDecision Evaluate(
        M03CreditProfile profile,
        M03CreditFacts facts,
        decimal proposedAmount,
        bool hasOverridePermission)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(facts);
        if (proposedAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(proposedAmount), "Proposed amount cannot be negative.");
        if (profile.CreditLimit < 0 || profile.CreditDays < 0)
            throw new ArgumentException("Credit terms must be non-negative.", nameof(profile));

        decimal? currentExposure = null;
        if (facts.Available)
        {
            if (facts.Outstanding is null || facts.Overdue is null ||
                facts.UndeliveredConfirmedOrders is null || facts.UnallocatedAdvances is null)
                throw new ArgumentException(
                    "Available credit facts must include invoice, confirmed-order, advance and overdue amounts.", nameof(facts));
            if (facts.Outstanding < 0 || facts.Overdue < 0 ||
                facts.UndeliveredConfirmedOrders < 0 || facts.UnallocatedAdvances < 0 ||
                facts.OldestOverdueDays is < 0)
                throw new ArgumentException("Credit facts cannot contain negative exposure or age.", nameof(facts));
            currentExposure = checked(facts.Outstanding.Value + facts.UndeliveredConfirmedOrders.Value -
                                      facts.UnallocatedAdvances.Value);
        }

        var reasons = new List<string>();
        if (profile.CreditStatus is "OnHold" or "Blocked")
        {
            reasons.Add($"Customer credit status is {profile.CreditStatus}.");
            return new M03CreditDecision(
                "Blocked", reasons, facts.Available ? facts.Outstanding : null,
                currentExposure is null ? null : checked(currentExposure.Value + proposedAmount),
                facts.Available ? facts.Overdue : null, true,
                hasOverridePermission && facts.Available);
        }

        if (!facts.Available)
        {
            var strictPolicy = profile.CreditPolicy is "SoftBlock" or "HardBlock";
            reasons.Add(facts.UnavailableReason ?? "Transaction-backed credit exposure is not available.");
            return new M03CreditDecision(
                strictPolicy ? "Blocked" : "Warning", reasons, null, null, null,
                strictPolicy, false);
        }

        var outstanding = facts.Outstanding.GetValueOrDefault();
        var projected = checked(currentExposure!.Value + proposedAmount);
        var breachesLimit = projected > profile.CreditLimit;
        var breachesDays = facts.Overdue > 0 &&
                           facts.OldestOverdueDays is { } age && age > profile.CreditDays;
        var overdueAgeUnknown = facts.Overdue > 0 && facts.OldestOverdueDays is null;
        if (breachesLimit)
            reasons.Add("Projected exposure exceeds the customer's credit limit.");
        if (breachesDays)
            reasons.Add("An open receivable is older than the customer's credit days.");
        if (overdueAgeUnknown)
            reasons.Add("Overdue receivable age is unavailable.");

        if (reasons.Count == 0)
            return new M03CreditDecision(
                "Allowed", reasons, outstanding, projected, facts.Overdue, false, false);

        var status = profile.CreditPolicy switch
        {
            "HardBlock" => "Blocked",
            "SoftBlock" => "ApprovalRequired",
            _ => "Warning"
        };
        var overrideRequired = status is "Blocked" or "ApprovalRequired";
        return new M03CreditDecision(
            status, reasons, outstanding, projected, facts.Overdue,
            overrideRequired, overrideRequired && hasOverridePermission);
    }
}

/// <summary>
/// Immutable tax inputs/results for future posted transactions. Consumers persist this
/// snapshot with their own document; M03 never creates or edits posted documents.
/// </summary>
public sealed record M03TaxSnapshot(
    Guid ProductId,
    string HsnCode,
    string SellerGstin,
    string? BuyerGstin,
    string PlaceOfSupplyStateCode,
    DateOnly EffectiveOn,
    string RateSource,
    decimal TaxableAmount,
    decimal TotalRatePercent,
    decimal CgstRatePercent,
    decimal SgstRatePercent,
    decimal IgstRatePercent,
    DateTimeOffset CapturedAt,
    Guid? SourceRateId)
{
    public static M03TaxSnapshot Create(
        Guid productId,
        string hsnCode,
        string sellerGstin,
        string? buyerGstin,
        string placeOfSupplyStateCode,
        DateOnly effectiveOn,
        string rateSource,
        decimal taxableAmount,
        decimal totalRatePercent,
        decimal cgstRatePercent,
        decimal sgstRatePercent,
        decimal igstRatePercent,
        Guid? sourceRateId,
        DateTimeOffset? capturedAt = null)
    {
        if (productId == Guid.Empty || string.IsNullOrWhiteSpace(hsnCode) ||
            string.IsNullOrWhiteSpace(sellerGstin) ||
            string.IsNullOrWhiteSpace(placeOfSupplyStateCode) ||
            string.IsNullOrWhiteSpace(rateSource))
            throw new ArgumentException("Tax snapshot identity and place-of-supply fields are required.");
        var normalizedSellerGstin = SetupRules.Gstin(sellerGstin);
        var normalizedBuyerGstin = string.IsNullOrWhiteSpace(buyerGstin)
            ? null
            : SetupRules.Gstin(buyerGstin);
        var placeOfSupply = placeOfSupplyStateCode.Trim();
        if (!Regex.IsMatch(placeOfSupply, "^[0-9]{2}$"))
            throw new ArgumentException("Place of supply must be a two-digit GST state code.", nameof(placeOfSupplyStateCode));
        if (normalizedBuyerGstin is not null && normalizedBuyerGstin[..2] != placeOfSupply)
            throw new ArgumentException("Place of supply must match the buyer GSTIN state code.", nameof(placeOfSupplyStateCode));
        if (taxableAmount < 0 || totalRatePercent < 0 ||
            cgstRatePercent < 0 || sgstRatePercent < 0 || igstRatePercent < 0)
            throw new ArgumentOutOfRangeException(nameof(taxableAmount), "Tax amounts and rates cannot be negative.");
        if (cgstRatePercent + sgstRatePercent + igstRatePercent != totalRatePercent)
            throw new ArgumentException("The component tax rates must equal the resolved total rate.");
        if (normalizedSellerGstin[..2] == placeOfSupply)
        {
            if (cgstRatePercent != sgstRatePercent || igstRatePercent != 0)
                throw new ArgumentException("Intra-state tax must use equal CGST/SGST components and no IGST.");
        }
        else if (cgstRatePercent != 0 || sgstRatePercent != 0 || igstRatePercent != totalRatePercent)
        {
            throw new ArgumentException("Inter-state tax must use IGST only.");
        }

        return new M03TaxSnapshot(
            productId, hsnCode.Trim(), normalizedSellerGstin, normalizedBuyerGstin,
            placeOfSupply, effectiveOn, rateSource.Trim(), taxableAmount,
            totalRatePercent, cgstRatePercent, sgstRatePercent, igstRatePercent,
            capturedAt ?? DateTimeOffset.UtcNow, sourceRateId);
    }
}
