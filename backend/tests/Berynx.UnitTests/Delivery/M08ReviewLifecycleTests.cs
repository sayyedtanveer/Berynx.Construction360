using Berynx.Domain.Delivery;
using NUnit.Framework;

namespace Berynx.UnitTests.Delivery;

[TestFixture]
public sealed class M08ReviewLifecycleTests
{
    private static M08Trip Submitted() => new()
    {
        Status = "Completed", EvidenceState = "Submitted",
        CompletedAt = DateTimeOffset.UtcNow, ReceiverName = "Receiver",
        SignatureReference = Guid.NewGuid().ToString("D")
    };

    [Test]
    public void Valid_completed_submitted_evidence_can_be_reviewed_once()
    {
        var trip = Submitted();
        Assert.That(M08Rules.CanReviewReceiverEvidence(trip), Is.True);
        trip.EvidenceState = "Verified";
        trip.ReceiverVerifiedAt = DateTimeOffset.UtcNow;
        trip.ReceiverVerifiedByUserId = Guid.NewGuid();
        Assert.That(M08Rules.CanReviewReceiverEvidence(trip), Is.False);
        Assert.That(M08Rules.IsReceiverVerified(trip), Is.True);
    }

    [Test]
    public void Missing_receiver_name_or_evidence_and_incomplete_trip_cannot_be_verified()
    {
        var trip = Submitted();
        trip.ReceiverName = " ";
        Assert.That(M08Rules.CanReviewReceiverEvidence(trip), Is.False);
        trip.ReceiverName = "Receiver";
        trip.SignatureReference = null;
        Assert.That(M08Rules.CanReviewReceiverEvidence(trip), Is.False);
        trip.SignatureReference = Guid.NewGuid().ToString("D");
        trip.Status = "Unloading";
        Assert.That(M08Rules.CanReviewReceiverEvidence(trip), Is.False);
    }

    [Test]
    public void Rejected_evidence_is_not_invoice_eligible_until_resubmitted_and_reviewed()
    {
        var trip = Submitted();
        trip.EvidenceState = "Rejected";
        trip.EvidenceDecisionReason = "Signature image is not legible";
        Assert.That(M08Rules.CanReviewReceiverEvidence(trip), Is.False);
        Assert.That(M08Rules.IsReceiverVerified(trip), Is.False);
        trip.EvidenceState = "Submitted";
        Assert.That(M08Rules.CanReviewReceiverEvidence(trip), Is.True);
    }
}