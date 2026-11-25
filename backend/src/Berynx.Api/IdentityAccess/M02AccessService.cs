using System.Security.Claims;
using Berynx.Api.Onboarding;
using Berynx.Application.IdentityAccess;
using Berynx.Application.Tenancy;
using Berynx.Domain.IdentityAccess;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.IdentityAccess;

public sealed class M02AccessService : IM02Access
{
    private readonly BerynxDbContext? db;
    private readonly IHttpContextAccessor? http;
    private readonly TenantAuthorizationService? authorization;

    public M02AccessService(TenantAuthorizationService authorization) =>
        this.authorization = authorization;

    // Used only by background approval processing, which already carries an
    // explicit tenant-scoped context and does not have an HTTP principal.
    public M02AccessService(BerynxDbContext db, IHttpContextAccessor http)
    {
        this.db = db;
        this.http = http;
    }

    public M02AccessService(
        BerynxDbContext db, ICurrentTenantContext _, IHttpContextAccessor http)
        : this(db, http) { }

    public async Task<M02User> CurrentUser(CancellationToken cancellationToken = default)
    {
        if (authorization is not null)
            return await authorization.CurrentUser(cancellationToken);
        var principal = http?.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(principal.FindFirstValue("sub"), out var identityUserId) ||
            !Guid.TryParse(principal.FindFirstValue("user_id"), out var userId))
            throw new UnauthorizedAccessException();

        var current = await db!.Set<M02User>().SingleOrDefaultAsync(x =>
            x.UserId == userId && x.IdentityUserId == identityUserId && x.Status == "Active",
            cancellationToken);
        return current ?? throw new UnauthorizedAccessException();
    }

    public async Task Require(string permissionCode, CancellationToken cancellationToken = default)
    {
        if (authorization is not null)
        {
            await authorization.Require("M02", permissionCode, cancellationToken);
            return;
        }
        var user = await CurrentUser(cancellationToken);
        if (!await HasPermission(user.UserId, permissionCode, cancellationToken))
            throw new UnauthorizedAccessException("This action is not permitted.");
    }

    public async Task<bool> HasPermission(Guid userId, string permissionCode,
        CancellationToken cancellationToken = default)
    {
        if (authorization is not null)
            return await authorization.HasPermission(userId, permissionCode, cancellationToken);
        return await (
            from user in db!.Set<M02User>()
            join assignment in db!.Set<M02UserRole>()
                on new { user.TenantId, user.UserId } equals new { assignment.TenantId, assignment.UserId }
            join grant in db!.Set<M02RolePermission>()
                on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
            where user.UserId == userId && user.Status == "Active" && grant.PermissionCode == permissionCode
            select assignment.AssignmentId
        ).AnyAsync(cancellationToken);
    }

    public async Task<bool> CanAccess(Guid userId, string permissionCode, Guid? branchId, Guid? plantId,
        Guid? ownerId, CancellationToken cancellationToken = default)
    {
        if (authorization is not null)
            return await authorization.CanAccess("M02", userId, permissionCode,
                branchId, plantId, ownerId, cancellationToken);
        var scopes = await (
            from user in db!.Set<M02User>()
            join assignment in db!.Set<M02UserRole>()
                on new { user.TenantId, user.UserId } equals new { assignment.TenantId, assignment.UserId }
            join grant in db!.Set<M02RolePermission>()
                on new { assignment.TenantId, assignment.RoleId } equals new { grant.TenantId, grant.RoleId }
            where user.UserId == userId && user.Status == "Active" && grant.PermissionCode == permissionCode
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

}
