using Berynx.Domain.Promotion;
using NUnit.Framework;

namespace Berynx.UnitTests.Promotion;

[TestFixture]
public sealed class M17RulesTests
{
    [Test]
    public void Opted_out_contact_is_never_eligible_for_promotion() =>
        Assert.That(M17Rules.CanSendPromotionalMessage(true, DateTimeOffset.UtcNow), Is.False);

    [Test]
    public void Promotional_channel_is_restricted_to_supported_channels() =>
        Assert.Throws<InvalidOperationException>(() => M17Rules.RequireChannel("push"));

    [Test]
    public void Supported_promotional_channel_is_trimmed_and_normalized() =>
        Assert.That(M17Rules.RequireChannel(" WhatsApp "), Is.EqualTo("whatsapp"));
}