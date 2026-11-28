using Berynx.Domain.Accounting;
using Berynx.Domain.Onboarding;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Api.Accounting;

internal static class M12LedgerMappingResolver
{
    public static async Task<IReadOnlyDictionary<string, string>> Resolve(
        BerynxDbContext db, Setup setup, Guid branchId, string gstin, DateOnly date,
        IEnumerable<string> requiredCodes, CancellationToken ct)
    {
        var codes = requiredCodes.Distinct(StringComparer.Ordinal).ToArray();
        var active = await db.Set<M12LedgerMapping>().AsNoTracking()
            .Where(x => x.BranchId == branchId && x.Gstin == gstin && x.IsActive &&
                        x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date) &&
                        codes.Contains(x.MappingCode))
            .ToListAsync(ct);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var code in codes)
        {
            var matching = active.Where(x => x.MappingCode == code).ToArray();
            if (matching.Length != 1)
                throw new InvalidOperationException($"Exactly one active '{code}' ledger mapping must cover {date:yyyy-MM-dd} for this branch and GSTIN.");
            var ledgerCode = matching[0].LedgerCode;
            var ledger = (setup.Ledgers ?? []).SingleOrDefault(x => x.IsActive &&
                x.Code.Equals(ledgerCode, StringComparison.OrdinalIgnoreCase));
            if (ledger is null)
                throw new InvalidOperationException($"Mapped ledger '{ledgerCode}' is not an active tenant account.");
            M12LedgerMappingRules.RequireLedgerGroup(code, ledger.Group);
            result.Add(code, ledgerCode);
        }
        return result;
    }
}