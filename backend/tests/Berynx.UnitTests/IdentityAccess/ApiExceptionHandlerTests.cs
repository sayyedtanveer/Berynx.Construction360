using Berynx.Api.Errors;
using NUnit.Framework;

namespace Berynx.UnitTests.IdentityAccess;

[TestFixture]
public sealed class ApiExceptionHandlerTests
{
    [TestCase(false, false, 401)]
    [TestCase(false, true, 403)]
    [TestCase(true, false, 401)]
    [TestCase(true, true, 401)]
    public void UnauthorizedStatusDistinguishesAuthenticationFromAuthorization(
        bool isAuthenticationEndpoint,
        bool isAuthenticated,
        int expectedStatus)
    {
        var status = ApiExceptionHandler.StatusCodeFor(
            new UnauthorizedAccessException(),
            isAuthenticationEndpoint,
            isAuthenticated);

        Assert.That(status, Is.EqualTo(expectedStatus));
    }
}
