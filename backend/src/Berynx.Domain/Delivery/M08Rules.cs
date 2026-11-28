namespace Berynx.Domain.Delivery;

public static class M08Rules
{
    private static readonly IReadOnlyDictionary<string, string[]> TripTransitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Planned"] = ["Loading", "Cancelled", "Breakdown"],
            ["Loading"] = ["Dispatched", "Cancelled", "Breakdown"],
            ["Dispatched"] = ["At site", "Cancelled", "Breakdown"],
            ["At site"] = ["Unloading", "Returned", "Rejected", "Breakdown"],
            ["Unloading"] = ["Completed", "Returned", "Rejected", "Breakdown"],
            ["Breakdown"] = ["Cancelled"],
            ["Completed"] = [],
            ["Rejected"] = [],
            ["Returned"] = [],
            ["Cancelled"] = []
        };

    private static readonly IReadOnlyDictionary<string, string[]> PourTransitions =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Scheduled"] = ["In progress", "Postponed", "Cancelled"],
            ["In progress"] = ["Completed", "Postponed", "Cancelled"],
            ["Postponed"] = ["Scheduled", "Cancelled"],
            ["Completed"] = [],
            ["Cancelled"] = []
        };

    public static bool CanTransitionTrip(string from, string to) =>
        TripTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to, StringComparer.Ordinal);

    public static void RequireTripTransition(string from, string to)
    {
        if (!CanTransitionTrip(from, to))
            throw new InvalidOperationException($"Trip transition '{from}' → '{to}' is not allowed.");
    }

    public static bool CanTransitionPour(string from, string to) =>
        PourTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to, StringComparer.Ordinal);

    public static void RequirePourTransition(string from, string to)
    {
        if (!CanTransitionPour(from, to))
            throw new InvalidOperationException($"Pour transition '{from}' → '{to}' is not allowed.");
    }

    public static IReadOnlyList<decimal> SuggestTripLoads(decimal quantity, decimal vehicleCapacity)
    {
        if (quantity <= 0 || vehicleCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity and capacity must be positive.");
        var result = new List<decimal>();
        var remaining = quantity;
        while (remaining > 0)
        {
            var load = Math.Min(remaining, vehicleCapacity);
            result.Add(decimal.Round(load, 3, MidpointRounding.AwayFromZero));
            remaining = decimal.Round(remaining - load, 3, MidpointRounding.AwayFromZero);
        }
        return result;
    }

    public static void ValidateAssignment(
        DateTimeOffset tripStart, DateTimeOffset tripEnd,
        IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> existingAssignments)
    {
        if (tripEnd <= tripStart)
            throw new InvalidOperationException("Trip end must follow trip start.");
        if (existingAssignments.Any(x => tripStart < x.End && tripEnd > x.Start))
            throw new InvalidOperationException("The vehicle or driver is already assigned to an overlapping trip.");
    }

    public static void ValidateDeliveredQuantity(
        decimal ordered, decimal alreadyDelivered, decimal tripDelivered, decimal tolerancePercent)
    {
        if (ordered <= 0 || alreadyDelivered < 0 || tripDelivered < 0 ||
            tolerancePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(tripDelivered));
        var maximum = decimal.Round(ordered * (1m + tolerancePercent / 100m), 3,
            MidpointRounding.AwayFromZero);
        if (alreadyDelivered + tripDelivered > maximum)
            throw new InvalidOperationException("Delivered quantity exceeds the order and configured tolerance.");
    }

    public static void ValidateTimestampSequence(M08Trip trip, string target, DateTimeOffset at)
    {
        if (trip.LoadedAt is { } loaded && at < loaded ||
            trip.DispatchedAt is { } dispatched && at < dispatched ||
            trip.ArrivedAt is { } arrived && at < arrived ||
            trip.UnloadingStartedAt is { } unloading && at < unloading ||
            trip.CompletedAt is { } completed && at < completed)
            throw new InvalidOperationException("Trip timestamps must remain in chronological order.");

        var previous = target switch
        {
            "Loaded" => (DateTimeOffset?)null,
            "Dispatched" => trip.LoadedAt,
            "At site" => trip.DispatchedAt,
            "Unloading" => trip.ArrivedAt,
            "Completed" => trip.UnloadingStartedAt,
            _ => at
        };
        if (target != "Loaded" && previous is null)
            throw new InvalidOperationException($"The {target} timestamp requires the preceding trip event.");
        if (previous is { } prior && at < prior)
            throw new InvalidOperationException("Trip timestamps must follow the lifecycle order.");
    }

    public static decimal PartLoadCharge(decimal loadQuantity, decimal minimumQuantity, decimal rate)
    {
        if (loadQuantity < 0 || minimumQuantity < 0 || rate < 0)
            throw new ArgumentOutOfRangeException(nameof(loadQuantity));
        return loadQuantity >= minimumQuantity ? 0m :
            decimal.Round((minimumQuantity - loadQuantity) * rate, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal WaitingCharge(
        decimal waitingMinutes, decimal freeMinutes, decimal rate, string rateUnit)
    {
        if (waitingMinutes < 0 || freeMinutes < 0 || rate < 0)
            throw new ArgumentOutOfRangeException(nameof(waitingMinutes));
        var billable = Math.Max(0m, waitingMinutes - freeMinutes);
        var units = rateUnit switch
        {
            "PerMinute" => billable,
            "PerHour" => decimal.Ceiling(billable / 60m),
            _ => throw new ArgumentException("Rate unit must be PerMinute or PerHour.", nameof(rateUnit))
        };
        return decimal.Round(units * rate, 2, MidpointRounding.AwayFromZero);
    }

    public static bool CanReviewReceiverEvidence(M08Trip trip) =>
        trip.Status == "Completed" && trip.EvidenceState == "Submitted" &&
        trip.CompletedAt is not null && !string.IsNullOrWhiteSpace(trip.ReceiverName) &&
        HasSubmittedEvidenceReference(trip) && trip.ReceiverVerifiedAt is null &&
        trip.ReceiverVerifiedByUserId is null;

    public static bool IsReceiverVerified(M08Trip trip) =>
        trip.Status == "Completed" && trip.EvidenceState == "Verified" &&
        trip.CompletedAt is not null && trip.ReceiverVerifiedAt is not null &&
        trip.ReceiverVerifiedByUserId is not null && !string.IsNullOrWhiteSpace(trip.ReceiverName) &&
        (!string.IsNullOrWhiteSpace(trip.SignatureReference) || !string.IsNullOrWhiteSpace(trip.PhotoReference));
    public static bool HasSubmittedEvidenceReference(M08Trip trip) =>
        !string.IsNullOrWhiteSpace(trip.SignatureReference) ||
        !string.IsNullOrWhiteSpace(trip.PhotoReference);
}
