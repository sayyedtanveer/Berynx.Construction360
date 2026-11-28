using Berynx.Domain.Delivery;
using NUnit.Framework;

namespace Berynx.UnitTests.Delivery;

[TestFixture]
public sealed class M08RulesTests
{
    [TestCase("Planned", "Loading", true)]
    [TestCase("Loading", "Dispatched", true)]
    [TestCase("Dispatched", "At site", true)]
    [TestCase("Unloading", "Completed", true)]
    [TestCase("Completed", "Dispatched", false)]
    [TestCase("Planned", "Completed", false)]
    public void Trip_lifecycle_allows_only_declared_transitions(string from, string to, bool expected) =>
        Assert.That(M08Rules.CanTransitionTrip(from, to), Is.EqualTo(expected));

    [Test]
    public void Trip_loads_split_by_vehicle_capacity_with_remainder()
    {
        Assert.That(M08Rules.SuggestTripLoads(24m, 8m), Is.EqualTo(new[] { 8m, 8m, 8m }));
        Assert.That(M08Rules.SuggestTripLoads(18m, 8m), Is.EqualTo(new[] { 8m, 8m, 2m }));
    }

    [Test]
    public void Vehicle_or_driver_assignment_rejects_overlapping_time_window() =>
        Assert.Throws<InvalidOperationException>(() => M08Rules.ValidateAssignment(
            DateTimeOffset.Parse("2026-01-01T10:00:00Z"), DateTimeOffset.Parse("2026-01-01T11:00:00Z"),
            [(DateTimeOffset.Parse("2026-01-01T10:30:00Z"), DateTimeOffset.Parse("2026-01-01T11:30:00Z"))]));

    [Test]
    public void Delivery_tolerance_and_part_load_charge_use_decimal_math()
    {
        M08Rules.ValidateDeliveredQuantity(100m, 100m, 2m, 2m);
        Assert.Throws<InvalidOperationException>(() =>
            M08Rules.ValidateDeliveredQuantity(100m, 100m, 2.001m, 2m));
        Assert.That(M08Rules.PartLoadCharge(3m, 4m, 100m), Is.EqualTo(100m));
    }

    [Test]
    public void Waiting_charge_subtracts_free_minutes_before_rounding()
    {
        Assert.That(M08Rules.WaitingCharge(45m, 30m, 10m, "PerMinute"), Is.EqualTo(150m));
        Assert.That(M08Rules.WaitingCharge(91m, 30m, 60m, "PerHour"), Is.EqualTo(120m));
    }

    [Test]
    public void Trip_evidence_references_are_not_mistaken_for_receiver_verification()
    {
        Assert.That(M08Rules.HasSubmittedEvidenceReference(new M08Trip { SignatureReference = "signature/1" }), Is.True);
        Assert.That(M08Rules.HasSubmittedEvidenceReference(new M08Trip { PhotoReference = "photo/1" }), Is.True);
        Assert.That(M08Rules.HasSubmittedEvidenceReference(new M08Trip()), Is.False);
    }

    [Test]
    public void Receiver_verification_requires_completed_trip_verified_time_and_signature_or_photo()
    {
        var verified = new M08Trip
        {
            Status = "Completed", EvidenceState = "Verified", CompletedAt = DateTimeOffset.UtcNow, ReceiverVerifiedAt = DateTimeOffset.UtcNow, ReceiverVerifiedByUserId = Guid.NewGuid(),
            ReceiverName = "Receiver", SignatureReference = "secure-ref"
        };
        Assert.That(M08Rules.IsReceiverVerified(verified), Is.True);
        verified.ReceiverVerifiedAt = null;
        Assert.That(M08Rules.IsReceiverVerified(verified), Is.False);
        verified.ReceiverVerifiedAt = DateTimeOffset.UtcNow;
        verified.SignatureReference = null;
        Assert.That(M08Rules.IsReceiverVerified(verified), Is.False);
    }}
