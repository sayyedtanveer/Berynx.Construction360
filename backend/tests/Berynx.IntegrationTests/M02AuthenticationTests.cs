using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Berynx.Api.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NUnit.Framework;

namespace Berynx.IntegrationTests;

[NonParallelizable]
public sealed class M02AuthenticationTests
{
    private sealed record Scope(Guid TenantId) : ICurrentTenantContext;

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? LastBody { get; private set; }

        public void Clear() => LastBody = null;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }

    private sealed class FixedHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private readonly Guid tenantOne = Guid.NewGuid();
    private readonly Guid tenantTwo = Guid.NewGuid();
    private readonly string schema = "m02_auth_test_" + Guid.NewGuid().ToString("N");
    private string? connection;
    private DbContextOptions<BerynxDbContext> dbOptions = null!;
    private ServiceProvider services = null!;
    private IConfigurationRoot configuration = null!;
    private CapturingHandler emailHandler = null!;

    [OneTimeSetUp]
    public async Task Prepare()
    {
        var value = Environment.GetEnvironmentVariable("BERYNX_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(value))
            Assert.Ignore("Set BERYNX_TEST_POSTGRES to a disposable local integration database.");
        var builder = new NpgsqlConnectionStringBuilder(value);
        if (builder.Host is not ("localhost" or "127.0.0.1" or "::1") ||
            builder.Database?.EndsWith("_test", StringComparison.OrdinalIgnoreCase) != true)
            throw new InvalidOperationException(
                "Authentication tests require a localhost database whose name ends in _test.");
        await using (var connectionHandle = new NpgsqlConnection(value))
        {
            await connectionHandle.OpenAsync();
            await using var create = new NpgsqlCommand("CREATE SCHEMA " + schema, connectionHandle);
            await create.ExecuteNonQueryAsync();
        }

        builder.SearchPath = schema;
        connection = value;
        dbOptions = new DbContextOptionsBuilder<BerynxDbContext>()
            .UseNpgsql(builder.ConnectionString).Options;

        emailHandler = new CapturingHandler();
        var emailClient = new HttpClient(emailHandler);
        configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            ["Jwt:Issuer"] = "berynx-auth-test",
            ["Jwt:Audience"] = "berynx-auth-test",
            ["IdentityProvider:PasswordResetEndpoint"] = "https://mail.example.invalid/password-reset",
            ["IdentityProvider:PasswordResetReturnUrl"] = "https://app.example.invalid/login",
            ["IdentityProvider:InvitationReturnUrl"] = "https://app.example.invalid/login",
            ["IdentityProvider:ServiceCredential"] = "test-only-not-a-real-credential"
        }).Build();

        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddHttpContextAccessor();
        registrations.AddSingleton<ICurrentTenantContext>(new Scope(Guid.Empty));
        registrations.AddDbContext<BerynxDbContext>(options => options.UseNpgsql(builder.ConnectionString));
        registrations.AddIdentityCore<M02IdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<BerynxDbContext>()
            .AddDefaultTokenProviders();
        registrations.AddDataProtection().UseEphemeralDataProtectionProvider();
        registrations.AddSingleton<IConfiguration>(configuration);
        registrations.AddSingleton<IHttpClientFactory>(new FixedHttpClientFactory(emailClient));
        registrations.AddScoped<M02AccountEmailSender>();
        registrations.AddScoped<M02InviteTokenService>();
        registrations.AddScoped<M02AuthenticationService>();
        services = registrations.BuildServiceProvider();

        await using var firstTenant = Open(tenantOne);
        await firstTenant.Database.MigrateAsync();
        firstTenant.Add(NewTenant(tenantOne, "Auth tenant one"));
        await firstTenant.SaveChangesAsync();
        await using var secondTenant = Open(tenantTwo);
        secondTenant.Add(NewTenant(tenantTwo, "Auth tenant two"));
        await secondTenant.SaveChangesAsync();
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        services?.Dispose();
        if (connection is null)
            return;
        await using var db = new NpgsqlConnection(connection);
        await db.OpenAsync();
        await using var drop = new NpgsqlCommand("DROP SCHEMA " + schema + " CASCADE", db);
        await drop.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task LoginRequiresOwnedTenantSelectionAndRefreshReuseRevokesTheSession()
    {
        const string email = "tenant-switch@example.invalid";
        var identity = await CreateIdentity(email, "SafePass123");
        await AddMembership(tenantOne, identity.Id, email, "Active");
        await AddMembership(tenantTwo, identity.Id, email, "Active");
        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();

        var ambiguous = await auth.Login(email, "SafePass123", CancellationToken.None);
        Assert.That(ambiguous.RequiresTenantSelection, Is.True);
        Assert.That(ambiguous.Tenants.Select(x => x.TenantId), Is.EquivalentTo(new[] { tenantOne, tenantTwo }));
        Assert.That(ambiguous.AccessToken, Is.Null);

        var selected = await auth.SelectTenant(ambiguous.ChallengeToken!, tenantOne, CancellationToken.None);
        Assert.That(selected.AccessToken, Is.Not.Null);
        var sessionId = ReadSessionId(selected.AccessToken!);
        Assert.That(await auth.ValidateSession(tenantOne, identity.Id, sessionId, CancellationToken.None), Is.True);
        Assert.That(await auth.ValidateSession(tenantTwo, identity.Id, sessionId, CancellationToken.None), Is.False);

        var forged = await auth.Login(email, "SafePass123", CancellationToken.None);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.SelectTenant(forged.ChallengeToken!, Guid.NewGuid(), CancellationToken.None));

        var rotated = await auth.Refresh(selected.RefreshToken!, CancellationToken.None);
        Assert.That(rotated.RefreshToken, Is.Not.EqualTo(selected.RefreshToken));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.Refresh(selected.RefreshToken!, CancellationToken.None));
        Assert.That(await auth.ValidateSession(tenantOne, identity.Id, sessionId, CancellationToken.None), Is.False);

    }

    [Test]
    public async Task PlatformAdministratorClaimComesOnlyFromTrustedServerConfiguration()
    {
        const string email = "platform-config@example.invalid";
        var identity = await CreateIdentity(email, "SafePass123");
        await AddMembership(tenantOne, identity.Id, email, "Active");
        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();

        var ordinaryLogin = await auth.Login(email, "SafePass123", CancellationToken.None);
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var ordinaryClaims = tokenHandler.ReadJwtToken(ordinaryLogin.AccessToken!).Claims;
        Assert.That(ordinaryClaims.Any(claim => claim.Type == "role" && claim.Value == "PlatformAdmin"), Is.False);

        configuration["PlatformAdmin:IdentityUserIds:0"] = identity.Id.ToString("D");
        var configuredLogin = await auth.Login(email, "SafePass123", CancellationToken.None);
        var configuredClaims = tokenHandler.ReadJwtToken(configuredLogin.AccessToken!).Claims;
        Assert.That(configuredClaims.Any(claim => claim.Type == "role" && claim.Value == "PlatformAdmin"), Is.True);
    }

    [Test]
    public async Task LoginRejectsDisabledMembershipAndIdentityWithoutTenantMembership()
    {
        const string disabledEmail = "disabled-membership@example.invalid";
        var disabledIdentity = await CreateIdentity(disabledEmail, "SafePass123");
        await AddMembership(tenantOne, disabledIdentity.Id, disabledEmail, "Inactive");

        const string noMembershipEmail = "no-membership@example.invalid";
        await CreateIdentity(noMembershipEmail, "SafePass123");

        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.Login(disabledEmail, "SafePass123", CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.Login(noMembershipEmail, "SafePass123", CancellationToken.None));
    }

    [Test]
    public async Task RepeatedBadPasswordsLockTheAccountAndAppendTenantSecurityAudit()
    {
        const string email = "lockout@example.invalid";
        var identity = await CreateIdentity(email, "SafePass123");
        var membership = await AddMembership(tenantOne, identity.Id, email, "Active");
        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();

        for (var attempt = 0; attempt < 5; attempt++)
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                auth.Login(email, "WrongPassword123", CancellationToken.None));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<M02IdentityUser>>();
        var lockedIdentity = await users.FindByIdAsync(identity.Id.ToString())
            ?? throw new InvalidOperationException("The test identity disappeared.");
        Assert.That(await users.IsLockedOutAsync(lockedIdentity), Is.True);
        await using var db = Open(tenantOne);
        Assert.That(await db.Set<M02AuditEvent>().CountAsync(x =>
            x.EntityId == membership.UserId && x.Action == "Login failed"), Is.EqualTo(5));
    }

    [Test]
    public async Task ExplicitLogoutRevokesTheTenantSessionAndRefreshCredential()
    {
        const string email = "logout@example.invalid";
        var identity = await CreateIdentity(email, "SafePass123");
        var membership = await AddMembership(tenantOne, identity.Id, email, "Active");
        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();
        var login = await auth.Login(email, "SafePass123", CancellationToken.None);
        var sessionId = ReadSessionId(login.AccessToken!);

        await auth.RevokeSession(tenantOne, sessionId, membership.UserId, false, CancellationToken.None);
        Assert.That(await auth.ValidateSession(tenantOne, identity.Id, sessionId, CancellationToken.None), Is.False);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.Refresh(login.RefreshToken!, CancellationToken.None));
    }

    [Test]
    public async Task InvitationActivationPasswordResetAndSessionInvalidationUseIdentity()
    {
        const string email = "invite-activate@example.invalid";
        var identity = await CreateIdentity(email, null, confirmed: false);
        var membership = await AddMembership(tenantOne, identity.Id, email, "Invited",
            deliveryId: Guid.NewGuid());
        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();
        var tokenService = scope.ServiceProvider.GetRequiredService<M02InviteTokenService>();
        var invite = tokenService.CreateUrl(new M02InvitationTicket(identity.Id, tenantOne,
            membership.UserId, membership.InvitationDeliveryId!.Value), email);
        var inviteToken = System.Web.HttpUtility.ParseQueryString(new Uri(invite).Query)["inviteToken"]!;

        await auth.ActivateInvitation(inviteToken, "InitialPass123", CancellationToken.None);
        await using (var db = Open(tenantOne))
        {
            var activated = await db.Set<M02User>().SingleAsync(x => x.UserId == membership.UserId);
            Assert.That(activated.Status, Is.EqualTo("Active"));
            Assert.That(activated.InvitationDeliveryId, Is.Null);
        }
        Assert.That((await scope.ServiceProvider.GetRequiredService<UserManager<M02IdentityUser>>()
            .FindByIdAsync(identity.Id.ToString()))!.EmailConfirmed, Is.True);

        var login = await auth.Login(email, "InitialPass123", CancellationToken.None);
        Assert.That(login.AccessToken, Is.Not.Null);
        var sessionId = ReadSessionId(login.AccessToken!);

        await auth.ForgotPassword(email, false, CancellationToken.None);
        Assert.That(emailHandler.LastBody, Is.Not.Null);
        using var mail = JsonDocument.Parse(emailHandler.LastBody!);
        var resetUrl = mail.RootElement.GetProperty("resetUrl").GetString()!;
        var resetQuery = System.Web.HttpUtility.ParseQueryString(new Uri(resetUrl).Query);
        await auth.ResetPassword(email, resetQuery["token"]!, "ReplacementPass456", CancellationToken.None);

        Assert.That(await auth.ValidateSession(tenantOne, identity.Id, sessionId, CancellationToken.None), Is.False);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.Login(email, "InitialPass123", CancellationToken.None));
        Assert.That((await auth.Login(email, "ReplacementPass456", CancellationToken.None)).AccessToken,
            Is.Not.Null);
    }

    [Test]
    public async Task LocalDevelopmentForgotPasswordReturnsResetUrlWithoutSendingEmail()
    {
        const string email = "platform.admin@berynx.local";
        var identity = await CreateIdentity(email, "SafePass123");
        await AddMembership(tenantOne, identity.Id, email, "Active");
        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();
        emailHandler.Clear();

        var resetUrl = await auth.ForgotPassword(email, true, CancellationToken.None);

        Assert.That(resetUrl, Does.StartWith("http://localhost:4200/login?"));
        Assert.That(resetUrl, Does.Contain(Uri.EscapeDataString(email)));
        Assert.That(resetUrl, Does.Contain("view=local-reset"));
        Assert.That(emailHandler.LastBody, Is.Null);
    }

    [Test]
    public async Task TotpRequiresChallengeAndIssuesSingleUseRecoveryCodes()
    {
        const string email = "mfa@example.invalid";
        var identity = await CreateIdentity(email, "SafePass123");
        await AddMembership(tenantOne, identity.Id, email, "Active");
        using var scope = services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<M02AuthenticationService>();

        var setup = await auth.BeginTotp(identity.Id, CancellationToken.None);
        Assert.That(setup.ProvisioningUri, Does.StartWith("otpauth://totp/"));
        Assert.That(setup.ManualKey, Is.Not.Empty);
        var recoveryCodes = await auth.ConfirmTotp(identity.Id, CurrentTotp(setup.ManualKey!),
            CancellationToken.None);
        Assert.That(recoveryCodes, Has.Count.EqualTo(10));

        var challenge = await auth.Login(email, "SafePass123", CancellationToken.None);
        Assert.That(challenge.RequiresMfa, Is.True);
        Assert.That(challenge.AccessToken, Is.Null);
        var completed = await auth.CompleteMfa(challenge.ChallengeToken!, recoveryCodes[0],
            CancellationToken.None);
        Assert.That(completed.AccessToken, Is.Not.Null);

        var secondChallenge = await auth.Login(email, "SafePass123", CancellationToken.None);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            auth.CompleteMfa(secondChallenge.ChallengeToken!, recoveryCodes[0], CancellationToken.None));
    }

    private BerynxDbContext Open(Guid tenantId) => new(dbOptions, new Scope(tenantId));

    private static Tenant NewTenant(Guid id, string name) => new()
    {
        TenantId = id,
        Name = name,
        Plan = "Test",
        Status = "Active",
        Configuration = OnboardingStore.Encode(new Setup())
    };

    private async Task<M02IdentityUser> CreateIdentity(string email, string? password, bool confirmed = true)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<M02IdentityUser>>();
        var identity = new M02IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = confirmed,
            LockoutEnabled = true
        };
        var result = password is null
            ? await users.CreateAsync(identity)
            : await users.CreateAsync(identity, password);
        Assert.That(result.Succeeded, Is.True, string.Join("; ", result.Errors.Select(x => x.Description)));
        return identity;
    }

    private async Task<M02User> AddMembership(Guid tenantId, Guid identityId, string email, string status,
        Guid? deliveryId = null)
    {
        var user = new M02User
        {
            TenantId = tenantId,
            UserId = Guid.NewGuid(),
            IdentityUserId = identityId,
            ProviderSubject = identityId.ToString("D"),
            Email = email,
            DisplayName = email,
            Status = status,
            InvitationDeliveryId = deliveryId,
            InviteExpiresAt = status == "Invited" ? DateTimeOffset.UtcNow.AddHours(72) : null,
            InvitationAvailableAt = DateTimeOffset.UtcNow,
            Version = 1
        };
        await using var db = Open(tenantId);
        db.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static Guid ReadSessionId(string token)
    {
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        return Guid.Parse(jwt.Claims.Single(x => x.Type == "sid").Value);
    }

    private static string CurrentTotp(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var secret = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in base32)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8)
            {
                secret.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var hash = HMACSHA1.HashData(secret.ToArray(), counter);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) |
                     (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }
}
