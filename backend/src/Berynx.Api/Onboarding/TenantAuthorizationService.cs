using System.Security.Claims;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Onboarding;

public sealed class TenantAuthorizationService(
    BerynxDbContext db,
    TenantEntitlementService entitlements,
    ICurrentTenantContext tenant,
    IHttpContextAccessor http)
{
    public async Task<M02User> CurrentUser(CancellationToken cancellationToken = default)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(principal.FindFirstValue("sub"), out var identityUserId) ||
            !Guid.TryParse(principal.FindFirstValue("user_id"), out var userId))
            throw new UnauthorizedAccessException();

        var current = await db.Set<M02User>().SingleOrDefaultAsync(x =>
            x.UserId == userId && x.IdentityUserId == identityUserId &&
            x.TenantId == tenant.TenantId && x.Status == "Active", cancellationToken);
        return current ?? throw new UnauthorizedAccessException();
    }

    public async Task RequireModule(
        string moduleCode,
        bool write,
        bool cancelledExport = false,
        CancellationToken cancellationToken = default)
    {
        await CurrentUser(cancellationToken);
        await entitlements.RequireModule(moduleCode, write, cancelledExport, cancellationToken);
    }

    public async Task Require(string moduleCode, string permissionCode,
        CancellationToken cancellationToken = default)
    {
        await RequireModule(moduleCode, IsWritePermission(permissionCode), cancellationToken: cancellationToken);
        var user = await CurrentUser(cancellationToken);
        if (!await HasPermission(user.UserId, permissionCode, cancellationToken))
            throw new UnauthorizedAccessException("This action is not permitted.");
    }

    public async Task<bool> HasPermission(Guid userId, string permissionCode,
        CancellationToken cancellationToken = default) =>
        await (
            from user in db.Set<M02User>()
            join assignment in db.Set<M02UserRole>()
                on new { user.TenantId, user.UserId } equals new { assignment.TenantId, assignment.UserId }
            join grant in db.Set<M02RolePermission>()
                on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
            where user.UserId == userId && user.Status == "Active" &&
                  user.TenantId == tenant.TenantId && grant.PermissionCode == permissionCode
            select assignment.AssignmentId
        ).AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<(string ScopeType, Guid? ScopeId)>> Scopes(
        Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from user in db.Set<M02User>()
            join assignment in db.Set<M02UserRole>()
                on new { user.TenantId, user.UserId } equals new { assignment.TenantId, assignment.UserId }
            join grant in db.Set<M02RolePermission>()
                on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
            where user.UserId == userId && user.Status == "Active" &&
                  user.TenantId == tenant.TenantId && grant.PermissionCode == permissionCode
            select new { assignment.ScopeType, assignment.ScopeId }
        ).ToListAsync(cancellationToken);
        return rows.Select(x => (x.ScopeType, x.ScopeId)).ToArray();
    }

    public async Task<bool> CanAccess(
        string moduleCode,
        Guid userId,
        string permissionCode,
        Guid? branchId,
        Guid? plantId,
        Guid? ownerId,
        CancellationToken cancellationToken = default,
        bool cancelledExport = false,
        bool? write = null)
    {
        await RequireModule(moduleCode, write ?? IsWritePermission(permissionCode),
            cancelledExport, cancellationToken);
        var scopes = await (
            from user in db.Set<M02User>()
            join assignment in db.Set<M02UserRole>()
                on new { user.TenantId, user.UserId } equals new { assignment.TenantId, assignment.UserId }
            join grant in db.Set<M02RolePermission>()
                on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
            where user.UserId == userId && user.Status == "Active" &&
                  user.TenantId == tenant.TenantId && grant.PermissionCode == permissionCode
            select new { assignment.ScopeType, assignment.ScopeId }
        ).ToListAsync(cancellationToken);
        return scopes.Any(scope => scope.ScopeType switch
        {
            "All" => true,
            "Branch" => branchId == scope.ScopeId,
            "Plant" => plantId == scope.ScopeId,
            "Own" => ownerId == userId,
            _ => false
        });
    }

    private static bool IsWritePermission(string permissionCode) =>
        !permissionCode.EndsWith(".view", StringComparison.Ordinal) &&
        !permissionCode.EndsWith(".read", StringComparison.Ordinal) &&
        !permissionCode.StartsWith("global.view_", StringComparison.Ordinal);
}
