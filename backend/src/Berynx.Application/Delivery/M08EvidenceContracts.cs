using Berynx.Domain.Documents;

namespace Berynx.Application.Delivery;

/// <summary>Metadata for a tenant-bound, clean-scanned M18 asset. Preview paths require authenticated API access.</summary>
public sealed record M08SecureEvidenceAsset(
    Guid AssetId,
    Guid TenantId,
    string Purpose,
    string ContentType,
    string ScanStatus,
    string StorageStatus,
    string PreviewPath,
    string FileName,
    int VersionNumber,
    bool IsCurrent);

public sealed record M08SecureEvidenceReviewResult(
    bool IsAvailable,
    IReadOnlyList<M08SecureEvidenceAsset> Assets,
    string? Detail);

/// <summary>Resolves opaque M18 asset IDs to exact, tenant-bound, clean-scanned evidence metadata.</summary>
public interface IM08SecureEvidenceProvider
{
    Task<M08SecureEvidenceReviewResult> GetForReviewAsync(
        Guid tenantId,
        Guid tripId,
        IReadOnlyCollection<Guid> assetIds,
        CancellationToken cancellationToken);
}

public static class M08SecureEvidenceReviewRules
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public static bool TryParseAssetIds(
        string? signatureReference,
        string? photoReference,
        out IReadOnlyList<Guid> ids)
    {
        var references = new List<(string Value, string Purpose)>();
        if (!string.IsNullOrWhiteSpace(signatureReference)) references.Add((signatureReference, M18DocumentRules.Signature));
        if (!string.IsNullOrWhiteSpace(photoReference)) references.Add((photoReference, M18DocumentRules.Photo));
        var parsed = new List<Guid>(references.Count);
        foreach (var reference in references)
        {
            if (!Guid.TryParse(reference.Value, out var id) || id == Guid.Empty)
            {
                ids = [];
                return false;
            }
            parsed.Add(id);
        }
        ids = parsed;
        return parsed.Count > 0 && parsed.Distinct().Count() == parsed.Count;
    }

    public static bool IsReviewable(
        Guid tenantId,
        Guid tripId,
        string? signatureReference,
        string? photoReference,
        M08SecureEvidenceReviewResult result)
    {
        if (!result.IsAvailable || !TryParseAssetIds(signatureReference, photoReference, out var ids) ||
            result.Assets.Count != ids.Count || result.Assets.Select(x => x.AssetId).Distinct().Count() != result.Assets.Count)
            return false;
        var assets = result.Assets.ToDictionary(x => x.AssetId);
        if (assets.Count != ids.Count || ids.Any(id => !assets.ContainsKey(id))) return false;
        if (!string.IsNullOrWhiteSpace(signatureReference) &&
            !IsSafe(assets[Guid.Parse(signatureReference)], tenantId, tripId, M18DocumentRules.Signature)) return false;
        if (!string.IsNullOrWhiteSpace(photoReference) &&
            !IsSafe(assets[Guid.Parse(photoReference)], tenantId, tripId, M18DocumentRules.Photo)) return false;
        return true;
    }

    private static bool IsSafe(M08SecureEvidenceAsset asset, Guid tenantId, Guid tripId, string purpose)
    {
        var typeAllowed = purpose == M18DocumentRules.Signature
            ? AllowedImageTypes.Contains(asset.ContentType)
            : AllowedImageTypes.Contains(asset.ContentType) ||
              string.Equals(asset.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        var expectedPath = $"/api/v1/m08/trips/{tripId:D}/evidence/{asset.AssetId:D}/content";
        return asset.TenantId == tenantId && asset.Purpose == purpose && asset.ScanStatus == M18DocumentRules.ScanClean &&
               asset.StorageStatus == M18DocumentRules.StorageAvailable && asset.VersionNumber > 0 && asset.IsCurrent &&
               typeAllowed && string.Equals(asset.PreviewPath, expectedPath, StringComparison.Ordinal);
    }
}