using Berynx.Application.Tenancy;
using Berynx.Application.IdentityAccess;
using Berynx.Domain.IdentityAccess;
using Berynx.Domain.Onboarding;
using Berynx.Domain.Masters;
using Berynx.Api.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Npgsql;
using NUnit.Framework;

namespace Berynx.IntegrationTests;

[NonParallelizable]
public sealed class M02AuthorizationTests
{
    private sealed record Scope(Guid TenantId) : ICurrentTenantContext;

    private readonly Guid tenantId = Guid.NewGuid();
    private readonly Guid otherTenantId = Guid.NewGuid();
    private readonly Guid branchId = Guid.NewGuid();
    private readonly Guid otherBranchId = Guid.NewGuid();
    private readonly Guid plantId = Guid.NewGuid();
    private readonly Guid otherPlantId = Guid.NewGuid();
    private readonly string schema = "m02_test_" + Guid.NewGuid().ToString("N");
    private DbContextOptions<BerynxDbContext> options = null!;
    private string? connection;
    private Guid adminUserId;

    private BerynxDbContext Open(Guid id) => new(options, new Scope(id));

    [OneTimeSetUp]
    public async Task Prepare()
    {
        var value = Environment.GetEnvironmentVariable("BERYNX_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(value))
            Assert.Ignore("Set BERYNX_TEST_POSTGRES to a disposable local integration database.");
        var builder = new NpgsqlConnectionStringBuilder(value);
        if (builder.Host is not ("localhost" or "127.0.0.1" or "::1") || builder.Database?.EndsWith("_test") != true)
            throw new InvalidOperationException("Integration fixture requires a localhost database whose name ends in _test.");
        await using var connectionHandle = new NpgsqlConnection(value);
        await connectionHandle.OpenAsync();
        await using (var create = new NpgsqlCommand("CREATE SCHEMA " + schema, connectionHandle))
            await create.ExecuteNonQueryAsync();
        connection = value;
        builder.SearchPath = schema;
        options = new DbContextOptionsBuilder<BerynxDbContext>().UseNpgsql(builder.ConnectionString).Options;

        await using (var db = Open(tenantId))
        {
            await db.Database.MigrateAsync();
            db.Add(NewTenant(tenantId));
            await db.SaveChangesAsync();
            await SeedSystemAdmin(db, tenantId);
            adminUserId = await AddActiveAdmin(db, tenantId);
        }
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        if (connection is null)
            return;
        await using var db = new NpgsqlConnection(connection);
        await db.OpenAsync();
        await using var drop = new NpgsqlCommand("DROP SCHEMA " + schema + " CASCADE", db);
        await drop.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task TenantFilterAndWriteBoundaryPreventCrossTenantAccess()
    {
        await using (var first = Open(tenantId))
            Assert.That(await first.Set<M02User>().CountAsync(), Is.EqualTo(1));

        await using (var other = Open(otherTenantId))
        {
            await other.Database.MigrateAsync();
            other.Add(NewTenant(otherTenantId));
            await other.SaveChangesAsync();
            await SeedSystemAdmin(other, otherTenantId);
            await AddActiveAdmin(other, otherTenantId);
            Assert.That(await other.Set<M02User>().CountAsync(), Is.EqualTo(1));
            other.Add(new M02User
            {
                TenantId = tenantId,
                UserId = Guid.NewGuid(),
                Email = "cross-tenant@example.invalid",
                DisplayName = "Invalid",
                Status = "Active"
            });
            Assert.Throws<UnauthorizedAccessException>(() => other.SaveChanges());
        }
    }

    [Test]
    public async Task M03MasterRowsAreTenantFilteredAndRejectCrossTenantWrites()
    {
        var partyId = Guid.NewGuid();
        await using (var first = Open(tenantId))
        {
            first.Add(new M03Party
            {
                TenantId = tenantId,
                PartyId = partyId,
                LegalName = "Tenant One Master",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await first.SaveChangesAsync();
        }

        await using var other = Open(otherTenantId);
        Assert.That(await other.Set<M03Party>().AnyAsync(x => x.PartyId == partyId), Is.False);
        other.Add(new M03Party
        {
            TenantId = tenantId,
            PartyId = Guid.NewGuid(),
            LegalName = "Cross tenant write",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        Assert.Throws<UnauthorizedAccessException>(() => other.SaveChanges());
    }

    [Test]
    public async Task DatabaseProtectsTheLastActiveTenantAdmin()
    {
        await using var db = Open(tenantId);
        var admin = await db.Set<M02User>().SingleAsync(x => x.UserId == adminUserId);
        admin.Status = "Inactive";
        admin.Version++;
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.That(error!.InnerException, Is.TypeOf<PostgresException>());
        Assert.That(((PostgresException)error.InnerException!).SqlState, Is.EqualTo("P0001"));
    }

    [Test]
    public async Task VersionTokenRejectsStaleConcurrentUserChanges()
    {
        await using var first = Open(tenantId);
        await using var second = Open(tenantId);
        var left = await first.Set<M02User>().SingleAsync(x => x.UserId == adminUserId);
        var right = await second.Set<M02User>().SingleAsync(x => x.UserId == adminUserId);
        left.DisplayName = "Updated by first writer";
        left.Version++;
        await first.SaveChangesAsync();
        right.DisplayName = "Stale writer";
        right.Version++;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Test]
    public async Task VersionTokenRejectsStaleApprovalRequestChanges()
    {
        var requestId = Guid.NewGuid();
        await using (var setup = Open(tenantId))
        {
            var role = await setup.Set<M02Role>().SingleAsync(x => x.RoleKey == "TenantAdmin");
            var ruleId = Guid.NewGuid();
            setup.Add(new M02ApprovalRule
            {
                TenantId = tenantId, RuleId = ruleId, Name = "Concurrency test",
                EntityType = "TestDocument", AmountGreaterThan = 1m, Enabled = true, Version = 1
            });
            setup.Add(new M02ApprovalLevel
            {
                TenantId = tenantId, RuleId = ruleId, Level = 1,
                ApproverRoleId = role.RoleId, SlaMinutes = 60
            });
            setup.Add(new M02ApprovalRequest
            {
                TenantId = tenantId, RequestId = requestId, RuleId = ruleId,
                SourceType = "TestDocument", SourceId = Guid.NewGuid(), RequesterUserId = adminUserId,
                Status = "Pending", CurrentLevel = 1, Amount = 2m, Snapshot = "{}",
                DueAt = DateTimeOffset.UtcNow.AddHours(1), Version = 1, CreatedAt = DateTimeOffset.UtcNow
            });
            await setup.SaveChangesAsync();
        }

        await using var first = Open(tenantId);
        await using var second = Open(tenantId);
        var firstRequest = await first.Set<M02ApprovalRequest>().SingleAsync(x => x.RequestId == requestId);
        var staleRequest = await second.Set<M02ApprovalRequest>().SingleAsync(x => x.RequestId == requestId);
        firstRequest.Status = "Approved";
        firstRequest.Version++;
        await first.SaveChangesAsync();
        staleRequest.Status = "Rejected";
        staleRequest.Version++;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var immutable = Open(tenantId);
        var approved = await immutable.Set<M02ApprovalRequest>().SingleAsync(x => x.RequestId == requestId);
        approved.Snapshot = "{\"tampered\":true}";
        approved.Version++;
        var immutableError = await Assert.ThrowsAsync<DbUpdateException>(() => immutable.SaveChangesAsync());
        Assert.That(((PostgresException)immutableError!.InnerException!).SqlState, Is.EqualTo("P0001"));
    }

    [Test]
    public async Task DatabaseRejectsChangesToSystemRoles()
    {
        await using var db = Open(tenantId);
        var role = await db.Set<M02Role>().SingleAsync(x => x.RoleKey == "TenantAdmin");
        role.Name = "Changed system role";
        role.Version++;
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.That(error!.InnerException, Is.TypeOf<PostgresException>());
    }

    [Test]
    public async Task AccessPermissionsRespectBranchPlantAndCostScopes()
    {
        var scopedUserId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        await using (var db = Open(tenantId))
        {
            foreach (var permission in new[]
                     {
                         ("approvals.view", "approvals", "view"),
                         ("global.view_cost", "global", "view_cost")
                     })
                if (!await db.Set<M02PermissionCatalog>().AnyAsync(x => x.PermissionCode == permission.Item1))
                    db.Add(new M02PermissionCatalog
                    {
                        PermissionCode = permission.Item1, Module = permission.Item2,
                        Action = permission.Item3, Description = permission.Item1
                    });
            db.Add(new M02Role
            {
                TenantId = tenantId, RoleId = roleId, RoleKey = "scope-test",
                Name = "Scope test", IsSystem = false, Version = 1
            });
            db.AddRange(
                new M02RolePermission { TenantId = tenantId, RoleId = roleId, PermissionCode = "approvals.view" },
                new M02RolePermission { TenantId = tenantId, RoleId = roleId, PermissionCode = "global.view_cost" });
            db.Add(new M02User
            {
                TenantId = tenantId, UserId = scopedUserId, Email = $"scope-{scopedUserId:N}@example.invalid",
                DisplayName = "Branch scoped", Status = "Active", Version = 1
            });
            db.AddRange(
                new M02UserRole
                {
                    TenantId = tenantId, AssignmentId = Guid.NewGuid(), UserId = scopedUserId, RoleId = roleId,
                    ScopeType = "Branch", ScopeId = branchId, AssignedAt = DateTimeOffset.UtcNow, AssignedBy = "test"
                },
                new M02UserRole
                {
                    TenantId = tenantId, AssignmentId = Guid.NewGuid(), UserId = scopedUserId, RoleId = roleId,
                    ScopeType = "Plant", ScopeId = plantId, AssignedAt = DateTimeOffset.UtcNow, AssignedBy = "test"
                });
            await db.SaveChangesAsync();
        }

        await using var scoped = Open(tenantId);
        var access = new M02AccessService(scoped, new Scope(tenantId), new HttpContextAccessor());
        Assert.That(await access.CanAccess(scopedUserId, "approvals.view", branchId, null, null), Is.True);
        Assert.That(await access.CanAccess(scopedUserId, "approvals.view", otherBranchId, null, null), Is.False);
        Assert.That(await access.CanAccess(scopedUserId, "approvals.view", null, plantId, null), Is.True);
        Assert.That(await access.CanAccess(scopedUserId, "approvals.view", null, otherPlantId, null), Is.False);
        Assert.That(await access.CanAccess(scopedUserId, "global.view_cost", branchId, null, null), Is.True);
        Assert.That(await access.CanAccess(scopedUserId, "global.view_cost", otherBranchId, null, null), Is.False);
    }

    [Test]
    public async Task ApprovalTaskReassignmentIsAuditedVersionedAndScopeBound()
    {
        var originalApproverId = Guid.NewGuid();
        var replacementApproverId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        await using var db = Open(tenantId);
        foreach (var permission in new[]
                 {
                     ("approvals.view", "approvals", "view"),
                     ("approvals.approve", "approvals", "approve")
                 })
            if (!await db.Set<M02PermissionCatalog>().AnyAsync(x => x.PermissionCode == permission.Item1))
                db.Add(new M02PermissionCatalog
                {
                    PermissionCode = permission.Item1, Module = permission.Item2,
                    Action = permission.Item3, Description = permission.Item1
                });
        db.Add(new M02Role
        {
            TenantId = tenantId, RoleId = roleId, RoleKey = "approval-scope-test",
            Name = "Approval scope test", IsSystem = false, Version = 1
        });
        db.AddRange(
            new M02RolePermission { TenantId = tenantId, RoleId = roleId, PermissionCode = "approvals.view" },
            new M02RolePermission { TenantId = tenantId, RoleId = roleId, PermissionCode = "approvals.approve" });
        foreach (var (id, name) in new[]
                 {
                     (originalApproverId, "Original approver"),
                     (replacementApproverId, "Replacement approver")
                 })
        {
            db.Add(new M02User
            {
                TenantId = tenantId, UserId = id, Email = $"approver-{id:N}@example.invalid",
                DisplayName = name, Status = "Active", Version = 1
            });
            db.Add(new M02UserRole
            {
                TenantId = tenantId, AssignmentId = Guid.NewGuid(), UserId = id, RoleId = roleId,
                ScopeType = "Branch", ScopeId = branchId, AssignedAt = DateTimeOffset.UtcNow, AssignedBy = "test"
            });
        }
        db.Add(new M02ApprovalRule
        {
            TenantId = tenantId, RuleId = ruleId, Name = "Task reassignment test",
            EntityType = "PurchaseOrder", AmountGreaterThan = 1m, Enabled = true, Version = 1
        });
        db.Add(new M02ApprovalLevel
        {
            TenantId = tenantId, RuleId = ruleId, Level = 1,
            ApproverUserId = originalApproverId, SlaMinutes = 60
        });
        db.Add(new M02ApprovalRequest
        {
            TenantId = tenantId, RequestId = requestId, RuleId = ruleId,
            SourceType = "PurchaseOrder", SourceId = Guid.NewGuid(), RequesterUserId = adminUserId,
            BranchId = branchId, PlantId = plantId, OwnerUserId = adminUserId,
            Status = "Pending", CurrentLevel = 1, Amount = 2m, Snapshot = "{}",
            DueAt = DateTimeOffset.UtcNow.AddHours(1), Version = 1, CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("sub", $"admin-subject-{tenantId:N}")], "integration-test"))
            }
        };
        var access = new M02AccessService(db, new Scope(tenantId), http);
        var provider = new M02ApprovalTaskReassignmentProvider(db, access, new Scope(tenantId), http);
        var tasks = await provider.OpenTasks(originalApproverId);
        Assert.That(tasks, Has.Count.EqualTo(1));
        await provider.Reassign(originalApproverId,
        [
            new M02TaskReassignment(provider.Provider, requestId.ToString("D"), replacementApproverId)
        ]);
        await db.SaveChangesAsync();

        Assert.That((await db.Set<M02ApprovalRequest>().SingleAsync(x => x.RequestId == requestId)).Version,
            Is.EqualTo(2));
        Assert.That((await db.Set<M02ApprovalTaskAssignment>().SingleAsync(x => x.RequestId == requestId))
            .ApproverUserId, Is.EqualTo(replacementApproverId));
        Assert.That(await db.Set<M02AuditEvent>().AnyAsync(x =>
            x.EntityId == requestId && x.Action == "Approval task reassigned"), Is.True);
        Assert.That(await provider.OpenTasks(originalApproverId), Is.Empty);
        Assert.That(await provider.OpenTasks(replacementApproverId), Has.Count.EqualTo(1));
    }

    private Tenant NewTenant(Guid id) => new()
    {
        TenantId = id,
        Name = "M02 Test",
        Plan = "Test",
        Configuration = OnboardingStore.Encode(new Setup
        {
            Branches =
            [
                new Branch(branchId, "Branch A", "Test", ""),
                new Branch(otherBranchId, "Branch B", "Test", "")
            ],
            Plants =
            [
                new Plant(plantId, branchId, "Plant A", "Test"),
                new Plant(otherPlantId, otherBranchId, "Plant B", "Test")
            ]
        })
    };

    private static async Task SeedSystemAdmin(BerynxDbContext db, Guid id)
    {
        if (!await db.Set<M02PermissionCatalog>().AnyAsync(x => x.PermissionCode == "users.edit"))
            db.Add(new M02PermissionCatalog
            {
                PermissionCode = "users.edit", Module = "users", Action = "edit", Description = "Manage users"
            });
        if (!await db.Set<M02SystemRoleTemplate>().AnyAsync(x => x.RoleKey == "TenantAdmin"))
            db.Add(new M02SystemRoleTemplate
            {
                RoleKey = "TenantAdmin", Name = "Tenant Admin", Description = "Tenant administration"
            });
        if (!await db.Set<M02SystemRolePermission>().AnyAsync(x =>
                x.RoleKey == "TenantAdmin" && x.PermissionCode == "users.edit"))
            db.Add(new M02SystemRolePermission { RoleKey = "TenantAdmin", PermissionCode = "users.edit" });
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT m02_seed_tenant({id})");
    }

    private static async Task<Guid> AddActiveAdmin(BerynxDbContext db, Guid id)
    {
        var userId = Guid.NewGuid();
        var role = await db.Set<M02Role>().SingleAsync(x => x.RoleKey == "TenantAdmin");
        db.Add(new M02User
        {
            TenantId = id, UserId = userId, Email = $"admin-{id:N}@example.invalid",
            DisplayName = "Tenant Admin", Status = "Active", Version = 1,
            ProviderSubject = $"admin-subject-{id:N}"
        });
        db.Add(new M02UserRole
        {
            TenantId = id, AssignmentId = Guid.NewGuid(), UserId = userId, RoleId = role.RoleId,
            ScopeType = "All", AssignedAt = DateTimeOffset.UtcNow, AssignedBy = "integration-test"
        });
        await db.SaveChangesAsync();
        return userId;
    }
}
