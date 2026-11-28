using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;

namespace Berynx.Domain.Invoicing;

public static class M10Rules
{
    public static bool IsValidBillingMode(string? mode) =>
        mode is "PerChallan" or "Daily" or "PerOrder" or "Monthly";

    public static string ResolveBillingMode(
        string? configuredMode, string? requestedOverride, string? overrideReason)
    {
        if (!IsValidBillingMode(configuredMode))
            throw new InvalidOperationException("Configure a customer billing mode in M03 before creating an invoice.");

        if (string.IsNullOrWhiteSpace(requestedOverride))
            return configuredMode!;

        var mode = requestedOverride.Trim();
        if (!IsValidBillingMode(mode))
            throw new InvalidOperationException("Choose a supported invoice billing mode.");
        if (!string.Equals(mode, configuredMode, StringComparison.Ordinal) &&
            string.IsNullOrWhiteSpace(overrideReason))
            throw new InvalidOperationException("A reason is required when overriding the customer's billing mode.");
        if (overrideReason is { Length: > 1000 })
            throw new InvalidOperationException("Billing-mode override reason cannot exceed 1000 characters.");
        return mode;
    }

    public static void ValidateBillingGrouping(
        string mode, IReadOnlyCollection<(Guid SalesOrderId, DateOnly ChallanDate)> challans)
    {
        if (!IsValidBillingMode(mode) || challans.Count == 0)
            throw new InvalidOperationException("A valid billing mode and at least one challan are required.");

        var sameOrder = challans.Select(x => x.SalesOrderId).Distinct().Count() == 1;
        var sameDay = challans.Select(x => x.ChallanDate).Distinct().Count() == 1;
        var sameMonth = challans.Select(x => (x.ChallanDate.Year, x.ChallanDate.Month)).Distinct().Count() == 1;
        var valid = mode switch
        {
            "PerChallan" => challans.Count == 1,
            "Daily" => sameDay,
            "PerOrder" => sameOrder,
            "Monthly" => sameMonth,
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException($"Selected challans do not match the {mode} billing group.");
    }

    public static void RequireCancellablePostedInvoice(
        string status, bool hasVoucher, bool hasReceiptAllocations, bool hasIrn)
    {
        if (status != "Posted" || !hasVoucher)
            throw new InvalidOperationException("Only a posted invoice with its accounting voucher can be cancelled.");
        if (hasReceiptAllocations)
            throw new InvalidOperationException("Unallocate receipts before cancelling this invoice.");
        if (hasIrn)
            throw new InvalidOperationException("This invoice has an IRN. Complete and record portal cancellation before cancelling it here.");
    }

    public static bool IsInvoiceEligible(M09DeliveryChallan challan, M08Trip trip, Guid? invoiceId = null) =>
        challan.TripId == trip.TripId && challan.Status == "Delivered" &&
        challan.EvidenceState == "Verified" &&
        (challan.InvoiceId is null || invoiceId is not null && challan.InvoiceId == invoiceId) &&
        challan.ReceiverVerifiedAt is not null && challan.ReceiverVerifiedAt == trip.ReceiverVerifiedAt &&
        challan.ReceiverName == trip.ReceiverName &&
        challan.SignatureReference == trip.SignatureReference && challan.PhotoReference == trip.PhotoReference &&
        !string.IsNullOrWhiteSpace(challan.ReceiverName) &&
        (!string.IsNullOrWhiteSpace(challan.SignatureReference) || !string.IsNullOrWhiteSpace(challan.PhotoReference)) &&
        M08Rules.IsReceiverVerified(trip);

    public static decimal RoundOff(decimal amount) =>
        decimal.Round(amount, 0, MidpointRounding.AwayFromZero) - amount;

    public static decimal Outstanding(decimal totalAmount, decimal amountPaid)
    {
        if (totalAmount < 0 || amountPaid < 0)
            throw new InvalidOperationException("Invoice totals and posted allocations cannot be negative.");
        return Math.Max(0m, totalAmount - amountPaid);
    }

    public static string CollectionStatus(
        string invoiceStatus, DateOnly? dueDate, decimal totalAmount, decimal amountPaid, DateOnly today)
    {
        if (invoiceStatus != "Posted") return invoiceStatus;
        if (Outstanding(totalAmount, amountPaid) == 0) return "Paid";
        return dueDate is { } due && due < today ? "Overdue" : "Open";
    }

    public static void RequireInvoiceableChallan(string status, bool receiverVerified)
    {
        if (status != "Delivered" || !receiverVerified)
            throw new InvalidOperationException("Only receiver-verified Delivered challans are invoiceable.");
    }

    public static void ValidateDeliveredQuantity(decimal delivered, decimal alreadyInvoiced, decimal requested)
    {
        if (delivered < 0 || alreadyInvoiced < 0 || requested <= 0 ||
            alreadyInvoiced + requested > delivered)
            throw new InvalidOperationException("Invoice quantity cannot exceed unbilled delivered quantity.");
    }

    public static string InvoiceType(string gstRegistrationType) => gstRegistrationType switch
    {
        "Regular" => "Tax invoice",
        "Composition" => "Bill of supply",
        _ => "Unresolved"
    };
    public static void RequirePostableInvoice(string status, string invoiceType, bool disputeHold,
        decimal total, decimal taxable, decimal cgst, decimal sgst, decimal igst, decimal tcs)
    {
        if (status != "Draft") throw new InvalidOperationException("Only a draft invoice can be posted.");
        if (invoiceType is not ("Tax invoice" or "Bill of supply"))
            throw new InvalidOperationException("Invoice type is unresolved; configure branch GST registration treatment.");
        if (disputeHold) throw new InvalidOperationException("An invoice on dispute hold cannot be posted.");
        if (total <= 0 || taxable < 0 || cgst < 0 || sgst < 0 || igst < 0 || tcs < 0)
            throw new InvalidOperationException("Invoice amounts are not valid for posting.");
        if (invoiceType == "Bill of supply" && (cgst != 0 || sgst != 0 || igst != 0))
            throw new InvalidOperationException("A bill of supply cannot post output GST.");
    }
    public static string TaxTreatment(string siteState, string branchState)
    {
        if (string.IsNullOrWhiteSpace(siteState) || string.IsNullOrWhiteSpace(branchState))
            throw new InvalidOperationException("Delivery and branch GST states are required to determine place of supply.");
        return string.Equals(siteState, branchState, StringComparison.OrdinalIgnoreCase)
            ? "CGST+SGST" : "IGST";
    }
}
