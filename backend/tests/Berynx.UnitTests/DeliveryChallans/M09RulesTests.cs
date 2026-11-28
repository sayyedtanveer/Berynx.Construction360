using Berynx.Domain.DeliveryChallans;
using NUnit.Framework;

namespace Berynx.UnitTests.DeliveryChallans;

[TestFixture]
public sealed class M09RulesTests
{
    [TestCase("Unverified", "Delivered")]
    [TestCase("Delivered", "Invoiced")]
    [TestCase("Delivered", "Cancelled")]
    public void Allows_only_documented_challan_transitions(string from, string to) =>
        Assert.DoesNotThrow(() => M09Rules.RequireTransition(from, to));

    [TestCase("Invoiced", "Cancelled")]
    [TestCase("Cancelled", "Delivered")]
    public void Rejects_locked_or_reversed_challan_transitions(string from, string to) =>
        Assert.Throws<InvalidOperationException>(() => M09Rules.RequireTransition(from, to));

    [Test]
    public void Correction_requires_reason_and_positive_quantity() =>
        Assert.Throws<ArgumentException>(() => M09Rules.ValidateCorrection(false, 10m, 0m, ""));

    [Test]
    public void Invoiced_challan_is_never_corrected_in_place() =>
        Assert.Throws<InvalidOperationException>(() => M09Rules.ValidateCorrection(true, 10m, 9m, "Correction"));

    [Test]
    public void Challan_is_delivered_only_for_persisted_receiver_verification() =>
        Assert.That(M09Rules.StatusForReceiverVerification(false), Is.EqualTo("Unverified"));
        }
