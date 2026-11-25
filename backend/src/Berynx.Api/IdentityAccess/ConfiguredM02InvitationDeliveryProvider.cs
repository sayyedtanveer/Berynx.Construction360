using System.Net.Http.Headers;
using Berynx.Application.IdentityAccess;

namespace Berynx.Api.IdentityAccess;

public sealed class ConfiguredM02InvitationDeliveryProvider(
    IHttpClientFactory clients,
    IConfiguration configuration) : IM02InvitationDeliveryProvider
{
    public async Task DeliverInvitation(M02InvitationDelivery invitation, CancellationToken cancellationToken)
    {
        using var response = await Send(HttpMethod.Post, "IdentityProvider:InvitationEndpoint",
            invitation, cancellationToken, invitation.DeliveryId.ToString("N"));
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string endpointKey, object body,
        CancellationToken cancellationToken, string? idempotencyKey = null)
    {
        var endpoint = Endpoint(endpointKey);
        using var request = new HttpRequestMessage(method, endpoint) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            Required("IdentityProvider:ServiceCredential"));
        request.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString("N"));
        return await clients.CreateClient("identity-provider").SendAsync(request, cancellationToken);
    }

    private Uri Endpoint(string key)
    {
        var value = Required(key);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new M02ProviderUnavailableException($"{key} must be configured as an HTTPS URL.");
        return endpoint;
    }

    private string Required(string key) => configuration[key] is { Length: > 0 } value
        ? value
        : throw new M02ProviderUnavailableException(
            $"{key} is not configured; identity-provider functionality is unavailable.");

}
