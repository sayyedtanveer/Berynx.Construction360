using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Berynx.Api.IdentityAccess;

public sealed record M02TenantChoice(Guid TenantId, string Name);

public sealed record M02AuthResult(
    string? AccessToken,
    string? RefreshToken,
    int? ExpiresIn,
    string? ChallengeToken,
    bool RequiresTenantSelection,
    bool RequiresMfa,
    IReadOnlyList<M02TenantChoice> Tenants,
    string? ProvisioningUri = null,
    string? ManualKey = null,
    IReadOnlyList<string>? RecoveryCodes = null);

public sealed class M02AuthenticationService(
    UserManager<M02IdentityUser> users,
    BerynxDbContext challengeDb,
    DbContextOptions<BerynxDbContext> dbOptions,
    IConfiguration configuration,
    M02AccountEmailSender emailSender,
    M02InviteTokenService inviteTokens,
    IDataProtectionProvider dataProtection,
    IHttpContextAccessor http,
    ILogger<M02AuthenticationService> logger)
{
    private const string GenericLoginError = "Email or password is incorrect.";
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(14);
    private static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(5);
    private readonly IDataProtectionProvider _dataProtection = dataProtection;

    private sealed record TenantScope(Guid TenantId) : ICurrentTenantContext;
    private sealed record Membership(M02User User, Tenant Tenant);

    public async Task<M02AuthResult> Login(string email, string password, CancellationToken ct)
    {
        var normalizedEmail = email?.Trim() ?? "";
        if (normalizedEmail.Length == 0 || string.IsNullOrEmpty(password))
            throw new UnauthorizedAccessException(GenericLoginError);
        var identity = await users.FindByEmailAsync(normalizedEmail);
        if (identity is null)
        {
            logger.LogWarning("Local sign-in rejected for an unknown or ineligible account.");
            throw new UnauthorizedAccessException(GenericLoginError);
        }
        if (!identity.EmailConfirmed || string.IsNullOrEmpty(identity.PasswordHash))
        {
            await AuditIdentityEvent(identity.Id, "Login rejected", new { reason = "ineligible" }, ct);
            logger.LogWarning("Local sign-in rejected for an ineligible account.");
            throw new UnauthorizedAccessException(GenericLoginError);
        }
        if (await users.IsLockedOutAsync(identity))
        {
            await AuditIdentityEvent(identity.Id, "Login locked out", new { reason = "lockout" }, ct);
            logger.LogWarning("Local sign-in rejected for a locked account.");
            throw new UnauthorizedAccessException(GenericLoginError);
        }

        if (!await users.CheckPasswordAsync(identity, password))
        {
            EnsureIdentitySuccess(await users.AccessFailedAsync(identity));
            await AuditIdentityEvent(identity.Id, "Login failed", new
            {
                lockedOut = await users.IsLockedOutAsync(identity)
            }, ct);
            throw new UnauthorizedAccessException(GenericLoginError);
        }

        EnsureIdentitySuccess(await users.ResetAccessFailedCountAsync(identity));
        var memberships = await Memberships(identity.Id, identity.NormalizedEmail ?? "", ct);
        if (memberships.Count == 0)
            throw new UnauthorizedAccessException(GenericLoginError);

        if (memberships.Count > 1)
        {
            var challenge = await CreateChallenge(identity.Id, null, null, "TenantSelection", ct);
            return new(null, null, null, challenge, true, false,
                memberships.Select(x => new M02TenantChoice(x.User.TenantId, x.Tenant.Name)).ToArray());
        }

        var membership = memberships[0];
        if (identity.TwoFactorEnabled)
        {
            var challenge = await CreateChallenge(identity.Id, membership.User.TenantId,
                membership.User.UserId, "Mfa", ct);
            return new(null, null, null, challenge, false, true, []);
        }

        return await Issue(identity, membership, ct);
    }

    public async Task<M02AuthResult> SelectTenant(string challengeToken, Guid tenantId, CancellationToken ct)
    {
        var challenge = await ConsumeChallenge(challengeToken, "TenantSelection", ct);
        var identity = await users.FindByIdAsync(challenge.IdentityUserId.ToString());
        if (identity is null)
            throw new UnauthorizedAccessException(GenericLoginError);
        var membership = await FindMembership(identity.Id, identity.NormalizedEmail ?? "", tenantId, ct);
        if (membership is null)
        {
            await AuditIdentityEvent(identity.Id, "Tenant selection rejected",
                new { reason = "not-authorized" }, ct);
            throw new UnauthorizedAccessException(GenericLoginError);
        }
        if (identity.TwoFactorEnabled)
        {
            var token = await CreateChallenge(identity.Id, tenantId, membership.User.UserId, "Mfa", ct);
            return new(null, null, null, token, false, true, []);
        }
        return await Issue(identity, membership, ct);
    }

    public async Task<M02AuthResult> CompleteMfa(string challengeToken, string code, CancellationToken ct)
    {
        await using var transaction = await challengeDb.Database.BeginTransactionAsync(ct);
        var challenge = await FindChallenge(challengeToken, "Mfa", ct);
        var identity = await users.FindByIdAsync(challenge.IdentityUserId.ToString());
        if (identity is null || challenge.TenantId is null || challenge.MembershipUserId is null)
            throw new UnauthorizedAccessException(GenericLoginError);

        var valid = VerifyTotp(identity, code);
        if (!valid)
        {
            var recovery = await users.RedeemTwoFactorRecoveryCodeAsync(identity, code);
            valid = recovery.Succeeded;
        }

        if (!valid)
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= 5)
                challenge.ConsumedAt = DateTimeOffset.UtcNow;
            await challengeDb.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            if (challenge.TenantId is { } failedTenant && challenge.MembershipUserId is { } failedUser)
            {
                await using var auditDb = new BerynxDbContext(dbOptions, new TenantScope(failedTenant));
                AddAudit(auditDb, failedTenant, failedUser, identity.Id, "MFA failed", "Authentication",
                    failedUser, null, new { challenge.FailedAttempts });
                await auditDb.SaveChangesAsync(ct);
            }
            throw new UnauthorizedAccessException(GenericLoginError);
        }

        challenge.ConsumedAt = DateTimeOffset.UtcNow;
        await challengeDb.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var membership = await FindMembership(identity.Id, identity.NormalizedEmail ?? "",
            challenge.TenantId.Value, ct);
        if (membership is null || membership.User.UserId != challenge.MembershipUserId.Value)
            throw new UnauthorizedAccessException(GenericLoginError);
        return await Issue(identity, membership, ct);
    }

    public async Task<string?> ForgotPassword(
        string email,
        bool localDevelopmentResetEnabled,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var identity = await users.FindByEmailAsync(email.Trim());
        if (identity is null || !identity.EmailConfirmed || string.IsNullOrWhiteSpace(identity.Email))
            return null;

        var resetToken = await users.GeneratePasswordResetTokenAsync(identity);
        var isLocalFixture = identity.Email.EndsWith("@berynx.local", StringComparison.OrdinalIgnoreCase);
        var useLocalReset = localDevelopmentResetEnabled && isLocalFixture;
        var baseUrl = useLocalReset
            ? configuration["IdentityProvider:DevelopmentPasswordResetReturnUrl"]
                ?? "http://localhost:4200/login"
            : configuration["IdentityProvider:PasswordResetReturnUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var resetUri) ||
            (useLocalReset
                ? !IsAllowedLocalResetReturnUrl(resetUri)
                : resetUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException(useLocalReset
                ? "Local reset return URL must be HTTP(S) localhost on port 4200 at /login."
                : "Password reset return URL must be configured as HTTPS.");

        var query = System.Web.HttpUtility.ParseQueryString(resetUri.Query);
        query["email"] = identity.Email;
        query["token"] = resetToken;
        if (useLocalReset)
            query["view"] = "local-reset";
        resetUri = new UriBuilder(resetUri) { Query = query.ToString() ?? "" }.Uri;
        if (useLocalReset) return resetUri.AbsoluteUri;

        await emailSender.SendPasswordReset(identity.Email, resetUri.AbsoluteUri, ct);
        return null;
    }

    private static bool IsAllowedLocalResetReturnUrl(Uri uri) =>
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        uri.IsLoopback &&
        uri.Port == 4200 &&
        string.Equals(uri.AbsolutePath, "/login", StringComparison.Ordinal) &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Fragment);

    public async Task ResetPassword(string email, string token, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrEmpty(password))
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "The reset link is invalid or expired.");
        var identity = await users.FindByEmailAsync(email.Trim())
            ?? throw new System.ComponentModel.DataAnnotations.ValidationException(
                "The reset link is invalid or expired.");
        var result = await users.ResetPasswordAsync(identity, token, password);
        if (!result.Succeeded)
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "The reset link is invalid or expired.");
        identity.EmailConfirmed = true;
        EnsureIdentitySuccess(await users.UpdateAsync(identity));
        await AuditIdentityEvent(identity.Id, "Password reset completed", new { }, ct);
        await RevokeSession(Guid.Empty, identity.Id, identity.Id, true, ct);
    }

    public async Task ActivateInvitation(string token, string? password, CancellationToken ct)
    {
        var ticket = inviteTokens.Read(token);
        await using var db = new BerynxDbContext(dbOptions, new TenantScope(ticket.TenantId));
        var membership = await db.Set<M02User>().SingleOrDefaultAsync(x =>
            x.UserId == ticket.MembershipUserId && x.IdentityUserId == ticket.IdentityUserId &&
            x.InvitationDeliveryId == ticket.DeliveryId && x.Status == "Invited" &&
            x.InviteExpiresAt > DateTimeOffset.UtcNow, ct);
        var tenant = await db.Set<Tenant>().SingleOrDefaultAsync(x =>
            x.TenantId == ticket.TenantId && (x.Status == "Active" || x.Status == "Trial"), ct);
        var identity = await users.FindByIdAsync(ticket.IdentityUserId.ToString());
        if (membership is null || tenant is null || identity is null ||
            !string.Equals(identity.Email, membership.Email, StringComparison.OrdinalIgnoreCase))
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "The invitation is invalid or expired.");

        if (string.IsNullOrEmpty(identity.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Choose a password to activate this account.");
            EnsureIdentitySuccess(await users.AddPasswordAsync(identity, password));
        }
        else if (!string.IsNullOrEmpty(password))
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "This account already has a password. Sign in after accepting the invitation.");
        }

        identity.EmailConfirmed = true;
        EnsureIdentitySuccess(await users.UpdateAsync(identity));
        var before = new { membership.Status, membership.InviteExpiresAt };
        membership.Status = "Active";
        membership.AcceptedAt = DateTimeOffset.UtcNow;
        membership.InvitationDeliveryId = null;
        membership.Version++;
        AddAudit(db, ticket.TenantId, membership.UserId, identity.Id, "Invitation accepted",
            "User", membership.UserId, before, new { membership.Status, membership.AcceptedAt });
        await db.SaveChangesAsync(ct);
    }

    public async Task<M02AuthResult> Refresh(string cookieToken, CancellationToken ct)
    {
        var parts = cookieToken.Split('.', 3);
        if (parts.Length != 3 ||
            !Guid.TryParseExact(parts[0], "N", out var tenantId) ||
            !Guid.TryParseExact(parts[1], "N", out var sessionId))
            throw new UnauthorizedAccessException(GenericLoginError);

        await using var db = new BerynxDbContext(dbOptions, new TenantScope(tenantId));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var hash = Hash(cookieToken);
        var token = await db.Set<M02RefreshToken>()
            .FromSqlInterpolated($"""
                SELECT * FROM m02_refresh_token
                WHERE "TenantId"={tenantId} AND "SessionId"={sessionId} AND "TokenHash"={hash}
                FOR UPDATE
                """).SingleOrDefaultAsync(ct);
        var session = await db.Set<M02AuthSession>()
            .FromSqlInterpolated($"""
                SELECT * FROM m02_auth_session
                WHERE "TenantId"={tenantId} AND "SessionId"={sessionId}
                FOR UPDATE
                """).SingleOrDefaultAsync(ct);
        if (token is null || session is null)
            throw new UnauthorizedAccessException(GenericLoginError);

        var now = DateTimeOffset.UtcNow;
        if (token.ConsumedAt is not null || token.RevokedAt is not null ||
            token.ExpiresAt <= now || session.RevokedAt is not null || session.ExpiresAt <= now ||
            session.LastSeenAt <= now.AddMinutes(-IdleMinutes()))
        {
            session.RevokedAt ??= now;
            foreach (var active in await db.Set<M02RefreshToken>()
                         .Where(x => x.SessionId == sessionId && x.RevokedAt == null).ToListAsync(ct))
                active.RevokedAt = now;
            AddAudit(db, tenantId, session.UserId, session.IdentityUserId, "Refresh token rejected", sessionId,
                new { reason = token.ConsumedAt is null ? "expired-or-revoked" : "reuse" });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new UnauthorizedAccessException(GenericLoginError);
        }

        var membership = await db.Set<M02User>().SingleOrDefaultAsync(x =>
            x.UserId == session.UserId && x.IdentityUserId == session.IdentityUserId &&
            x.Status == "Active", ct);
        var tenant = await db.Set<Tenant>().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (membership is null || tenant is null || tenant.Status is not ("Active" or "Trial"))
        {
            session.RevokedAt = now;
            token.RevokedAt = now;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new UnauthorizedAccessException(GenericLoginError);
        }

        var identity = await users.FindByIdAsync(session.IdentityUserId.ToString());
        if (identity is null || !string.Equals(identity.SecurityStamp ?? "", session.SecurityStamp,
                StringComparison.Ordinal))
        {
            session.RevokedAt = now;
            token.RevokedAt = now;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new UnauthorizedAccessException(GenericLoginError);
        }

        token.ConsumedAt = now;
        session.LastSeenAt = now;
        session.Version++;
        var nextRefresh = NewRefreshToken(tenantId, sessionId);
        db.Add(new M02RefreshToken
        {
            TenantId = tenantId,
            TokenId = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = Hash(nextRefresh),
            CreatedAt = now,
            ExpiresAt = Min(session.ExpiresAt, now.Add(SessionLifetime))
        });
        AddAudit(db, tenantId, membership.UserId, session.IdentityUserId, "Refresh token rotated", sessionId,
            new { sessionId });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(await CreateAccessToken(identity, new Membership(membership, tenant), sessionId), nextRefresh,
            (int)AccessLifetime.TotalSeconds, null, false, false, []);
    }

    public async Task<M02AuthResult> BeginTotp(Guid identityUserId, CancellationToken ct)
    {
        var identity = await users.FindByIdAsync(identityUserId.ToString())
            ?? throw new UnauthorizedAccessException();
        var secret = RandomNumberGenerator.GetBytes(20);
        var protector = _dataProtection.CreateProtector("Berynx.M02.Totp.v1", identity.Id.ToString("N"));
        identity.TotpPendingSecretProtected = protector.Protect(Convert.ToBase64String(secret));
        var result = await users.UpdateAsync(identity);
        EnsureIdentitySuccess(result);
        var key = Base32(secret);
        var label = Uri.EscapeDataString(identity.Email ?? identity.UserName ?? "user");
        var issuer = Uri.EscapeDataString("Berynx Construction 360");
        var uri = $"otpauth://totp/{issuer}:{label}?secret={key}&issuer={issuer}&digits=6&period=30";
        return new(null, null, null, null, false, false, [], uri, key);
    }

    public async Task<IReadOnlyList<string>> ConfirmTotp(Guid identityUserId, string code, CancellationToken ct)
    {
        var identity = await users.FindByIdAsync(identityUserId.ToString())
            ?? throw new UnauthorizedAccessException();
        if (identity.TotpPendingSecretProtected is null || !VerifySecret(identity, identity.TotpPendingSecretProtected, code))
        {
            await AuditIdentityEvent(identityUserId, "MFA setup failed", new { method = "TOTP" }, ct);
            throw new System.ComponentModel.DataAnnotations.ValidationException("The authenticator code is invalid.");
        }
        identity.TotpSecretProtected = identity.TotpPendingSecretProtected;
        identity.TotpPendingSecretProtected = null;
        identity.TwoFactorEnabled = true;
        EnsureIdentitySuccess(await users.UpdateAsync(identity));
        return (await users.GenerateNewTwoFactorRecoveryCodesAsync(identity, 10) ?? []).ToArray();
    }

    public async Task RevokeSession(Guid tenantId, Guid sessionId, Guid userId, bool all, CancellationToken ct)
    {
        if (all)
        {
            List<(Guid TenantId, Guid SessionId)> sessions;
            await using (var discovery = new BerynxDbContext(dbOptions, new TenantScope(Guid.Empty)))
                sessions = (await discovery.Set<M02AuthSession>().IgnoreQueryFilters()
                    .Where(x => x.IdentityUserId == userId && x.RevokedAt == null)
                    .Select(x => new { x.TenantId, x.SessionId }).ToListAsync(ct))
                    .Select(x => (x.TenantId, x.SessionId)).ToList();
            foreach (var tenantSessions in sessions.GroupBy(x => x.TenantId))
            {
                await using var db = new BerynxDbContext(dbOptions, new TenantScope(tenantSessions.Key));
                var sessionIds = tenantSessions.Select(x => x.SessionId).ToArray();
                var active = await db.Set<M02AuthSession>()
                    .Where(x => sessionIds.Contains(x.SessionId) && x.IdentityUserId == userId &&
                                x.RevokedAt == null).ToListAsync(ct);
                var now = DateTimeOffset.UtcNow;
                foreach (var session in active)
                {
                    session.RevokedAt = now;
                    foreach (var token in await db.Set<M02RefreshToken>()
                                 .Where(x => x.SessionId == session.SessionId && x.RevokedAt == null)
                                 .ToListAsync(ct))
                        token.RevokedAt = now;
                    AddAudit(db, session.TenantId, session.UserId, session.IdentityUserId,
                        "All sessions revoked", session.SessionId, new { session.SessionId });
                }
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        await using var tenantDb = new BerynxDbContext(dbOptions, new TenantScope(tenantId));
        var own = await tenantDb.Set<M02AuthSession>().SingleOrDefaultAsync(x =>
            x.SessionId == sessionId && x.UserId == userId, ct)
            ?? throw new KeyNotFoundException();
        own.RevokedAt = DateTimeOffset.UtcNow;
        foreach (var token in await tenantDb.Set<M02RefreshToken>()
                     .Where(x => x.SessionId == sessionId && x.RevokedAt == null).ToListAsync(ct))
            token.RevokedAt = own.RevokedAt;
        AddAudit(tenantDb, tenantId, userId, own.IdentityUserId, "Session revoked", sessionId,
            new { sessionId });
        await tenantDb.SaveChangesAsync(ct);
    }

    public async Task<bool> ValidateSession(Guid tenantId, Guid identityUserId, Guid sessionId,
        CancellationToken ct)
    {
        await using var db = new BerynxDbContext(dbOptions, new TenantScope(tenantId));
        var now = DateTimeOffset.UtcNow;
        var session = await (
            from authSession in db.Set<M02AuthSession>()
            join user in db.Set<M02User>()
                on new { authSession.TenantId, authSession.UserId } equals new { user.TenantId, user.UserId }
            join tenant in db.Set<Tenant>() on authSession.TenantId equals tenant.TenantId
            where authSession.SessionId == sessionId && authSession.IdentityUserId == identityUserId &&
                  authSession.RevokedAt == null && authSession.ExpiresAt > now &&
                  authSession.LastSeenAt > now.AddMinutes(-IdleMinutes()) &&
                  user.IdentityUserId == identityUserId && user.Status == "Active" &&
                  (tenant.Status == "Active" || tenant.Status == "Trial")
            select authSession
        ).SingleOrDefaultAsync(ct);
        if (session is null)
            return false;
        var identity = await users.FindByIdAsync(identityUserId.ToString());
        return identity is not null &&
               string.Equals(identity.SecurityStamp ?? "", session.SecurityStamp, StringComparison.Ordinal);
    }

    private async Task<M02AuthResult> Issue(M02IdentityUser identity, Membership membership, CancellationToken ct)
    {
        await using var db = new BerynxDbContext(dbOptions, new TenantScope(membership.User.TenantId));
        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        var refresh = NewRefreshToken(membership.User.TenantId, sessionId);
        var accessToken = await CreateAccessToken(identity, membership, sessionId);
        var session = new M02AuthSession
        {
            TenantId = membership.User.TenantId,
            SessionId = sessionId,
            UserId = membership.User.UserId,
            IdentityUserId = identity.Id,
            SecurityStamp = identity.SecurityStamp ?? "",
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.Add(SessionLifetime),
            IpAddress = http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Device = http.HttpContext?.Request.Headers.UserAgent.ToString()
        };
        db.Add(session);
        db.Add(new M02RefreshToken
        {
            TenantId = membership.User.TenantId,
            TokenId = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = Hash(refresh),
            CreatedAt = now,
            ExpiresAt = session.ExpiresAt
        });
        AddAudit(db, session.TenantId, membership.User.UserId, identity.Id, "Login", sessionId,
            new { sessionId, session.Device, session.IpAddress });
        await db.SaveChangesAsync(ct);
        return new(accessToken, refresh,
            (int)AccessLifetime.TotalSeconds, null, false, false, []);
    }

    private async Task<string> CreateAccessToken(M02IdentityUser identity, Membership membership, Guid sessionId)
    {
        var signingKey = configuration["Jwt:SigningKey"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32 ||
            string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT issuer, audience and a 32-byte signing key must be configured.");

        var claims = new List<Claim>
        {
            new("sub", identity.Id.ToString("D")),
            new("tenant_id", membership.User.TenantId.ToString("D")),
            new("user_id", membership.User.UserId.ToString("D")),
            new("email", identity.Email ?? ""),
            new("email_verified", identity.EmailConfirmed ? "true" : "false"),
            new("sid", sessionId.ToString("D"))
        };
        if (IsConfiguredPlatformAdministrator(identity.Id))
            claims.Add(new Claim("role", "PlatformAdmin"));
        var token = new JwtSecurityToken(issuer, audience, claims,
            notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.Add(AccessLifetime),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    private bool IsConfiguredPlatformAdministrator(Guid identityUserId) =>
        configuration.GetSection("PlatformAdmin:IdentityUserIds").Get<string[]>()?
            .Any(value => Guid.TryParse(value, out var configuredId) && configuredId == identityUserId) == true;

    private async Task<List<Membership>> Memberships(Guid identityUserId, string normalizedEmail,
        CancellationToken ct)
    {
        await using var db = new BerynxDbContext(dbOptions, new TenantScope(Guid.Empty));
        var legacyMemberships = await db.Set<M02User>().IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.IdentityUserId == null && user.Email != null &&
                           user.Email.ToUpper() == normalizedEmail)
            .Select(user => new { user.TenantId, user.UserId }).ToListAsync(ct);
        foreach (var tenantMemberships in legacyMemberships.GroupBy(x => x.TenantId))
        {
            await using var tenantDb = new BerynxDbContext(dbOptions, new TenantScope(tenantMemberships.Key));
            foreach (var key in tenantMemberships)
            {
                var user = await tenantDb.Set<M02User>().SingleOrDefaultAsync(x =>
                    x.UserId == key.UserId && x.IdentityUserId == null, ct);
                if (user is null)
                    continue;
                user.IdentityUserId = identityUserId;
                user.ProviderSubject = identityUserId.ToString("D");
                user.Version++;
            }
            await tenantDb.SaveChangesAsync(ct);
        }

        return await (
            from user in db.Set<M02User>().IgnoreQueryFilters()
            join tenant in db.Set<Tenant>().IgnoreQueryFilters() on user.TenantId equals tenant.TenantId
            where user.IdentityUserId == identityUserId &&
                  user.Status == "Active" &&
                  (tenant.Status == "Active" || tenant.Status == "Trial")
            orderby tenant.Name
            select new Membership(user, tenant)
        ).ToListAsync(ct);
    }

    private async Task<Membership?> FindMembership(Guid identityUserId, string normalizedEmail, Guid tenantId,
        CancellationToken ct)
    {
        await using var db = new BerynxDbContext(dbOptions, new TenantScope(tenantId));
        var tenant = await db.Set<Tenant>().SingleOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Status != "Suspended" && x.Status != "Cancelled", ct);
        var membership = await db.Set<M02User>().SingleOrDefaultAsync(x =>
            (x.IdentityUserId == identityUserId ||
             (x.IdentityUserId == null && x.Email != null && x.Email.ToUpper() == normalizedEmail)) &&
            x.Status == "Active", ct);
        if (membership is not null && membership.IdentityUserId is null)
        {
            membership.IdentityUserId = identityUserId;
            membership.ProviderSubject = identityUserId.ToString("D");
            membership.Version++;
            await db.SaveChangesAsync(ct);
        }
        return tenant is null || membership is null ? null : new Membership(membership, tenant);
    }

    private async Task AuditIdentityEvent(Guid identityUserId, string action, object detail, CancellationToken ct)
    {
        List<(Guid TenantId, Guid UserId)> memberships;
        await using (var discovery = new BerynxDbContext(dbOptions, new TenantScope(Guid.Empty)))
            memberships = (await discovery.Set<M02User>().IgnoreQueryFilters()
                    .Where(x => x.IdentityUserId == identityUserId && x.Status != "Revoked")
                    .Select(x => new { x.TenantId, x.UserId }).ToListAsync(ct))
                .Select(x => (x.TenantId, x.UserId)).ToList();

        foreach (var tenantMembers in memberships.GroupBy(x => x.TenantId))
        {
            await using var db = new BerynxDbContext(dbOptions, new TenantScope(tenantMembers.Key));
            foreach (var membership in tenantMembers)
                AddAudit(db, membership.TenantId, membership.UserId, identityUserId, action,
                    "Authentication", membership.UserId, null, detail);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<string> CreateChallenge(Guid identityUserId, Guid? tenantId, Guid? membershipUserId,
        string purpose, CancellationToken ct)
    {
        var token = RandomToken();
        challengeDb.Add(new M02AuthChallenge
        {
            ChallengeId = Guid.NewGuid(),
            IdentityUserId = identityUserId,
            TenantId = tenantId,
            MembershipUserId = membershipUserId,
            Purpose = purpose,
            TokenHash = Hash(token),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(ChallengeLifetime)
        });
        await challengeDb.SaveChangesAsync(ct);
        return token;
    }

    private async Task<M02AuthChallenge> ConsumeChallenge(string token, string purpose, CancellationToken ct)
    {
        await using var transaction = await challengeDb.Database.BeginTransactionAsync(ct);
        var challenge = await FindChallenge(token, purpose, ct);
        challenge.ConsumedAt = DateTimeOffset.UtcNow;
        await challengeDb.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return challenge;
    }

    private async Task<M02AuthChallenge> FindChallenge(string token, string purpose, CancellationToken ct)
    {
        var hash = Hash(token);
        var challenge = await challengeDb.Set<M02AuthChallenge>()
            .FromSqlInterpolated($"""
                SELECT * FROM m02_auth_challenge
                WHERE "TokenHash"={hash} AND "Purpose"={purpose} AND "ConsumedAt" IS NULL
                  AND "ExpiresAt">{DateTimeOffset.UtcNow} AND "FailedAttempts"<5
                FOR UPDATE
                """).SingleOrDefaultAsync(ct);
        return challenge ?? throw new UnauthorizedAccessException(GenericLoginError);
    }

    private bool VerifyTotp(M02IdentityUser user, string code)
    {
        if (user.TotpSecretProtected is null)
            return false;
        return VerifySecret(user, user.TotpSecretProtected, code);
    }

    private bool VerifySecret(M02IdentityUser user, string protectedSecret, string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 6 || !code.All(char.IsAsciiDigit))
            return false;
        try
        {
            var protector = _dataProtection.CreateProtector("Berynx.M02.Totp.v1", user.Id.ToString("N"));
            var secret = Convert.FromBase64String(protector.Unprotect(protectedSecret));
            var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
            for (var offset = -1; offset <= 1; offset++)
                if (Totp(secret, counter + offset) == code)
                    return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        return false;
    }

    private string NewRefreshToken(Guid tenantId, Guid sessionId) =>
        $"{tenantId:N}.{sessionId:N}.{RandomToken()}";

    private static string RandomToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private int IdleMinutes() =>
        Math.Clamp(configuration.GetValue("Authentication:SessionIdleMinutes", 30), 5, 1440);

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left < right ? left : right;

    private static string Base32(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder();
        var buffer = 0;
        var bits = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0)
            output.Append(alphabet[(buffer << (5 - bits)) & 31]);
        return output.ToString();
    }

    private static string Totp(byte[] secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        var hash = HMACSHA1.HashData(secret, counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) |
                     (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void AddAudit(BerynxDbContext db, Guid tenantId, Guid userId, Guid identityUserId,
        string action, Guid sessionId, object after) =>
        AddAudit(db, tenantId, userId, identityUserId, action, "Session", sessionId, null, after);

    private static void AddAudit(BerynxDbContext db, Guid tenantId, Guid userId, Guid identityUserId,
        string action, string entityType, Guid? entityId, object? before, object after) =>
        db.Add(new M02AuditEvent
        {
            TenantId = tenantId,
            EventId = Guid.NewGuid(),
            ActorUserId = userId,
            ActorSubject = identityUserId.ToString("D"),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Before = System.Text.Json.JsonSerializer.Serialize(before ?? new { }),
            After = System.Text.Json.JsonSerializer.Serialize(after),
            IpAddress = null,
            At = DateTimeOffset.UtcNow
        });

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
