using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Berynx.Api.IdentityAccess;

public sealed record M02InvitationTicket(
    Guid IdentityUserId,
    Guid TenantId,
    Guid MembershipUserId,
    Guid DeliveryId);

public sealed class M02InviteTokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(72);
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IConfiguration _configuration;

    public M02InviteTokenService(IDataProtectionProvider dataProtection, IConfiguration configuration)
    {
        _protector = dataProtection.CreateProtector("Berynx.M02.Invitation.v1")
            .ToTimeLimitedDataProtector();
        _configuration = configuration;
    }

    public string CreateUrl(M02InvitationTicket ticket, string email)
    {
        var returnUrl = _configuration["IdentityProvider:InvitationReturnUrl"];
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Invitation return URL must be configured as HTTPS.");
        var token = _protector.Protect(JsonSerializer.Serialize(ticket), Lifetime);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        query["inviteToken"] = token;
        query["email"] = email;
        return new UriBuilder(uri) { Query = query.ToString() ?? "" }.Uri.AbsoluteUri;
    }

    public M02InvitationTicket Read(string token)
    {
        try
        {
            var json = _protector.Unprotect(token, out _);
            return JsonSerializer.Deserialize<M02InvitationTicket>(json)
                ?? throw new CryptographicException("Invalid invitation ticket.");
        }
        catch (Exception error) when (error is CryptographicException or JsonException or ArgumentException)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "The invitation is invalid or expired.");
        }
    }
}
