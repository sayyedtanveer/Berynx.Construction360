using Berynx.Application.Onboarding;
using Berynx.Application.Tenancy;
using Berynx.Domain.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Persistence;
public sealed class PostingNumbers(BerynxDbContext db, ICurrentTenantContext context, IBranchAccess branchAccess) : IPostingNumbers
{
    public async Task<PostedNumber> Assign(Guid postingId, Guid branchId, string document, DateOnly date, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("A posting transaction is required.");
        Workflow.Require(postingId != Guid.Empty, "Supply a stable posting ID.");
        branchAccess.RequireBranch(branchId);
        var tenantId = context.TenantId;
        var t = (await db.Set<Tenant>().FromSqlInterpolated($"SELECT * FROM onboarding_tenant WHERE \"TenantId\"={tenantId} FOR UPDATE").AsNoTracking().ToListAsync(ct)).Single();
        if (t.Status is "Suspended" or "Cancelled" || t.LiveAt == null)
            throw new UnauthorizedAccessException();
        var s = OnboardingStore.Decode(t);
        var month = s.StartMonth!.Value;
        var fy = date.Year - (date.Month < month ? 1 : 0);
        var existing = await db.Database.SqlQuery<PostedNumber>($"""
            SELECT number AS "Number", gstin AS "Gstin", branch_id AS "BranchId", fiscal_year AS "FiscalYear"
            FROM onboarding_number WHERE tenant_id={tenantId} AND document={document} AND posting_id={postingId}
            """).SingleOrDefaultAsync(ct);
        if (existing != null)
        {
            Workflow.Require(existing.BranchId == branchId && existing.FiscalYear == fy, "Posting ID already used for another branch or year.");
            return existing;
        }

        var used = await db.Database.SqlQuery<long>($"""
            SELECT count(*) AS "Value" FROM onboarding_number WHERE tenant_id={tenantId}
              AND posted_at>=date_trunc('month',now() AT TIME ZONE 'Asia/Kolkata') AT TIME ZONE 'Asia/Kolkata'
            """).SingleAsync(ct);
        SetupRules.Capacity(used, 1, s.Limits.Documents);
        var series = s.Series.SingleOrDefault(x => x.BranchId == branchId && x.Document == document) ?? throw new System.ComponentModel.DataAnnotations.ValidationException("Configure a branch series first.");
        var gstin = s.Branches.Single(b => b.Id == branchId).Gstin;
        var sequence = await db.Database.SqlQuery<long>($"""
            SELECT COALESCE(max(sequence),0)+1 AS "Value" FROM onboarding_number
              WHERE tenant_id={tenantId} AND branch_id={branchId} AND document={document} AND fiscal_year={fy}
            """).SingleAsync(ct);
        var number = SetupRules.Number(series, month, date, sequence);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO onboarding_number(tenant_id,branch_id,document,fiscal_year,posting_id,sequence,number,gstin)
            VALUES({tenantId},{branchId},{document},{fy},{postingId},{sequence},{number},{gstin})
            """, ct);
        return new(number, gstin, branchId, fy);
    }
}
