using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;

namespace Berynx.IntegrationTests;

[NonParallelizable]
public sealed class FinancialEndpointAuthorizationTests
{
    private TestEnvironment environment = null!;
    private TestApplicationFactory factory = null!;
    private HttpClient client = null!;

    [SetUp]
    public void SetUp()
    {
        environment = new TestEnvironment(new Dictionary<string, string?>
        {
            ["ConnectionStrings__DefaultConnection"] =
                "Host=127.0.0.1;Port=1;Database=unused;Username=test",
        });
        factory = new TestApplicationFactory();
        client = factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        client.Dispose();
        factory.Dispose();
        environment.Dispose();
    }

    [TestCase("GET", "/api/v1/m10/invoices")]
    [TestCase("GET", "/api/v1/m11/receipt-options")]
    [TestCase("GET", "/api/v1/m12/trial-balance")]
    [TestCase("GET", "/api/v1/m08/dispatch-board")]
    [TestCase("GET", "/api/v1/m09/challans")]
    [TestCase("POST", "/api/v1/m08/trips/00000000-0000-0000-0000-000000000001/evidence-review")]
    [TestCase("POST", "/api/v1/m10/invoices/drafts")]
    [TestCase("POST", "/api/v1/m10/invoices/00000000-0000-0000-0000-000000000001/cancel")]
    [TestCase("POST", "/api/v1/m11/receipts/drafts")]
    [TestCase("POST", "/api/v1/m12/vouchers/manual")]
    public async Task Financial_routes_reject_anonymous_requests_before_database_access(
        string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            request.Content = JsonContent.Create(new { });

        using var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
