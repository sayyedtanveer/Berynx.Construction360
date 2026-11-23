using System.Security.Claims;
using Berynx.Api.Auth;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;

namespace Berynx.UnitTests.Auth;

public sealed class HttpCurrentTenantContextTests
{
    [Test]
    public void TenantId_UsesTenantClaimFromAuthenticatedPrincipal()
    {
        var expectedTenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("tenant_id", expectedTenantId.ToString())],
                    authenticationType: "test-authentication")),
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(value => value.HttpContext).Returns(httpContext);

        var tenantContext = new HttpCurrentTenantContext(accessor.Object);

        Assert.That(tenantContext.TenantId, Is.EqualTo(expectedTenantId));
    }

    [Test]
    public void TenantId_RejectsUnauthenticatedPrincipalEvenWhenClaimIsPresent()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("tenant_id", Guid.NewGuid().ToString())])),
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(value => value.HttpContext).Returns(httpContext);

        var tenantContext = new HttpCurrentTenantContext(accessor.Object);

        Assert.Throws<UnauthorizedAccessException>(() => _ = tenantContext.TenantId);
    }

    [Test]
    public void TenantId_RejectsMissingTenantClaim()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-1")],
                authenticationType: "test-authentication")),
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(value => value.HttpContext).Returns(httpContext);

        var tenantContext = new HttpCurrentTenantContext(accessor.Object);

        Assert.Throws<UnauthorizedAccessException>(() => _ = tenantContext.TenantId);
    }
}
