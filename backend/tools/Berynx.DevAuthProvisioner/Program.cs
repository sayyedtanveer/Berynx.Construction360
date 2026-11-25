using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

if (!string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development",
        StringComparison.Ordinal) ||
    !string.Equals(Environment.GetEnvironmentVariable("BERYNX_ENABLE_DEV_AUTH_PROVISIONING"), "1",
        StringComparison.Ordinal))
{
    Console.Error.WriteLine(
        "Refusing provisioning. Set ASPNETCORE_ENVIRONMENT=Development and BERYNX_ENABLE_DEV_AUTH_PROVISIONING=1 explicitly.");
    return 2;
}

if (args.Length != 0 && (args.Length != 2 || args[0] != "--reset-password"))
{
    Console.Error.WriteLine(
        "Usage: run without arguments to provision fixtures, or --reset-password <local-fixture-email> to reset one.");
    return 2;
}

var resetEmail = args.Length == 2 ? args[1] : null;
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Write("Local development database connection string (input hidden): ");
    connectionString = ReadSecret();
}

NpgsqlConnectionStringBuilder connection;
try
{
    connection = new NpgsqlConnectionStringBuilder(connectionString);
}
catch (ArgumentException)
{
    Console.Error.WriteLine("The connection string is invalid.");
    return 2;
}

if (connection.Host is not ("localhost" or "127.0.0.1" or "::1") ||
    !string.Equals(connection.Database, "berynx_construction_dev", StringComparison.Ordinal))
{
    Console.Error.WriteLine(
        "Refusing provisioning: this command is restricted to localhost database berynx_construction_dev.");
    return 2;
}

var tenantScope = new MutableTenantContext();
UserDefinition[] userDefinitions =
[
    new("platform.admin@berynx.local", "Local platform administrator", "TenantAdmin", true),
    new("tenant.admin@berynx.local", "Local tenant administrator", "TenantAdmin"),
    new("branch.manager@berynx.local", "Local branch manager", "BranchManager"),
    new("sales.manager@berynx.local", "Local sales manager", "SalesManager"),
    new("procurement.manager@berynx.local", "Local procurement manager", "ProcurementManager"),
    new("fleet.manager@berynx.local", "Local fleet manager", "FleetManager"),
    new("accountant@berynx.local", "Local accountant", "Accountant"),
    new("read.only@berynx.local", "Local read-only user", "ReadOnly")
];
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(tenantScope);
services.AddScoped<ICurrentTenantContext>(provider => provider.GetRequiredService<MutableTenantContext>());
services.AddDbContext<BerynxDbContext>(options => options.UseNpgsql(connection.ConnectionString));
services.AddIdentityCore<M02IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<BerynxDbContext>()
    .AddDefaultTokenProviders();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<BerynxDbContext>();

try
{
    if (resetEmail is not null)
    {
        var definition = userDefinitions.SingleOrDefault(user =>
            string.Equals(user.Email, resetEmail, StringComparison.OrdinalIgnoreCase));
        if (definition is null)
        {
            Console.Error.WriteLine("Password reset is restricted to the documented local fixture accounts.");
            return 2;
        }

        var resetIdentityManager = scope.ServiceProvider.GetRequiredService<UserManager<M02IdentityUser>>();
        var identity = await resetIdentityManager.FindByEmailAsync(definition.Email);
        if (identity is null || !identity.EmailConfirmed)
        {
            Console.Error.WriteLine(
                $"No confirmed local Identity account exists for {definition.Email}. No password was changed.");
            return 2;
        }
        var memberships = await db.Set<M02User>().IgnoreQueryFilters()
            .Where(user => user.IdentityUserId == identity.Id)
            .Select(user => new { user.TenantId, user.Status })
            .ToListAsync();
        if (memberships.Count == 0 || memberships.Any(membership => membership.Status != "Active"))
        {
            Console.Error.WriteLine(
                "The fixture account must have an active tenant membership before its password can be reset.");
            return 2;
        }

        var resetToken = await resetIdentityManager.GeneratePasswordResetTokenAsync(identity);
        var resetResult = await resetIdentityManager.ResetPasswordAsync(identity, resetToken, ReadPassword("New"));
        if (!resetResult.Succeeded)
        {
            Console.Error.WriteLine(string.Join(Environment.NewLine,
                resetResult.Errors.Select(error => error.Description)));
            return 2;
        }

        EnsureIdentitySuccess(await resetIdentityManager.UpdateSecurityStampAsync(identity));
        EnsureIdentitySuccess(await resetIdentityManager.SetLockoutEndDateAsync(identity, null));
        EnsureIdentitySuccess(await resetIdentityManager.ResetAccessFailedCountAsync(identity));

        var resetAt = DateTimeOffset.UtcNow;
        var activeSessions = db.Set<M02AuthSession>().IgnoreQueryFilters()
            .Where(session => session.IdentityUserId == identity.Id && session.RevokedAt == null);
        var sessionIds = await activeSessions.Select(session => session.SessionId).ToArrayAsync();
        await activeSessions.ExecuteUpdateAsync(update =>
            update.SetProperty(session => session.RevokedAt, resetAt));
        if (sessionIds.Length > 0)
        {
            await db.Set<M02RefreshToken>().IgnoreQueryFilters()
                .Where(token => sessionIds.Contains(token.SessionId) && token.RevokedAt == null)
                .ExecuteUpdateAsync(update =>
                    update.SetProperty(token => token.RevokedAt, resetAt));
        }

        foreach (var membership in memberships)
        {
            tenantScope.TenantId = membership.TenantId;
            db.Add(new M02AuditEvent
            {
                TenantId = membership.TenantId,
                EventId = Guid.NewGuid(),
                ActorSubject = "local-development-password-reset",
                Action = "Development password reset",
                EntityType = "IdentityUser",
                EntityId = identity.Id,
                Before = "{}",
                After = System.Text.Json.JsonSerializer.Serialize(new
                {
                    revokedSessionCount = sessionIds.Length
                }),
                Reason = "Operator-initiated local development reset",
                At = resetAt
            });
            await db.SaveChangesAsync();
        }

        Console.WriteLine(
            $"Password reset for {definition.Email}; {sessionIds.Length} active session(s) revoked. The password was not printed or stored by this tool.");
        return 0;
    }

    var tenantValue = Environment.GetEnvironmentVariable("BERYNX_DEV_TENANT_ID");
    if (string.IsNullOrWhiteSpace(tenantValue))
    {
        Console.Write("Tenant ID from DEV-AUTH/04-verification.sql: ");
        tenantValue = Console.ReadLine();
    }
    if (!Guid.TryParse(tenantValue, out var tenantId))
    {
        Console.Error.WriteLine("A valid BERYNX_DEV_TENANT_ID is required.");
        return 2;
    }

    var tenant = await db.Set<Tenant>().IgnoreQueryFilters()
        .SingleOrDefaultAsync(row => row.TenantId == tenantId);
    if (tenant is null || tenant.Status is not ("Trial" or "Active"))
    {
        Console.Error.WriteLine("The selected tenant does not exist or is not active/trial.");
        return 2;
    }

    tenantScope.TenantId = tenantId;
    var setup = OnboardingStore.Decode(tenant);
    var requiredRoles = userDefinitions.Select(definition => definition.RoleKey).Distinct().ToArray();
    var availableRoles = await db.Set<M02Role>().Select(row => row.RoleKey).ToListAsync();
    var missingRoles = requiredRoles.Except(availableRoles, StringComparer.Ordinal).ToArray();
    if (missingRoles.Length > 0)
    {
        Console.Error.WriteLine(
            $"Missing M02 role templates: {string.Join(", ", missingRoles)}. Run DEV-AUTH/02-roles-permissions.sql first.");
        return 2;
    }
    if (!setup.Branches.Any())
    {
        Console.Error.WriteLine(
            "The selected tenant has no configured branch for the BranchManager fixture. Select the local development tenant or configure a branch in M01.");
        return 2;
    }

    var identities = scope.ServiceProvider.GetRequiredService<UserManager<M02IdentityUser>>();
    var now = DateTimeOffset.UtcNow;
    var usersProvisioned = 0;
    var membershipsAdded = 0;
    var assignmentsAdded = 0;
    Guid? platformAdminIdentityId = null;

    foreach (var definition in userDefinitions)
    {
        var identity = await identities.FindByEmailAsync(definition.Email);
        if (identity is null)
        {
            Console.WriteLine($"Create local-only account {definition.Email}");
            var password = ReadPassword("Initial");
            identity = new M02IdentityUser
            {
                UserName = definition.Email,
                Email = definition.Email,
                EmailConfirmed = true,
                LockoutEnabled = true
            };
            var created = await identities.CreateAsync(identity, password);
            if (!created.Succeeded)
            {
                Console.Error.WriteLine(string.Join(Environment.NewLine, created.Errors.Select(error => error.Description)));
                return 2;
            }
            usersProvisioned++;
        }
        else if (!identity.EmailConfirmed || string.IsNullOrWhiteSpace(identity.PasswordHash))
        {
            Console.Error.WriteLine(
                $"Existing identity {definition.Email} is not login-ready. No account state or password was changed.");
            return 2;
        }
        else if (await identities.IsLockedOutAsync(identity))
        {
            Console.Error.WriteLine($"Existing identity {definition.Email} is locked out; no account state was changed.");
            return 2;
        }

        if (definition.IsPlatformAdministrator)
            platformAdminIdentityId = identity.Id;

        var membership = await db.Set<M02User>().SingleOrDefaultAsync(row =>
            row.Email != null && row.Email.ToUpper() == definition.Email.ToUpper());
        if (membership is null)
        {
            membership = new M02User
            {
                TenantId = tenantId,
                UserId = Guid.NewGuid(),
                IdentityUserId = identity.Id,
                ProviderSubject = identity.Id.ToString("D"),
                Email = definition.Email,
                DisplayName = definition.DisplayName,
                Status = "Active",
                AcceptedAt = now,
                InvitationAvailableAt = now,
                Version = 1
            };
            db.Add(membership);
            await db.SaveChangesAsync();
            membershipsAdded++;
        }
        else if (membership.Status != "Active" ||
                 membership.IdentityUserId is { } linkedIdentity && linkedIdentity != identity.Id)
        {
            Console.Error.WriteLine(
                $"Existing tenant membership for {definition.Email} is inactive or linked to another identity; it was not changed.");
            return 2;
        }
        else if (membership.IdentityUserId is null)
        {
            membership.IdentityUserId = identity.Id;
            membership.ProviderSubject = identity.Id.ToString("D");
            membership.Version++;
            await db.SaveChangesAsync();
        }

        var role = await db.Set<M02Role>().SingleOrDefaultAsync(row => row.RoleKey == definition.RoleKey);
        if (role is null) throw new InvalidOperationException("A preflighted M02 role disappeared.");

        var scopes = definition.RoleKey == "BranchManager"
            ? setup.Branches.Select(branch => (Type: "Branch", Id: (Guid?)branch.Id)).ToArray()
            : new[] { (Type: "All", Id: (Guid?)null) };
        if (scopes.Length == 0)
        {
            Console.Error.WriteLine(
                "The selected tenant has no configured branch for the BranchManager fixture. Select the local development tenant or configure a branch in M01.");
            return 2;
        }

        foreach (var assignmentScope in scopes)
        {
            var exists = await db.Set<M02UserRole>().AnyAsync(row =>
                row.UserId == membership.UserId && row.RoleId == role.RoleId &&
                row.ScopeType == assignmentScope.Type && row.ScopeId == assignmentScope.Id);
            if (exists) continue;
            db.Add(new M02UserRole
            {
                TenantId = tenantId,
                AssignmentId = Guid.NewGuid(),
                UserId = membership.UserId,
                RoleId = role.RoleId,
                ScopeType = assignmentScope.Type,
                ScopeId = assignmentScope.Id,
                AssignedAt = now,
                AssignedBy = "local-development-provisioner"
            });
            assignmentsAdded++;
        }
        await db.SaveChangesAsync();
    }

    Console.WriteLine($"Provisioning complete. New Identity users: {usersProvisioned}; memberships: {membershipsAdded}; role assignments: {assignmentsAdded}.");
    if (platformAdminIdentityId is { } platformId)
    {
        Console.WriteLine("To grant the local PlatformAdmin entitlement, set this trusted backend environment value before starting the API:");
        Console.WriteLine($"PlatformAdmin__IdentityUserIds__0={platformId:D}");
        Console.WriteLine("This value is server configuration; never accept it from a browser or commit it to source.");
    }
    return 0;
}
catch (Exception error) when (error is NpgsqlException or InvalidOperationException or DbUpdateException)
{
    Console.Error.WriteLine($"Provisioning stopped without resetting existing passwords: {error.Message}");
    return 2;
}

static string ReadPassword(string description)
{
    while (true)
    {
        Console.Write($"{description} password (input hidden): ");
        var password = ReadSecret();
        Console.Write($"Confirm {description.ToLowerInvariant()} password (input hidden): ");
        var confirmation = ReadSecret();
        if (password == confirmation) return password;
        Console.WriteLine("Passwords did not match. Try again.");
    }
}

static void EnsureIdentitySuccess(IdentityResult result)
{
    if (!result.Succeeded)
        throw new InvalidOperationException(
            string.Join(Environment.NewLine, result.Errors.Select(error => error.Description)));
}

static string ReadSecret()
{
    var value = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key is ConsoleKey.Enter) break;
        if (key.Key is ConsoleKey.Backspace)
        {
            if (value.Length > 0) value.Length--;
            continue;
        }
        if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
    }
    Console.WriteLine();
    return value.ToString();
}

sealed record UserDefinition(string Email, string DisplayName, string RoleKey, bool IsPlatformAdministrator = false);

sealed class MutableTenantContext : ICurrentTenantContext
{
    public Guid TenantId { get; set; }
}
