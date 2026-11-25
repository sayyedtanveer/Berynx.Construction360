using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Berynx.IntegrationTests;
public class OnboardingAuthorizationTests
{
    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].ToString();
            if (string.IsNullOrWhiteSpace(role))
                return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim>
            {
                new("sub", Guid.NewGuid().ToString("D")),
                new("role", role),
                new("tenant_id", Guid.NewGuid().ToString("D"))
            };
            if (role == "PlatformAdmin")
                claims.Add(new Claim("branch_id", Guid.NewGuid().ToString("D")));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    [TestCase(null, "/api/v1/onboarding", 401)]
    [TestCase("TenantAdmin", "/api/v1/platform/tenants", 403)]
    [TestCase("PlatformAdmin", "/api/v1/openings", 403)]
    public async Task RejectsInsufficientAuthorityBeforeDatabaseAccess(string? role, string route, int expected)
    {
        using var environment = new TestEnvironment(new Dictionary<string, string?>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "Host=127.0.0.1;Port=1;Database=unused;Username=test"
        });
        using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("Test", _ => { });
            }));
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.NewGuid().ToString());
        if (role != null)
            client.DefaultRequestHeaders.Add("X-Test-Role", role);

        using var response = await client.GetAsync(route);
        Assert.That((int)response.StatusCode, Is.EqualTo(expected));
    }
}
