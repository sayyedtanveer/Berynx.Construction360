using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Berynx.IntegrationTests;

public sealed class HealthEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private TestEnvironment _environment = null!;

    [SetUp]
    public void SetUp()
    {
        _environment = new TestEnvironment(new Dictionary<string, string?>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "Host=127.0.0.1;Port=1;Database=unused;Username=test",
        });
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        _environment.Dispose();
    }

    [Test]
    public async Task HealthEndpoint_ReturnsHealthyWithoutDatabaseConnection()
    {
        using var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(
            await response.Content.ReadAsStringAsync(),
            Does.Contain("Healthy"));
    }
}
