using Berynx.Api.Receivables;
using Berynx.Application.Tenancy;
using Berynx.Domain.Receivables;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Berynx.UnitTests.Receivables;

[TestFixture]
public sealed class PostedReceiptAllocationQueryTests
{
    [Test]
    public void Build_translates_posted_receipt_join_and_grouping_with_tenant_filters()
    {
        using var db = CreateContext();

        var sql = PostedReceiptAllocationQuery.Build(db).ToQueryString();

        StringAssert.Contains("m11_receipt_allocation", sql);
        StringAssert.Contains("m11_receipt", sql);
        StringAssert.Contains("JOIN", sql);
        StringAssert.Contains("GROUP BY", sql);
        StringAssert.Contains("sum(", sql.ToLowerInvariant());
        StringAssert.Contains("Posted", sql);
        Assert.That(sql.Split("TenantId").Length - 1, Is.GreaterThanOrEqualTo(3),
            "The grouped allocation query must retain tenant predicates on both source sets and the join.");
    }

    [Test]
    public void Open_invoice_query_translates_customer_branch_and_posted_state_scope()
    {
        using var db = CreateContext();
        var customerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        IReadOnlyList<(string ScopeType, Guid? ScopeId)> scopes = [("Branch", branchId)];

        var sql = M11OpenInvoiceQuery.Build(db, customerId, branchId, scopes).ToQueryString();

        StringAssert.Contains("m10_invoice", sql);
        StringAssert.Contains("CustomerPartyId", sql);
        StringAssert.Contains("BranchId", sql);
        StringAssert.Contains("Posted", sql);
        StringAssert.Contains("TenantId", sql);
    }

    [Test]
    public void Receipt_allocation_model_keeps_tenant_invoice_and_receipt_indexes_and_relationships()
    {
        using var db = CreateContext();
        var entity = db.Model.FindEntityType(typeof(M11ReceiptAllocation))!;
        var indexes = entity.GetIndexes().Select(index =>
            index.Properties.Select(property => property.Name).ToArray()).ToArray();

        Assert.That(indexes.Select(index => string.Join(",", index)),
            Does.Contain($"{nameof(M11ReceiptAllocation.TenantId)},{nameof(M11ReceiptAllocation.ReceiptId)}"));
        Assert.That(indexes.Select(index => string.Join(",", index)),
            Does.Contain($"{nameof(M11ReceiptAllocation.TenantId)},{nameof(M11ReceiptAllocation.InvoiceId)}"));
        Assert.That(entity.GetForeignKeys().SelectMany(key => key.Properties)
            .Select(property => property.Name), Does.Contain(nameof(M11ReceiptAllocation.TenantId)));
    }

    private static BerynxDbContext CreateContext()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Mock<ICurrentTenantContext>();
        tenant.SetupGet(context => context.TenantId).Returns(tenantId);
        var options = new DbContextOptionsBuilder<BerynxDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=translation_only;Username=unused;Password=unused")
            .Options;
        return new BerynxDbContext(options, tenant.Object);
    }
}
