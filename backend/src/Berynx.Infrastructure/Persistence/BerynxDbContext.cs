using System.Linq.Expressions;
using Berynx.Application.Tenancy;
using Berynx.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Persistence;

public sealed class BerynxDbContext(
    DbContextOptions<BerynxDbContext> options,
    ICurrentTenantContext currentTenant) : DbContext(options)
{
    private Guid CurrentTenantId => currentTenant.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType =>
                         typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType)))
        {
            var entity = Expression.Parameter(entityType.ClrType, "entity");
            var entityTenant = Expression.Property(
                entity,
                nameof(ITenantOwned.TenantId));
            var currentTenantId = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentTenantId));
            var tenantFilter = Expression.Lambda(
                Expression.Equal(entityTenant, currentTenantId),
                entity);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(tenantFilter);
        }
    }

    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantBoundary();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(
            acceptAllChangesOnSuccess: true,
            cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceTenantBoundary();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void EnforceTenantBoundary()
    {
        var tenantEntries = ChangeTracker.Entries<ITenantOwned>().ToArray();
        if (tenantEntries.Length == 0)
        {
            return;
        }

        var tenantId = currentTenant.TenantId;
        foreach (var entry in tenantEntries)
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = tenantId;
            }

            if (entry.Entity.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException(
                    "A tenant-owned entity cannot be saved outside the authenticated tenant scope.");
            }
        }
    }
}
