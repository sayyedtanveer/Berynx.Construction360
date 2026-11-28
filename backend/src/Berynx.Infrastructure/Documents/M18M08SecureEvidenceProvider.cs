using Berynx.Application.Delivery;
using Berynx.Domain.Documents;
using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Berynx.Infrastructure.Documents;

public sealed class M18M08SecureEvidenceProvider(BerynxDbContext db) : IM08SecureEvidenceProvider
{
    public async Task<M08SecureEvidenceReviewResult> GetForReviewAsync(
        Guid tenantId, Guid tripId, IReadOnlyCollection<Guid> assetIds, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || tripId == Guid.Empty || assetIds.Count == 0 || assetIds.Count > 2)
            return new(false, [], "Evidence reference set is invalid.");
        var documents = await db.Set<M18Document>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && assetIds.Contains(x.DocumentId))
            .Select(x => new M08SecureEvidenceAsset(
                x.DocumentId, x.TenantId, x.Purpose, x.ContentType, x.ScanStatus, x.StorageStatus,
                $"/api/v1/m08/trips/{x.TripId:D}/evidence/{x.DocumentId:D}/content",
                x.OriginalFileName, x.VersionNumber, x.IsCurrent))
            .ToListAsync(cancellationToken);
        return new(true, documents, null);
    }
}