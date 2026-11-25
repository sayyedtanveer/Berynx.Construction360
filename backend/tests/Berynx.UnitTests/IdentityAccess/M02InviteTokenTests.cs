using Berynx.Api.IdentityAccess;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Berynx.UnitTests.IdentityAccess;

[TestFixture]
public sealed class M02InviteTokenTests
{
    [Test]
    public void InvitationUrlContainsProtectedTicketAndPreservesItsScope()
    {
        var identityId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();
        var service = CreateService("https://app.example.invalid/login");

        var url = service.CreateUrl(
            new M02InvitationTicket(identityId, tenantId, membershipId, deliveryId),
            "invitee@example.invalid");
        var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
        var ticket = service.Read(query["inviteToken"]!);

        Assert.That(query["email"], Is.EqualTo("invitee@example.invalid"));
        Assert.That(ticket.IdentityUserId, Is.EqualTo(identityId));
        Assert.That(ticket.TenantId, Is.EqualTo(tenantId));
        Assert.That(ticket.MembershipUserId, Is.EqualTo(membershipId));
        Assert.That(ticket.DeliveryId, Is.EqualTo(deliveryId));
    }

    [Test]
    public void InvitationUrlRequiresHttps()
    {
        var service = CreateService("http://app.example.invalid/login");
        Assert.Throws<InvalidOperationException>(() => service.CreateUrl(
            new M02InvitationTicket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            "invitee@example.invalid"));
    }

    [Test]
    public void TamperedInvitationTokenIsRejected()
    {
        var service = CreateService("https://app.example.invalid/login");
        var ticket = new M02InvitationTicket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var url = service.CreateUrl(ticket, "invitee@example.invalid");
        var token = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query)["inviteToken"]!;
        var tampered = token[..^1] + (token[^1] == 'A' ? "B" : "A");

        Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => service.Read(tampered));
    }

    private static M02InviteTokenService CreateService(string returnUrl)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["IdentityProvider:InvitationReturnUrl"] = returnUrl }).Build();
        var provider = new EphemeralDataProtectionProvider();
        return new M02InviteTokenService(provider, configuration);
    }
}
