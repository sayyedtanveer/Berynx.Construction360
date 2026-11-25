using System.Net.Http.Headers;

namespace Berynx.Api.IdentityAccess;

public sealed class M02AccountEmailSender(
    IHttpClientFactory clients,
    IConfiguration configuration,
    ILogger<M02AccountEmailSender> logger)
{
    public bool IsPasswordResetConfigured =>
        Uri.TryCreate(configuration["IdentityProvider:PasswordResetEndpoint"], UriKind.Absolute,
            out var endpoint) &&
        endpoint.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(configuration["IdentityProvider:ServiceCredential"]);

    public async Task SendPasswordReset(string email, string resetUrl, CancellationToken ct)
    {
        var endpointValue = configuration["IdentityProvider:PasswordResetEndpoint"];
        var credential = configuration["IdentityProvider:ServiceCredential"];
        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(credential))
            throw new InvalidOperationException("Password reset email delivery is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new { email, resetUrl })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var response = await clients.CreateClient("identity-provider").SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Password reset email delivery failed with HTTP {Status}.",
                (int)response.StatusCode);
            throw new HttpRequestException("Password reset email delivery failed.");
        }
    }
}
