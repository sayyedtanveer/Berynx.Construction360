using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Berynx.Api.Onboarding;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.IdentityAccess;

public static class M02Endpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public sealed record LoginRequest(string Email, string Password);
    public sealed record TenantSelectionRequest(string ChallengeToken, Guid TenantId);
    public sealed record MfaLoginRequest(string ChallengeToken, string Code);
    public sealed record MfaSetupChallengeRequest(string ChallengeToken);
    public sealed record MfaSetupConfirmRequest(string ChallengeToken, string Code);
    public sealed record SecurityPolicyWrite(
        bool RequireMfaForTenantAdmins, bool RequireMfaForAccounts, long Version);
    public sealed record ForgotPasswordRequest(string Email);
    public sealed record ResetPasswordRequest(string Email, string Token, string Password);
    public sealed record InvitationActivationRequest(string Token, string? Password);
    public sealed record InviteRequest(string? Email, string? Mobile, string DisplayName, RoleGrant[] Roles);
    public sealed record RoleGrant(string RoleKey, string ScopeType, Guid? ScopeId);
    public sealed record UserRolesRequest(RoleGrant[] Roles, long Version);
    public sealed record UserDeactivationRequest(long Version, M02TaskReassignment[] Reassignments);
    public sealed record UserNameRequest(string DisplayName, long Version);
    public sealed record RoleWrite(string Name, string Description, string[] Permissions, long Version);
    public sealed record RuleLevelWrite(int Level, Guid? ApproverRoleId, Guid? ApproverUserId,
        int SlaMinutes, Guid? EscalationRoleId, Guid? EscalationUserId);
    public sealed record RuleWrite(string Name, string EntityType, decimal? AmountGreaterThan,
        decimal? DiscountPercentGreaterThan, decimal? MarginPercentBelow, string? CreditStatus,
        bool Enabled, RuleLevelWrite[] Levels, long Version);
    public sealed record ApprovalDecisionRequest(string Decision, string? Comment, long Version);
    public sealed record DelegationRequest(Guid DelegateUserId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
    public sealed record ProfileWrite(string DisplayName);
    public sealed record SessionRevoke(string SessionId);

    public static void MapM02(this WebApplication app)
    {
        var auth = app.MapGroup("/api/v1/m02/auth");
        auth.RequireRateLimiting("m02-auth");
        auth.MapPost("/login", async (LoginRequest request, M02AuthenticationService authentication,
            HttpContext context, CancellationToken ct) =>
                AuthResponse(context, await authentication.Login(request.Email, request.Password, ct)))
            .AllowAnonymous();
        auth.MapPost("/tenant-selection", async (TenantSelectionRequest request,
            M02AuthenticationService authentication, HttpContext context, CancellationToken ct) =>
                AuthResponse(context, await authentication.SelectTenant(
                    request.ChallengeToken, request.TenantId, ct)))
            .AllowAnonymous();
        auth.MapPost("/mfa", async (MfaLoginRequest request, M02AuthenticationService authentication,
            HttpContext context, CancellationToken ct) =>
                AuthResponse(context, await authentication.CompleteMfa(
                    request.ChallengeToken, request.Code, ct)))
            .AllowAnonymous();
        auth.MapPost("/mfa/setup/begin", async (MfaSetupChallengeRequest request,
            M02AuthenticationService authentication, CancellationToken ct) =>
            {
                var setup = await authentication.BeginRequiredMfaSetup(request.ChallengeToken, ct);
                return Results.Ok(new { setup.ProvisioningUri, setup.ManualKey });
            }).AllowAnonymous();
        auth.MapPost("/mfa/setup/confirm", async (MfaSetupConfirmRequest request,
            M02AuthenticationService authentication, HttpContext context, CancellationToken ct) =>
                AuthResponse(context, await authentication.CompleteRequiredMfaSetup(
                    request.ChallengeToken, request.Code, ct)))
            .AllowAnonymous();
        auth.MapPost("/refresh", async (HttpContext context, M02AuthenticationService authentication,
            CancellationToken ct) =>
            {
                var refresh = context.Request.Cookies["m02_refresh"];
                if (string.IsNullOrWhiteSpace(refresh))
                    throw new UnauthorizedAccessException();
                try
                {
                    return AuthResponse(context, await authentication.Refresh(refresh, ct));
                }
                catch (UnauthorizedAccessException)
                {
                    ClearRefreshCookie(context);
                    throw;
                }
            }).AllowAnonymous();
        auth.MapPost("/password/forgot", async (ForgotPasswordRequest request,
            M02AuthenticationService authentication, M02AccountEmailSender emailSender,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment environment, IConfiguration configuration,
            HttpContext context, CancellationToken ct) =>
            {
                string? developmentResetUrl = null;
                var localDevelopmentResetEnabled =
                    environment.IsDevelopment() &&
                    configuration["BERYNX_ENABLE_LOCAL_PASSWORD_RESET"] == "1" &&
                    context.Connection.RemoteIpAddress is { } remoteAddress &&
                    System.Net.IPAddress.IsLoopback(remoteAddress);
                var deliveryStatus = "unconfirmed";
                var message =
                    "If the account is eligible, reset instructions are requested through the configured email channel. Delivery is not confirmed here; contact your administrator if they do not arrive.";
                if (localDevelopmentResetEnabled)
                {
                    deliveryStatus = "local-pending";
                    message = "No email is sent by the local reset flow. If the account is eligible, a one-time local reset link will be shown.";
                }
                else if (!emailSender.IsPasswordResetConfigured)
                {
                    deliveryStatus = "unavailable";
                    message = "Password reset email delivery is not configured. Contact your administrator.";
                }
                try
                {
                    developmentResetUrl = await authentication.ForgotPassword(
                        request.Email, localDevelopmentResetEnabled, ct);
                    if (developmentResetUrl is not null)
                    {
                        deliveryStatus = "local-link";
                        message = "A local development reset link is ready. No email was sent.";
                        context.Response.Headers.CacheControl = "no-store";
                    }
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    loggerFactory.CreateLogger("M02PasswordReset").LogWarning(
                        "Password reset request could not be delivered ({ExceptionType}).",
                        error.GetType().Name);
                    if (!localDevelopmentResetEnabled)
                    {
                        deliveryStatus = "unconfirmed";
                        message =
                            "If the account is eligible, a reset request was processed, but delivery could not be confirmed. Contact your administrator if instructions do not arrive.";
                    }
                    else
                    {
                        deliveryStatus = "unavailable";
                        message = "The local development reset flow is unavailable. No email was sent.";
                    }
                }
                return Results.Ok(new
                {
                    message,
                    deliveryStatus,
                    developmentResetUrl
                });
            }).AllowAnonymous();
        auth.MapPost("/password/reset", async (ResetPasswordRequest request,
            M02AuthenticationService authentication, CancellationToken ct) =>
            {
                await authentication.ResetPassword(request.Email, request.Token, request.Password, ct);
                return Results.NoContent();
            }).AllowAnonymous();
        auth.MapPost("/invitation/activate", async (InvitationActivationRequest request,
            M02AuthenticationService authentication, CancellationToken ct) =>
            {
                await authentication.ActivateInvitation(request.Token, request.Password, ct);
                return Results.NoContent();
            }).AllowAnonymous();
        auth.MapGet("/refresh-state", (HttpContext context) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Results.Ok(new
                {
                    hasRefreshSession = !string.IsNullOrWhiteSpace(context.Request.Cookies["m02_refresh"])
                });
            }).AllowAnonymous();

        var api = app.MapGroup("/api/v1/m02")
            .RequireAuthorization(p => p.RequireClaim("sub").RequireClaim("tenant_id"));
        api.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<TenantAuthorizationService>();
            var path = context.HttpContext.Request.Path.Value;
            await authorization.RequireModule(
                "M02",
                !HttpMethods.IsGet(context.HttpContext.Request.Method),
                path == "/api/v1/onboarding/export",
                context.HttpContext.RequestAborted);
            return await next(context);
        });

        api.MapGet("/security-policy", async (IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            await RequireTenantWide(access, "users.view", ct);
            var row = await db.Set<Tenant>().SingleAsync(x => x.TenantId == tenant.TenantId, ct);
            var policy = OnboardingStore.Decode(row).Security;
            return Results.Ok(new
            {
                policy.RequireMfaForTenantAdmins,
                policy.RequireMfaForAccounts,
                row.Version
            });
        });
        api.MapPut("/security-policy", async (SecurityPolicyWrite request, IM02Access access,
            BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            await RequireTenantWide(access, "users.manage", ct);
            var actor = await access.CurrentUser(ct);
            var row = await db.Set<Tenant>().SingleAsync(x => x.TenantId == tenant.TenantId, ct);
            if (row.Version != request.Version)
                return Results.Conflict(new { detail = "Tenant settings changed. Reload before saving." });
            var setup = OnboardingStore.Decode(row);
            var before = new
            {
                setup.Security.RequireMfaForTenantAdmins,
                setup.Security.RequireMfaForAccounts
            };
            setup.Security.RequireMfaForTenantAdmins = request.RequireMfaForTenantAdmins;
            setup.Security.RequireMfaForAccounts = request.RequireMfaForAccounts;
            row.Configuration = OnboardingStore.Encode(setup);
            row.Version++;
            Audit(db, tenant.TenantId, actor, "Tenant MFA policy changed", "Tenant",
                tenant.TenantId, before, setup.Security);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new
            {
                setup.Security.RequireMfaForTenantAdmins,
                setup.Security.RequireMfaForAccounts,
                row.Version
            });
        });

        api.MapGet("/users", async (IM02Access access, BerynxDbContext db, CancellationToken ct) =>
        {
            await RequireTenantWide(access, "users.view", ct);
            var users = await db.Set<M02User>().OrderBy(x => x.DisplayName).ToListAsync(ct);
            var roleRows = await (
                from assignment in db.Set<M02UserRole>()
                join role in db.Set<M02Role>()
                    on new { assignment.TenantId, assignment.RoleId } equals new { role.TenantId, role.RoleId }
                select new { assignment.UserId, role.RoleKey, role.Name, assignment.ScopeType, assignment.ScopeId }
            ).ToListAsync(ct);
            return Results.Ok(users.Select(user => new
            {
                user.UserId, user.Email, user.Mobile, user.DisplayName, user.Status, user.InviteExpiresAt,
                user.InvitationDeliveredAt, user.Version,
                roles = roleRows.Where(x => x.UserId == user.UserId)
            }));
        });

        api.MapGet("/scopes", async (IM02Access access, BerynxDbContext db, ICurrentTenantContext tenant,
            CancellationToken ct) =>
        {
            var current = await access.CurrentUser(ct);
            if (!await access.CanAccess(current.UserId, "users.view", null, null, null, ct) &&
                !await access.CanAccess(current.UserId, "users.create", null, null, null, ct))
                throw new UnauthorizedAccessException();
            var setupTenant = await db.Set<Tenant>().SingleAsync(x => x.TenantId == tenant.TenantId, ct);
            var setup = OnboardingStore.Decode(setupTenant);
            return Results.Ok(new
            {
                branches = setup.Branches.Select(x => new { x.Id, x.Name }),
                plants = setup.Plants.Select(x => new { x.Id, x.Name })
            });
        });

        api.MapGet("/context", async (IM02Access access, BerynxDbContext db, ICurrentTenantContext tenant,
            TenantEntitlementService entitlements, CancellationToken ct) =>
        {
            var current = await access.CurrentUser(ct);
            var tenantName = await db.Set<Tenant>()
                .Where(x => x.TenantId == tenant.TenantId)
                .Select(x => x.Name)
                .SingleAsync(ct);
            var permissions = await (
                from assignment in db.Set<M02UserRole>()
                join grant in db.Set<M02RolePermission>()
                    on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
                where assignment.UserId == current.UserId
                select grant.PermissionCode
            ).Distinct().OrderBy(x => x).ToListAsync(ct);
            var scopeTypes = await db.Set<M02UserRole>()
                .Where(x => x.UserId == current.UserId)
                .Select(x => x.ScopeType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);
            var modules = await entitlements.EnabledModules(ct);

            return Results.Ok(new
            {
                tenantName,
                current.DisplayName,
                current.Email,
                permissions,
                scopeTypes,
                modules
            });
        });

        api.MapPost("/users", async (InviteRequest request, IM02Access access, BerynxDbContext db,
            UserManager<M02IdentityUser> identityUsers,
            ICurrentTenantContext tenant, TenantEntitlementService entitlements, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "users.create", ct);
            if (request.Roles is null || request.Roles.Length == 0)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Assign at least one role.");
            var email = NormalizeEmail(request.Email);
            var mobile = NormalizeMobile(request.Mobile);
            M02Rules.ValidateInvitation(email, mobile, request.Roles.Select(x => x.RoleKey).ToArray());
            if (email is null)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "An email address is required for local password activation. Mobile OTP is not available.");
            if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 150)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Enter a name up to 150 characters.");
            var grants = await ResolveGrants(db, request.Roles, tenant.TenantId, ct);
            await EnsureCanAssignRoles(db, access, current.UserId, grants, ct);
            if (await db.Set<M02User>().AnyAsync(x => x.Email == email, ct))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "This account already has a membership in this tenant. Manage the existing user instead.");
            await entitlements.EnsureInviteCapacity(ct);
            var identity = await identityUsers.FindByEmailAsync(email);
            if (identity is null)
            {
                identity = new M02IdentityUser
                {
                    UserName = email,
                    Email = email,
                    LockoutEnabled = true
                };
                var created = await identityUsers.CreateAsync(identity);
                if (!created.Succeeded)
                    identity = await identityUsers.FindByEmailAsync(email);
                if (identity is null)
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "Could not create the invited account.");
            }
            if (await db.Set<M02User>().AnyAsync(x => x.IdentityUserId == identity.Id, ct))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "This account already has a membership in this tenant. Manage the existing user instead.");
            var now = DateTimeOffset.UtcNow;
            var user = new M02User
            {
                TenantId = tenant.TenantId,
                UserId = Guid.NewGuid(),
                IdentityUserId = identity.Id,
                ProviderSubject = identity.Id.ToString("D"),
                Email = email,
                Mobile = mobile,
                DisplayName = request.DisplayName.Trim(),
                Status = "Invited",
                InvitationDeliveryId = Guid.NewGuid(),
                InviteExpiresAt = M02Rules.InvitationExpiry(now),
                InvitationAvailableAt = now,
                Version = 1
            };
            db.Add(user);
            foreach (var grant in grants)
                db.Add(new M02UserRole
                {
                    TenantId = tenant.TenantId, AssignmentId = Guid.NewGuid(), UserId = user.UserId,
                    RoleId = grant.RoleId, ScopeType = grant.ScopeType, ScopeId = grant.ScopeId,
                    AssignedAt = now, AssignedBy = current.ProviderSubject ?? current.UserId.ToString()
                });
            Audit(db, tenant.TenantId, current, "Invite", "User", user.UserId, null,
                new { user.Email, user.Mobile, user.DisplayName, user.Status, roles = request.Roles });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/m02/users/{user.UserId}", new { user.UserId, user.InviteExpiresAt });
        });

        api.MapPost("/users/{id:guid}/resend", async (Guid id, IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "users.edit", ct);
            var user = await db.Set<M02User>().SingleOrDefaultAsync(x => x.UserId == id, ct)
                ?? throw new KeyNotFoundException();
            if (user.Status != "Invited")
                throw new System.ComponentModel.DataAnnotations.ValidationException("Only pending invitations can be resent.");
            var before = UserSnapshot(user);
            user.InvitationDeliveryId = Guid.NewGuid();
            user.InvitationDeliveredAt = null;
            user.InviteExpiresAt = M02Rules.InvitationExpiry(DateTimeOffset.UtcNow);
            user.InvitationAttempts = 0;
            user.InvitationAvailableAt = DateTimeOffset.UtcNow;
            user.Version++;
            Audit(db, tenant.TenantId, current, "Invitation resent", "User", id, before, UserSnapshot(user));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapPut("/users/{id:guid}/roles", async (Guid id, UserRolesRequest request,
            IM02Access access, BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "users.edit", ct);
            if (request.Roles is null || request.Roles.Length == 0)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Assign at least one role.");
            var keys = request.Roles.Select(x => $"{x.RoleKey}|{x.ScopeType}|{x.ScopeId}")
                .ToArray();
            if (keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Length)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Remove duplicate role scopes.");
            var user = await db.Set<M02User>().SingleOrDefaultAsync(x => x.UserId == id, ct)
                ?? throw new KeyNotFoundException();
            if (user.Version != request.Version)
                throw new DbUpdateConcurrencyException();
            if (user.Status is "Revoked")
                throw new System.ComponentModel.DataAnnotations.ValidationException("A revoked user cannot be reassigned.");
            var desired = await ResolveGrants(db, request.Roles, tenant.TenantId, ct);
            await EnsureCanAssignRoles(db, access, current.UserId, desired, ct);
            var existing = await db.Set<M02UserRole>().Where(x => x.UserId == id).ToListAsync(ct);
            var before = existing.Select(x => new { x.RoleId, x.ScopeType, x.ScopeId }).ToArray();
            var retained = new List<M02UserRole>();
            foreach (var grant in desired)
            {
                var match = existing.FirstOrDefault(x => x.RoleId == grant.RoleId &&
                    x.ScopeType == grant.ScopeType && x.ScopeId == grant.ScopeId);
                if (match is not null)
                    retained.Add(match);
                else
                    db.Add(new M02UserRole
                    {
                        TenantId = tenant.TenantId, AssignmentId = Guid.NewGuid(), UserId = id,
                        RoleId = grant.RoleId, ScopeType = grant.ScopeType, ScopeId = grant.ScopeId,
                        AssignedAt = DateTimeOffset.UtcNow,
                        AssignedBy = current.ProviderSubject ?? current.UserId.ToString()
                    });
            }
            var remove = existing.Except(retained).ToList();
            db.RemoveRange(remove);
            user.Version++;
            Audit(db, tenant.TenantId, current, "User roles changed", "User", id,
                before, desired.Select(x => new { x.RoleId, x.ScopeType, x.ScopeId }));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.UserId, user.Version });
        });

        api.MapPost("/users/{id:guid}/revoke-invitation", async (Guid id, IM02Access access,
            BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "users.cancel", ct);
            var user = await db.Set<M02User>().SingleOrDefaultAsync(x => x.UserId == id, ct)
                ?? throw new KeyNotFoundException();
            if (user.Status != "Invited")
                throw new System.ComponentModel.DataAnnotations.ValidationException("Only pending invitations can be revoked.");
            var before = UserSnapshot(user);
            user.Status = "Revoked";
            user.InvitationDeliveryId = null;
            user.Version++;
            Audit(db, tenant.TenantId, current, "Invitation revoked", "User", id, before, UserSnapshot(user));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapGet("/users/{id:guid}/open-tasks", async (Guid id, IM02Access access,
            BerynxDbContext db, IEnumerable<IM02TaskReassignmentProvider> providers, CancellationToken ct) =>
        {
            await RequireTenantWide(access, "users.edit", ct);
            if (!await db.Set<M02User>().AnyAsync(x => x.UserId == id, ct))
                throw new KeyNotFoundException();
            var tasks = new List<M02OpenTask>();
            foreach (var provider in providers)
                tasks.AddRange(await provider.OpenTasks(id, ct));
            return Results.Ok(tasks);
        });

        api.MapPost("/users/{id:guid}/deactivate", async (Guid id, UserDeactivationRequest request,
            IM02Access access, BerynxDbContext db, IEnumerable<IM02TaskReassignmentProvider> taskProviders,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "users.edit", ct);
            var user = await db.Set<M02User>().SingleOrDefaultAsync(x => x.UserId == id, ct)
                ?? throw new KeyNotFoundException();
            if (user.Status != "Active")
                throw new System.ComponentModel.DataAnnotations.ValidationException("Only active users can be deactivated.");
            if (user.Version != request.Version)
                throw new DbUpdateConcurrencyException();
            var before = UserSnapshot(user);
            var openTasks = new List<M02OpenTask>();
            foreach (var taskProvider in taskProviders)
                openTasks.AddRange(await taskProvider.OpenTasks(id, ct));
            if (request.Reassignments is null)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Assign every open task exactly once before deactivating this user.");
            var assignmentKeys = request.Reassignments.Select(x => $"{x.Provider}|{x.TaskId}")
                .ToHashSet(StringComparer.Ordinal);
            if (openTasks.Any(x => !assignmentKeys.Contains($"{x.Provider}|{x.TaskId}")) ||
                request.Reassignments.Any(x => !openTasks.Any(t => t.Provider == x.Provider && t.TaskId == x.TaskId)) ||
                assignmentKeys.Count != request.Reassignments.Length)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Assign every open task exactly once before deactivating this user.");
            foreach (var assignment in request.Reassignments)
                if (!await db.Set<M02User>().AnyAsync(x => x.UserId == assignment.NewOwnerUserId &&
                        x.Status == "Active", ct))
                    throw new System.ComponentModel.DataAnnotations.ValidationException(
                        "Choose an active user in this tenant for every reassignment.");

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            foreach (var taskProvider in taskProviders)
                await taskProvider.Reassign(id, request.Reassignments.Where(x => x.Provider == taskProvider.Provider)
                    .ToArray(), ct);
            user.Status = "Inactive";
            user.DeactivatedAt = DateTimeOffset.UtcNow;
            user.Version++;
            var revokedAt = DateTimeOffset.UtcNow;
            foreach (var session in await db.Set<M02AuthSession>()
                         .Where(x => x.UserId == id && x.RevokedAt == null).ToListAsync(ct))
            {
                session.RevokedAt = revokedAt;
                foreach (var token in await db.Set<M02RefreshToken>()
                             .Where(x => x.SessionId == session.SessionId && x.RevokedAt == null)
                             .ToListAsync(ct))
                    token.RevokedAt = revokedAt;
            }
            Audit(db, tenant.TenantId, current, "User deactivated", "User", id, before, UserSnapshot(user),
                JsonSerializer.Serialize(request.Reassignments));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        });

        api.MapPost("/users/{id:guid}/activate", async (Guid id, IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "users.edit", ct);
            var user = await db.Set<M02User>().SingleOrDefaultAsync(x => x.UserId == id, ct)
                ?? throw new KeyNotFoundException();
            if (user.Status != "Inactive")
                throw new System.ComponentModel.DataAnnotations.ValidationException("Only inactive users can be activated.");
            var before = UserSnapshot(user);
            user.Status = "Active";
            user.DeactivatedAt = null;
            user.Version++;
            Audit(db, tenant.TenantId, current, "User activated", "User", id, before, UserSnapshot(user));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapGet("/roles", async (IM02Access access, BerynxDbContext db, CancellationToken ct) =>
        {
            await RequireTenantWide(access, "roles.view", ct);
            var roles = await db.Set<M02Role>().OrderBy(x => x.Name).ToListAsync(ct);
            var permissions = await db.Set<M02RolePermission>().ToListAsync(ct);
            var catalog = await db.Set<M02PermissionCatalog>().OrderBy(x => x.Module).ThenBy(x => x.Action).ToListAsync(ct);
            return Results.Ok(new
            {
                permissions = catalog,
                roles = roles.Select(role => new
                {
                    role.RoleId, role.RoleKey, role.Name, role.Description, role.IsSystem,
                    role.ClonedFromRoleId, role.Version,
                    permissionCodes = permissions.Where(x => x.RoleId == role.RoleId).Select(x => x.PermissionCode)
                })
            });
        });

        api.MapPost("/roles", async (RoleWrite request, IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "roles.create", ct);
            ValidateRole(request);
            var role = new M02Role
            {
                TenantId = tenant.TenantId, RoleId = Guid.NewGuid(),
                RoleKey = "custom-" + Guid.NewGuid().ToString("N"),
                Name = request.Name.Trim(), Description = request.Description.Trim(),
                IsSystem = false, Version = 1
            };
            var catalog = await db.Set<M02PermissionCatalog>()
                .Where(x => request.Permissions.Contains(x.PermissionCode)).Select(x => x.PermissionCode).ToListAsync(ct);
            if (catalog.Count != request.Permissions.Distinct().Count())
                throw new System.ComponentModel.DataAnnotations.ValidationException("A permission is not in the server catalog.");
            await EnsureCanGrantPermissions(db, current.UserId, catalog, ct);
            db.Add(role);
            AddPermissions(db, tenant.TenantId, role.RoleId, catalog);
            Audit(db, tenant.TenantId, current, "Role created", "Role", role.RoleId, null,
                new { role.Name, permissions = catalog });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/m02/roles/{role.RoleId}", new { role.RoleId, role.Version });
        });

        api.MapPost("/roles/{id:guid}/clone", async (Guid id, RoleWrite request, IM02Access access,
            BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "roles.create", ct);
            var source = await db.Set<M02Role>().SingleOrDefaultAsync(x => x.RoleId == id, ct)
                ?? throw new KeyNotFoundException();
            var permissions = await db.Set<M02RolePermission>().Where(x => x.RoleId == id)
                .Select(x => x.PermissionCode).ToArrayAsync(ct);
            await EnsureCanGrantPermissions(db, current.UserId, permissions, ct);
            var clone = new M02Role
            {
                TenantId = tenant.TenantId, RoleId = Guid.NewGuid(),
                RoleKey = "custom-" + Guid.NewGuid().ToString("N"),
                Name = string.IsNullOrWhiteSpace(request.Name) ? source.Name + " copy" : request.Name.Trim(),
                Description = request.Description ?? source.Description,
                IsSystem = false, ClonedFromRoleId = source.RoleId, Version = 1
            };
            db.Add(clone);
            AddPermissions(db, tenant.TenantId, clone.RoleId, permissions);
            Audit(db, tenant.TenantId, current, "Role cloned", "Role", clone.RoleId, null,
                new { source.RoleId, clone.Name, permissions });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/m02/roles/{clone.RoleId}", new { clone.RoleId, clone.Version });
        });

        api.MapPut("/roles/{id:guid}", async (Guid id, RoleWrite request, IM02Access access,
            BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "roles.edit", ct);
            ValidateRole(request);
            var role = await db.Set<M02Role>().SingleOrDefaultAsync(x => x.RoleId == id, ct)
                ?? throw new KeyNotFoundException();
            if (role.IsSystem)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Clone system roles before editing.");
            if (role.Version != request.Version)
                throw new DbUpdateConcurrencyException();
            var permissions = await db.Set<M02PermissionCatalog>()
                .Where(x => request.Permissions.Contains(x.PermissionCode)).Select(x => x.PermissionCode).ToListAsync(ct);
            if (permissions.Count != request.Permissions.Distinct().Count())
                throw new System.ComponentModel.DataAnnotations.ValidationException("A permission is not in the server catalog.");
            var before = RoleSnapshot(role, await db.Set<M02RolePermission>()
                .Where(x => x.RoleId == id).Select(x => x.PermissionCode).ToArrayAsync(ct));
            await EnsureCanGrantPermissions(db, current.UserId, permissions, ct);
            role.Name = request.Name.Trim();
            role.Description = request.Description.Trim();
            role.Version++;
            var old = await db.Set<M02RolePermission>().Where(x => x.RoleId == id).ToListAsync(ct);
            db.RemoveRange(old);
            AddPermissions(db, tenant.TenantId, id, permissions);
            Audit(db, tenant.TenantId, current, "Role permissions changed", "Role", id, before,
                RoleSnapshot(role, permissions));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { role.RoleId, role.Version });
        });

        api.MapDelete("/roles/{id:guid}", async (Guid id, long version, IM02Access access,
            BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "roles.cancel", ct);
            var role = await db.Set<M02Role>().SingleOrDefaultAsync(x => x.RoleId == id, ct)
                ?? throw new KeyNotFoundException();
            if (role.IsSystem)
                throw new System.ComponentModel.DataAnnotations.ValidationException("System roles cannot be deleted.");
            if (role.Version != version)
                throw new DbUpdateConcurrencyException();
            if (await db.Set<M02UserRole>().AnyAsync(x => x.RoleId == id, ct) ||
                await db.Set<M02ApprovalLevel>().AnyAsync(x =>
                    x.ApproverRoleId == id || x.EscalationRoleId == id, ct) ||
                await db.Set<M02Role>().AnyAsync(x => x.ClonedFromRoleId == id, ct))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "An assigned, approval-referenced or cloned role cannot be deleted.");
            Audit(db, tenant.TenantId, current, "Role deleted", "Role", id, RoleSnapshot(role, []), null);
            db.Remove(role);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapGet("/approval-rules", async (IM02Access access, BerynxDbContext db, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "approvals.view", ct);
            var canViewCost = await access.CanAccess(current.UserId, "global.view_cost", null, null, null, ct);
            var rules = await db.Set<M02ApprovalRule>().OrderBy(x => x.Name).ToListAsync(ct);
            var levels = await db.Set<M02ApprovalLevel>().OrderBy(x => x.Level).ToListAsync(ct);
            return Results.Ok(rules.Select(rule => new
            {
                rule.RuleId, rule.Name, rule.EntityType, rule.AmountGreaterThan, rule.DiscountPercentGreaterThan,
                marginPercentBelow = canViewCost ? rule.MarginPercentBelow : null,
                rule.CreditStatus, rule.Enabled, rule.Version,
                levels = levels.Where(level => level.RuleId == rule.RuleId)
            }));
        });

        api.MapPost("/approval-rules", async (RuleWrite request, IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "approvals.create", ct);
            ValidateRule(request);
            if (request.MarginPercentBelow.HasValue &&
                !await access.CanAccess(current.UserId, "global.view_cost", null, null, null, ct))
                throw new UnauthorizedAccessException("Cost and margin permission is required for margin rules.");
            var rule = new M02ApprovalRule
            {
                TenantId = tenant.TenantId, RuleId = Guid.NewGuid(), Name = request.Name.Trim(),
                EntityType = request.EntityType.Trim(), AmountGreaterThan = request.AmountGreaterThan,
                DiscountPercentGreaterThan = request.DiscountPercentGreaterThan,
                MarginPercentBelow = request.MarginPercentBelow, CreditStatus = request.CreditStatus?.Trim(),
                Enabled = request.Enabled, Version = 1
            };
            db.Add(rule);
            AddLevels(db, tenant.TenantId, rule.RuleId, request.Levels);
            Audit(db, tenant.TenantId, current, "Approval rule created", "ApprovalRule", rule.RuleId, null, request);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/m02/approval-rules/{rule.RuleId}", new { rule.RuleId, rule.Version });
        });

        api.MapPut("/approval-rules/{id:guid}", async (Guid id, RuleWrite request, IM02Access access,
            BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "approvals.edit", ct);
            ValidateRule(request);
            var rule = await db.Set<M02ApprovalRule>().SingleOrDefaultAsync(x => x.RuleId == id, ct)
                ?? throw new KeyNotFoundException();
            if (rule.Version != request.Version)
                throw new DbUpdateConcurrencyException();
            if (await db.Set<M02ApprovalRequest>().AnyAsync(x =>
                    x.RuleId == id && x.Status == "Pending", ct))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "A rule with pending requests cannot be edited; finish those approvals first.");
            var canViewCost = await access.CanAccess(current.UserId, "global.view_cost", null, null, null, ct);
            if (request.MarginPercentBelow.HasValue && !canViewCost)
                throw new UnauthorizedAccessException("Cost and margin permission is required for margin rules.");
            var before = JsonSerializer.Serialize(rule, Json);
            rule.Name = request.Name.Trim();
            rule.EntityType = request.EntityType.Trim();
            rule.AmountGreaterThan = request.AmountGreaterThan;
            rule.DiscountPercentGreaterThan = request.DiscountPercentGreaterThan;
            if (canViewCost)
                rule.MarginPercentBelow = request.MarginPercentBelow;
            rule.CreditStatus = request.CreditStatus?.Trim();
            rule.Enabled = request.Enabled;
            rule.Version++;
            var oldLevels = await db.Set<M02ApprovalLevel>().Where(x => x.RuleId == id).ToListAsync(ct);
            db.RemoveRange(oldLevels);
            AddLevels(db, tenant.TenantId, id, request.Levels);
            Audit(db, tenant.TenantId, current, "Approval rule changed", "ApprovalRule", id, before, request);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { rule.RuleId, rule.Version });
        });

        api.MapDelete("/approval-rules/{id:guid}", async (Guid id, long version, IM02Access access,
            BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "approvals.cancel", ct);
            var rule = await db.Set<M02ApprovalRule>().SingleOrDefaultAsync(x => x.RuleId == id, ct)
                ?? throw new KeyNotFoundException();
            if (rule.Version != version)
                throw new DbUpdateConcurrencyException();
            if (await db.Set<M02ApprovalRequest>().AnyAsync(x => x.RuleId == id, ct))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "A rule with approval history cannot be deleted; disable it instead.");
            Audit(db, tenant.TenantId, current, "Approval rule deleted", "ApprovalRule", id,
                JsonSerializer.Serialize(rule, Json), null);
            db.Remove(rule);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapGet("/approvals/inbox", async (IM02Access access, BerynxDbContext db, CancellationToken ct) =>
        {
            await access.Require("approvals.view", ct);
            var current = await access.CurrentUser(ct);
            var requests = await db.Set<M02ApprovalRequest>()
                .Where(x => x.Status == "Pending").OrderBy(x => x.DueAt).ToListAsync(ct);
            var levels = await db.Set<M02ApprovalLevel>().ToListAsync(ct);
            var taskAssignments = await db.Set<M02ApprovalTaskAssignment>().ToListAsync(ct);
            var roles = await db.Set<M02UserRole>().Where(x => x.UserId == current.UserId)
                .Select(x => x.RoleId).ToListAsync(ct);
            var delegations = await db.Set<M02Delegation>().Where(x => x.DelegateUserId == current.UserId &&
                x.RevokedAt == null && x.StartsAt <= DateTimeOffset.UtcNow && x.EndsAt > DateTimeOffset.UtcNow)
                .ToListAsync(ct);
            var delegatedBy = delegations.Select(x => x.DelegatorUserId).Distinct().ToArray();
            var activeDelegators = await db.Set<M02User>()
                .Where(x => delegatedBy.Contains(x.UserId) && x.Status == "Active")
                .Select(x => x.UserId).ToListAsync(ct);
            var delegatedRoleAssignments = await db.Set<M02UserRole>()
                .Where(x => activeDelegators.Contains(x.UserId))
                .Select(x => new { x.UserId, x.RoleId }).ToListAsync(ct);
            var decisions = await db.Set<M02ApprovalDecision>().OrderByDescending(x => x.At).ToListAsync(ct);
            var visible = new List<M02ApprovalRequest>();
            var costVisibility = new Dictionary<Guid, bool>();
            foreach (var request in requests)
            {
                if (request.RequesterUserId == current.UserId)
                    continue;
                var level = levels.SingleOrDefault(x => x.RuleId == request.RuleId && x.Level == request.CurrentLevel);
                if (level is null)
                    continue;
                var taskAssignment = taskAssignments.SingleOrDefault(x =>
                    x.RequestId == request.RequestId && x.Level == request.CurrentLevel);
                var originalAssignment = taskAssignment is not null
                    ? taskAssignment.ApproverUserId == current.UserId
                    : level.ApproverUserId == current.UserId ||
                      level.ApproverRoleId.HasValue && roles.Contains(level.ApproverRoleId.Value);
                var escalatedAssignment = request.EscalatedAt.HasValue &&
                    (level.EscalationUserId == current.UserId ||
                     level.EscalationRoleId.HasValue && roles.Contains(level.EscalationRoleId.Value));
                var delegatedApprover = taskAssignment?.ApproverUserId ?? level.ApproverUserId;
                var delegatedRole = taskAssignment is null ? level.ApproverRoleId : null;
                var delegatedAssignment = delegations.Any(d => activeDelegators.Contains(d.DelegatorUserId) &&
                    (d.DelegatorUserId == delegatedApprover ||
                     delegatedRole.HasValue && delegatedRoleAssignments.Any(r =>
                         r.UserId == d.DelegatorUserId && r.RoleId == delegatedRole)));
                var escalatedDelegation = request.EscalatedAt.HasValue &&
                    delegations.Any(d => activeDelegators.Contains(d.DelegatorUserId) &&
                        (d.DelegatorUserId == level.EscalationUserId ||
                         level.EscalationRoleId.HasValue && delegatedRoleAssignments.Any(r =>
                             r.UserId == d.DelegatorUserId && r.RoleId == level.EscalationRoleId)));
                if (!(originalAssignment || escalatedAssignment || delegatedAssignment || escalatedDelegation))
                    continue;
                if (!await access.CanAccess(current.UserId, "approvals.view",
                        request.BranchId, request.PlantId, request.OwnerUserId, ct))
                    continue;
                visible.Add(request);
                costVisibility[request.RequestId] = await access.CanAccess(current.UserId, "global.view_cost",
                    request.BranchId, request.PlantId, request.OwnerUserId, ct);
            }
            var output = visible.Select(request => new
            {
                request.RequestId, request.SourceType, request.SourceId, request.RequesterUserId,
                request.BranchId, request.Status, request.CurrentLevel,
                amount = costVisibility[request.RequestId] ? request.Amount : (decimal?)null,
                marginPercent = costVisibility[request.RequestId] ? request.MarginPercent : null,
                request.DiscountPercent, request.CreditStatus, request.DueAt, request.CreatedAt,
                snapshot = costVisibility[request.RequestId] ? request.Snapshot : RedactFinancial(request.Snapshot),
                history = decisions.Where(x => x.RequestId == request.RequestId)
            });
            return Results.Ok(output);
        });

        api.MapGet("/notifications", async (IM02Access access, BerynxDbContext db, CancellationToken ct) =>
        {
            var current = await access.CurrentUser(ct);
            return Results.Ok(await db.Set<M02Notification>()
                .Where(x => x.RecipientUserId == current.UserId)
                .OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct));
        });

        api.MapPost("/notifications/{id:guid}/read", async (Guid id, IM02Access access,
            BerynxDbContext db, CancellationToken ct) =>
        {
            var current = await access.CurrentUser(ct);
            var notification = await db.Set<M02Notification>().SingleOrDefaultAsync(x =>
                x.NotificationId == id && x.RecipientUserId == current.UserId, ct)
                ?? throw new KeyNotFoundException();
            notification.ReadAt ??= DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapPost("/approvals/{id:guid}/decision", async (Guid id, ApprovalDecisionRequest request,
            IM02Access access, BerynxDbContext db, ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            await access.Require("approvals.approve", ct);
            var current = await access.CurrentUser(ct);
            var approval = await db.Set<M02ApprovalRequest>().SingleOrDefaultAsync(x => x.RequestId == id, ct)
                ?? throw new KeyNotFoundException();
            if (approval.Version != request.Version || approval.Status != "Pending")
                throw new DbUpdateConcurrencyException();
            M02Rules.ValidateDecision(approval.RequesterUserId, current.UserId, request.Decision, request.Comment);
            var level = await db.Set<M02ApprovalLevel>().SingleAsync(x =>
                x.RuleId == approval.RuleId && x.Level == approval.CurrentLevel, ct);
            var taskAssignment = await db.Set<M02ApprovalTaskAssignment>().SingleOrDefaultAsync(x =>
                x.RequestId == approval.RequestId && x.Level == approval.CurrentLevel, ct);
            var direct = taskAssignment is not null
                ? taskAssignment.ApproverUserId == current.UserId
                : level.ApproverUserId == current.UserId;
            if (taskAssignment is null && !direct && level.ApproverRoleId.HasValue)
                direct = await db.Set<M02UserRole>().AnyAsync(x =>
                    x.UserId == current.UserId && x.RoleId == level.ApproverRoleId, ct);
            if (!direct && approval.EscalatedAt.HasValue)
            {
                direct = level.EscalationUserId == current.UserId;
                if (!direct && level.EscalationRoleId.HasValue)
                    direct = await db.Set<M02UserRole>().AnyAsync(x =>
                        x.UserId == current.UserId && x.RoleId == level.EscalationRoleId, ct);
            }
            var delegated = false;
            if (!direct)
            {
                var delegation = db.Set<M02Delegation>().Where(x =>
                    x.DelegateUserId == current.UserId && x.RevokedAt == null &&
                    x.StartsAt <= DateTimeOffset.UtcNow && x.EndsAt > DateTimeOffset.UtcNow &&
                    db.Set<M02User>().Any(u => u.UserId == x.DelegatorUserId && u.Status == "Active"));
                delegated = taskAssignment is null
                    ? await delegation.AnyAsync(x =>
                        x.DelegatorUserId == level.ApproverUserId ||
                        level.ApproverRoleId.HasValue && db.Set<M02UserRole>().Any(r =>
                            r.UserId == x.DelegatorUserId && r.RoleId == level.ApproverRoleId) ||
                        approval.EscalatedAt.HasValue && x.DelegatorUserId == level.EscalationUserId ||
                        approval.EscalatedAt.HasValue && level.EscalationRoleId.HasValue &&
                        db.Set<M02UserRole>().Any(r =>
                            r.UserId == x.DelegatorUserId && r.RoleId == level.EscalationRoleId), ct)
                    : await delegation.AnyAsync(x =>
                        x.DelegatorUserId == taskAssignment.ApproverUserId ||
                        approval.EscalatedAt.HasValue && x.DelegatorUserId == level.EscalationUserId ||
                        approval.EscalatedAt.HasValue && level.EscalationRoleId.HasValue &&
                        db.Set<M02UserRole>().Any(r =>
                            r.UserId == x.DelegatorUserId && r.RoleId == level.EscalationRoleId), ct);
            }
            if (!direct && !delegated)
                throw new UnauthorizedAccessException("This approval is not assigned to the current user.");
            var withinScope = await access.CanAccess(current.UserId, "approvals.approve",
                approval.BranchId, approval.PlantId, approval.OwnerUserId, ct);
            var hasApprovalPermission = await access.HasPermission(current.UserId, "approvals.approve", ct);
            if (delegated)
                M02Rules.ValidateDelegatedApproval(hasApprovalPermission, withinScope);
            else if (!withinScope)
                throw new UnauthorizedAccessException("The current user is outside the approval scope.");

            var before = JsonSerializer.Serialize(new { approval.Status, approval.CurrentLevel, approval.Version }, Json);
            var now = DateTimeOffset.UtcNow;
            db.Add(new M02ApprovalDecision
            {
                TenantId = tenant.TenantId, DecisionId = Guid.NewGuid(), RequestId = id,
                ActorUserId = current.UserId, Level = approval.CurrentLevel, Decision = request.Decision,
                Comment = request.Comment?.Trim(), At = now
            });
            if (request.Decision == "Reject")
            {
                approval.Status = "Rejected";
                AddNotification(db, tenant.TenantId, approval.RequesterUserId, "ApprovalRejected", id,
                    new { approval.SourceType, comment = request.Comment?.Trim() });
            }
            else
            {
                var next = await db.Set<M02ApprovalLevel>().SingleOrDefaultAsync(x =>
                    x.RuleId == approval.RuleId && x.Level == approval.CurrentLevel + 1, ct);
                if (next is null)
                {
                    approval.Status = "Approved";
                    AddNotification(db, tenant.TenantId, approval.RequesterUserId, "ApprovalApproved", id,
                        new { approval.SourceType });
                }
                else
                {
                    approval.CurrentLevel++;
                    approval.DueAt = now.AddMinutes(next.SlaMinutes);
                    approval.EscalatedAt = null;
                    var candidates = next.ApproverUserId.HasValue
                        ? [next.ApproverUserId.Value]
                        : await db.Set<M02UserRole>().Where(x => x.RoleId == next.ApproverRoleId)
                            .Select(x => x.UserId).Distinct().ToArrayAsync(ct);
                    var recipients = new List<Guid>();
                    foreach (var recipient in candidates)
                        if (recipient != approval.RequesterUserId &&
                            await access.CanAccess(recipient, "approvals.view", approval.BranchId,
                                approval.PlantId, approval.OwnerUserId, ct) &&
                            await access.CanAccess(recipient, "approvals.approve", approval.BranchId,
                                approval.PlantId, approval.OwnerUserId, ct))
                            recipients.Add(recipient);
                    if (recipients.Count == 0)
                        throw new InvalidOperationException(
                            "No active approver has inbox permission for the request data scope.");
                    foreach (var recipient in recipients)
                        AddNotification(db, tenant.TenantId, recipient, "ApprovalRequested", id,
                            new { approval.SourceType, approval.CurrentLevel, approval.DueAt });
                }
            }
            approval.Version++;
            Audit(db, tenant.TenantId, current, "Approval " + request.Decision, "ApprovalRequest", id, before,
                new { approval.Status, approval.CurrentLevel, request.Comment });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { approval.Status, approval.CurrentLevel, approval.Version });
        });

        api.MapGet("/delegations", async (IM02Access access, BerynxDbContext db, CancellationToken ct) =>
        {
            var current = await access.CurrentUser(ct);
            return Results.Ok(await db.Set<M02Delegation>().Where(x =>
                    x.DelegatorUserId == current.UserId || x.DelegateUserId == current.UserId)
                .OrderByDescending(x => x.StartsAt).ToListAsync(ct));
        });

        api.MapPost("/delegations", async (DelegationRequest request, IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            await access.Require("approvals.edit", ct);
            await access.Require("approvals.approve", ct);
            var current = await access.CurrentUser(ct);
            var now = DateTimeOffset.UtcNow;
            if (request.DelegateUserId == current.UserId || request.StartsAt < now ||
                request.EndsAt <= request.StartsAt)
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Delegation must be to another user with a valid future date range.");
            var delegateUser = await db.Set<M02User>().SingleOrDefaultAsync(x =>
                x.UserId == request.DelegateUserId && x.Status == "Active", ct)
                ?? throw new System.ComponentModel.DataAnnotations.ValidationException("Choose an active delegate.");
            if (!await access.HasPermission(delegateUser.UserId, "approvals.approve", ct))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "A delegate must already have approval permission; delegation cannot grant it.");
            var currentUser = await access.CurrentUser(ct);
            var delegation = new M02Delegation
            {
                TenantId = tenant.TenantId, DelegationId = Guid.NewGuid(),
                DelegatorUserId = currentUser.UserId, DelegateUserId = delegateUser.UserId,
                StartsAt = request.StartsAt, EndsAt = request.EndsAt, CreatedAt = now
            };
            db.Add(delegation);
            Audit(db, tenant.TenantId, currentUser, "Delegation created", "Delegation",
                delegation.DelegationId, null, delegation);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/m02/delegations/{delegation.DelegationId}", delegation);
        });

        api.MapDelete("/delegations/{id:guid}", async (Guid id, IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            var current = await access.CurrentUser(ct);
            var delegation = await db.Set<M02Delegation>().SingleOrDefaultAsync(x =>
                x.DelegationId == id && x.DelegatorUserId == current.UserId && x.RevokedAt == null, ct)
                ?? throw new KeyNotFoundException();
            delegation.RevokedAt = DateTimeOffset.UtcNow;
            Audit(db, tenant.TenantId, current, "Delegation revoked", "Delegation", id, null,
                new { delegation.DelegateUserId, delegation.RevokedAt });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapGet("/audit", async (IM02Access access, BerynxDbContext db, Guid? userId, string? entity,
            string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "audit.view", ct);
            var canViewCost = await access.CanAccess(current.UserId, "global.view_cost", null, null, null, ct);
            var events = db.Set<M02AuditEvent>().AsNoTracking();
            if (userId.HasValue) events = events.Where(x => x.ActorUserId == userId);
            if (!string.IsNullOrWhiteSpace(entity)) events = events.Where(x => x.EntityType == entity);
            if (!string.IsNullOrWhiteSpace(action)) events = events.Where(x => x.Action == action);
            if (from.HasValue) events = events.Where(x => x.At >= from);
            if (to.HasValue) events = events.Where(x => x.At <= to);
            var result = await events.OrderByDescending(x => x.At).Take(500).ToListAsync(ct);
            if (!canViewCost)
                foreach (var item in result)
                {
                    item.Before = RedactFinancial(item.Before);
                    item.After = RedactFinancial(item.After);
                }
            return Results.Ok(result);
        });

        api.MapGet("/audit/export", async (IM02Access access, BerynxDbContext db, Guid? userId,
            string? entity, string? action, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        {
            var current = await RequireTenantWide(access, "audit.export", ct);
            var canViewCost = await access.CanAccess(current.UserId, "global.view_cost", null, null, null, ct);
            var query = db.Set<M02AuditEvent>().AsNoTracking();
            if (userId.HasValue) query = query.Where(x => x.ActorUserId == userId);
            if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(x => x.EntityType == entity);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action);
            if (from.HasValue) query = query.Where(x => x.At >= from);
            if (to.HasValue) query = query.Where(x => x.At <= to);
            var events = await query.OrderByDescending(x => x.At).Take(10_000).ToListAsync(ct);
            if (!canViewCost)
                foreach (var item in events)
                {
                    item.Before = RedactFinancial(item.Before);
                    item.After = RedactFinancial(item.After);
                }
            var csv = new StringBuilder("At,Actor,Action,EntityType,EntityId,Before,After,Reason,IpAddress,Device\r\n");
            foreach (var item in events)
                csv.AppendLine(string.Join(',', new[]
                {
                    item.At.ToString("O", CultureInfo.InvariantCulture), item.ActorSubject, item.Action,
                    item.EntityType, item.EntityId?.ToString(), item.Before, item.After, item.Reason,
                    item.IpAddress, item.Device
                }.Select(Csv)));
            return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "m02-audit.csv");
        });

        api.MapGet("/profile", async (IM02Access access, BerynxDbContext db, HttpContext context,
            ClaimsPrincipal principal, CancellationToken ct) =>
        {
            var user = await access.CurrentUser(ct);
            var sessionId = Guid.TryParse(principal.FindFirstValue("sid"), out var parsed) ? parsed : Guid.Empty;
            return Results.Ok(new
            {
                user.UserId, user.Email, user.Mobile, user.DisplayName, user.Status,
                sessions = await db.Set<M02AuthSession>().Where(x => x.UserId == user.UserId)
                    .OrderByDescending(x => x.LastSeenAt)
                    .Select(x => new
                    {
                        id = x.SessionId.ToString("D"),
                        x.IpAddress,
                        x.Device,
                        createdAt = x.CreatedAt,
                        lastSeenAt = x.LastSeenAt,
                        x.ExpiresAt,
                        current = x.SessionId == sessionId
                    }).ToListAsync(ct)
            });
        });

        api.MapPut("/profile", async (ProfileWrite request, IM02Access access, BerynxDbContext db,
            ICurrentTenantContext tenant, CancellationToken ct) =>
        {
            await access.Require("profile.edit", ct);
            if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 150)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Enter a name up to 150 characters.");
            var user = await access.CurrentUser(ct);
            var before = UserSnapshot(user);
            user.DisplayName = request.DisplayName.Trim();
            user.Version++;
            Audit(db, tenant.TenantId, user, "Profile changed", "User", user.UserId, before, UserSnapshot(user));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.UserId, user.DisplayName, user.Version });
        });

        api.MapPost("/mfa/setup", async (IM02Access access, M02AuthenticationService authentication,
            BerynxDbContext db, ICurrentTenantContext tenant,
            CancellationToken ct) =>
        {
            var user = await access.CurrentUser(ct);
            var identityUserId = user.IdentityUserId ?? throw new UnauthorizedAccessException();
            var setup = await authentication.BeginTotp(identityUserId, ct);
            Audit(db, tenant.TenantId, user, "MFA setup started", "User", user.UserId, null,
                new { method = "TOTP" });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { setup.ProvisioningUri, setup.ManualKey });
        });

        api.MapPost("/mfa/confirm", async (CodeRequest request, IM02Access access,
            M02AuthenticationService authentication, BerynxDbContext db, ICurrentTenantContext tenant,
            CancellationToken ct) =>
        {
            var user = await access.CurrentUser(ct);
            var identityUserId = user.IdentityUserId ?? throw new UnauthorizedAccessException();
            var recoveryCodes = await authentication.ConfirmTotp(
                identityUserId, request.Code, request.CurrentCode, ct);
            Audit(db, tenant.TenantId, user, "MFA enabled", "User", user.UserId, null,
                new { method = "TOTP" });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { recoveryCodes });
        });

        api.MapGet("/sessions", async (IM02Access access, BerynxDbContext db, IConfiguration configuration,
            ClaimsPrincipal principal, CancellationToken ct) =>
        {
            var user = await access.CurrentUser(ct);
            var sessionId = Guid.TryParse(principal.FindFirstValue("sid"), out var parsed) ? parsed : Guid.Empty;
            var now = DateTimeOffset.UtcNow;
            var idleMinutes = Math.Clamp(configuration.GetValue("Authentication:SessionIdleMinutes", 30), 5, 1440);
            return Results.Ok(await db.Set<M02AuthSession>()
                .Where(x => x.UserId == user.UserId && x.RevokedAt == null &&
                            x.ExpiresAt > now && x.LastSeenAt > now.AddMinutes(-idleMinutes))
                .OrderByDescending(x => x.LastSeenAt)
                .Select(x => new
                {
                    id = x.SessionId.ToString("D"),
                    x.IpAddress,
                    x.Device,
                    createdAt = x.CreatedAt,
                    lastSeenAt = x.LastSeenAt,
                    x.ExpiresAt,
                    current = x.SessionId == sessionId
                }).ToListAsync(ct));
        });

        api.MapDelete("/sessions/{sessionId:guid}", async (Guid sessionId, IM02Access access,
            M02AuthenticationService authentication, ICurrentTenantContext tenant,
            CancellationToken ct) =>
        {
            var user = await access.CurrentUser(ct);
            await authentication.RevokeSession(tenant.TenantId, sessionId, user.UserId, false, ct);
            return Results.NoContent();
        });

        api.MapPost("/logout", async (IM02Access access, M02AuthenticationService authentication,
            ICurrentTenantContext tenant, ClaimsPrincipal principal, HttpContext context, CancellationToken ct) =>
        {
            var user = await access.CurrentUser(ct);
            if (!Guid.TryParse(principal.FindFirstValue("sid"), out var sessionId))
                throw new UnauthorizedAccessException();
            await authentication.RevokeSession(tenant.TenantId, sessionId, user.UserId, false, ct);
            ClearRefreshCookie(context);
            return Results.NoContent();
        });

        api.MapPost("/logout-all", async (IM02Access access, M02AuthenticationService authentication,
            ICurrentTenantContext tenant, HttpContext context, CancellationToken ct) =>
        {
            var user = await access.CurrentUser(ct);
            var identityUserId = user.IdentityUserId ?? throw new UnauthorizedAccessException();
            await authentication.RevokeSession(tenant.TenantId, identityUserId, identityUserId, true, ct);
            ClearRefreshCookie(context);
            return Results.NoContent();
        });
    }

    private sealed record CodeRequest(string Code, string? CurrentCode);

    private static IResult AuthResponse(HttpContext context, M02AuthResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.RefreshToken))
            context.Response.Cookies.Append("m02_refresh", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/m02/auth",
                MaxAge = TimeSpan.FromDays(14)
            });
        return Results.Ok(result with { RefreshToken = null });
    }

    private static void ClearRefreshCookie(HttpContext context) =>
        context.Response.Cookies.Delete("m02_refresh", new CookieOptions
        {
            Path = "/api/v1/m02/auth",
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        });

    private static async Task<List<(Guid RoleId, string ScopeType, Guid? ScopeId)>> ResolveGrants(
        BerynxDbContext db, RoleGrant[] grants, Guid tenantId, CancellationToken ct)
    {
        var output = new List<(Guid, string, Guid?)>();
        foreach (var grant in grants)
        {
            M02Rules.ValidateScope(grant.ScopeType, grant.ScopeId);
            var role = await db.Set<M02Role>().SingleOrDefaultAsync(x => x.RoleKey == grant.RoleKey, ct)
                ?? throw new System.ComponentModel.DataAnnotations.ValidationException("Choose an existing role.");
            if (grant.ScopeType is "Branch" or "Plant")
            {
                var tenant = await db.Set<Tenant>().SingleAsync(x => x.TenantId == tenantId, ct);
                var setup = OnboardingStore.Decode(tenant);
                var valid = grant.ScopeType == "Branch"
                    ? setup.Branches.Any(x => x.Id == grant.ScopeId)
                    : setup.Plants.Any(x => x.Id == grant.ScopeId);
                if (!valid)
                    throw new System.ComponentModel.DataAnnotations.ValidationException("The selected branch or plant is not in this tenant.");
            }
            output.Add((role.RoleId, grant.ScopeType, grant.ScopeId));
        }
        return output;
    }

    private static async Task EnsureCanAssignRoles(BerynxDbContext db, IM02Access access, Guid actorUserId,
        IEnumerable<(Guid RoleId, string ScopeType, Guid? ScopeId)> targetGrants, CancellationToken ct)
    {
        var grants = targetGrants.ToArray();
        var roleIds = grants.Select(g => g.RoleId).Distinct().ToArray();
        var permissions = await db.Set<M02RolePermission>()
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => new { x.RoleId, x.PermissionCode }).ToListAsync(ct);
        await EnsureCanGrantPermissions(db, actorUserId, permissions.Select(x => x.PermissionCode), ct);
        foreach (var grant in grants)
        {
            Guid? branchId = grant.ScopeType == "Branch" ? grant.ScopeId : null;
            Guid? plantId = grant.ScopeType == "Plant" ? grant.ScopeId : null;
            Guid? ownerId = grant.ScopeType == "Own" ? actorUserId : null;
            foreach (var permission in permissions.Where(x => x.RoleId == grant.RoleId)
                         .Select(x => x.PermissionCode).Distinct(StringComparer.Ordinal))
                if (!await access.CanAccess(actorUserId, permission, branchId, plantId, ownerId, ct))
                    throw new UnauthorizedAccessException("You cannot assign a permission outside your own data scope.");
        }
    }

    private static async Task<M02User> RequireTenantWide(IM02Access access, string permissionCode,
        CancellationToken ct)
    {
        var current = await access.CurrentUser(ct);
        if (!await access.CanAccess(current.UserId, permissionCode, null, null, null, ct))
            throw new UnauthorizedAccessException("Tenant-wide access is required for this operation.");
        return current;
    }

    private static async Task EnsureCanGrantPermissions(BerynxDbContext db, Guid actorUserId,
        IEnumerable<string> targetPermissions, CancellationToken ct)
    {
        var allowed = await (
            from assignment in db.Set<M02UserRole>()
            join grant in db.Set<M02RolePermission>()
                on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
            where assignment.UserId == actorUserId
            select grant.PermissionCode
        ).Distinct().ToListAsync(ct);
        if (targetPermissions.Except(allowed, StringComparer.Ordinal).Any())
            throw new UnauthorizedAccessException("You cannot grant permissions you do not hold yourself.");
    }

    private static void ValidateRole(RoleWrite request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100 ||
            request.Description?.Length > 500 || request.Permissions is null)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Enter a role name, description and permissions.");
        foreach (var permission in request.Permissions)
            M02Rules.ValidatePermission(permission);
    }

    private static void ValidateRule(RuleWrite request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120 ||
            string.IsNullOrWhiteSpace(request.EntityType) || request.EntityType.Trim().Length > 80 ||
            request.AmountGreaterThan < 0 || request.DiscountPercentGreaterThan is < 0 or > 100 ||
            request.MarginPercentBelow is < 0 or > 100 ||
            !request.AmountGreaterThan.HasValue && !request.DiscountPercentGreaterThan.HasValue &&
            !request.MarginPercentBelow.HasValue && string.IsNullOrWhiteSpace(request.CreditStatus) ||
            request.Levels is null || request.Levels.Length == 0 ||
            request.Levels.Select(x => x.Level).Order().Where((level, index) => level != index + 1).Any())
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "Rules need a condition and contiguous approval levels beginning at one.");
        foreach (var level in request.Levels)
            if (level.SlaMinutes < 1 ||
                (level.ApproverRoleId.HasValue == level.ApproverUserId.HasValue) ||
                (level.EscalationRoleId.HasValue && level.EscalationUserId.HasValue))
                throw new System.ComponentModel.DataAnnotations.ValidationException(
                    "Each level needs one approver, an SLA and at most one escalation target.");
    }

    private static void AddLevels(BerynxDbContext db, Guid tenantId, Guid ruleId, IEnumerable<RuleLevelWrite> levels)
    {
        foreach (var level in levels)
            db.Add(new M02ApprovalLevel
            {
                TenantId = tenantId, RuleId = ruleId, Level = level.Level,
                ApproverRoleId = level.ApproverRoleId, ApproverUserId = level.ApproverUserId,
                SlaMinutes = level.SlaMinutes, EscalationRoleId = level.EscalationRoleId,
                EscalationUserId = level.EscalationUserId
            });
    }

    private static void AddPermissions(BerynxDbContext db, Guid tenantId, Guid roleId,
        IEnumerable<string> permissions)
    {
        foreach (var permission in permissions.Distinct(StringComparer.Ordinal))
            db.Add(new M02RolePermission { TenantId = tenantId, RoleId = roleId, PermissionCode = permission });
    }

    private static void AddNotification(BerynxDbContext db, Guid tenantId, Guid recipientId, string kind,
        Guid entityId, object payload) =>
        db.Add(new M02Notification
        {
            TenantId = tenantId, NotificationId = Guid.NewGuid(), RecipientUserId = recipientId,
            Kind = kind, EntityId = entityId, Payload = JsonSerializer.Serialize(payload, Json),
            CreatedAt = DateTimeOffset.UtcNow
        });

    private static void Audit(BerynxDbContext db, Guid tenantId, M02User actor, string action,
        string entity, Guid? entityId, object? before, object? after, string? reason = null) =>
        db.Add(new M02AuditEvent
        {
            TenantId = tenantId, EventId = Guid.NewGuid(), ActorUserId = actor.UserId,
            ActorSubject = actor.ProviderSubject ?? actor.UserId.ToString(),
            Action = action, EntityType = entity, EntityId = entityId,
            Before = before is null ? "{}" : JsonSerializer.Serialize(before, Json),
            After = after is null ? "{}" : JsonSerializer.Serialize(after, Json), Reason = reason,
            At = DateTimeOffset.UtcNow
        });

    private static object UserSnapshot(M02User user) => new
    {
        user.Email, user.Mobile, user.DisplayName, user.Status, user.InviteExpiresAt, user.Version
    };

    private static object RoleSnapshot(M02Role role, IEnumerable<string> permissions) => new
    {
        role.Name, role.Description, role.IsSystem, role.Version, permissions
    };

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static string? NormalizeMobile(string? mobile) =>
        string.IsNullOrWhiteSpace(mobile) ? null : mobile.Trim();

    private static string Csv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

    private static string RedactFinancial(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output))
            WriteRedacted(writer, document.RootElement);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static void WriteRedacted(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name.Replace("_", "", StringComparison.Ordinal)
                        .Replace("-", "", StringComparison.Ordinal);
                    if (name.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("margin", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("profit", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("purchase", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("price", StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("rate", StringComparison.OrdinalIgnoreCase))
                        continue;
                    writer.WritePropertyName(property.Name);
                    WriteRedacted(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteRedacted(writer, item);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
