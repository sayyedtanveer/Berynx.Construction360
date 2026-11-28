using Berynx.Domain.Delivery;
using Berynx.Domain.DeliveryChallans;
using Berynx.Domain.Invoicing;
using NUnit.Framework;

namespace Berynx.UnitTests.Invoicing;

[TestFixture]
public sealed class M10EvidenceEligibilityTests
{
    [Test]
    public void Invoice_requires_delivered_challan_and_verified_persisted_trip_facts()
    {
        var tripId = Guid.NewGuid();
        var verifiedAt = DateTimeOffset.UtcNow;
        var reviewerId = Guid.NewGuid();
        var trip = new M08Trip
        {
            TripId = tripId, Status = "Completed", EvidenceState = "Verified",
            CompletedAt = verifiedAt.AddMinutes(-5), ReceiverName = "Receiver",
            SignatureReference = Guid.NewGuid().ToString("D"),
            ReceiverVerifiedAt = verifiedAt, ReceiverVerifiedByUserId = reviewerId
        };
        var challan = new M09DeliveryChallan
        {
            TripId = tripId, Status = "Delivered", EvidenceState = "Verified",
            ReceiverName = "Receiver", SignatureReference = trip.SignatureReference,
            ReceiverVerifiedAt = verifiedAt
        };
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip), Is.True);

        trip.EvidenceState = "Rejected";
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip), Is.False);
        trip.EvidenceState = "Verified";
        trip.ReceiverVerifiedByUserId = null;
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip), Is.False);
        trip.ReceiverVerifiedByUserId = reviewerId;
        challan.Status = "Unverified";
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip), Is.False);
    }

    [Test]
    public void Requires_exact_challan_snapshot_and_allows_only_its_reserved_invoice()
    {
        var tripId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var verifiedAt = DateTimeOffset.UtcNow;
        var signature = Guid.NewGuid().ToString("D");
        var photo = Guid.NewGuid().ToString("D");
        var reviewerId = Guid.NewGuid();
        var trip = new M08Trip
        {
            TripId = tripId, Status = "Completed", EvidenceState = "Verified",
            CompletedAt = verifiedAt.AddMinutes(-5), ReceiverName = "Receiver",
            SignatureReference = signature, PhotoReference = photo,
            ReceiverVerifiedAt = verifiedAt, ReceiverVerifiedByUserId = reviewerId
        };
        var challan = new M09DeliveryChallan
        {
            TripId = tripId, Status = "Delivered", EvidenceState = "Verified",
            ReceiverName = trip.ReceiverName, SignatureReference = signature,
            PhotoReference = photo, ReceiverVerifiedAt = verifiedAt
        };
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip), Is.True);

        challan.PhotoReference = Guid.NewGuid().ToString("D");
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip), Is.False);
        challan.PhotoReference = photo;
        challan.InvoiceId = invoiceId;
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip), Is.False);
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip, invoiceId), Is.True);
        Assert.That(M10Rules.IsInvoiceEligible(challan, trip, Guid.NewGuid()), Is.False);
    }}