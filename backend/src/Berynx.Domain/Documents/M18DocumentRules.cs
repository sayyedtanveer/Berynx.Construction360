using System.Text;

namespace Berynx.Domain.Documents;

public static class M18DocumentRules
{
    public const string ScanPending = "Pending";
    public const string ScanClean = "Clean";
    public const string ScanRejected = "Rejected";
    public const string StorageQuarantined = "Quarantined";
    public const string StorageAvailable = "Available";
    public const string StorageDeleted = "Deleted";
    public const string Signature = "Signature";
    public const string Photo = "Photo";

    private static readonly HashSet<string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public static bool IsPurpose(string purpose) => purpose is Signature or Photo;

    public static bool IsAllowedContentType(string purpose, string? contentType) =>
        IsPurpose(purpose) && contentType is not null &&
        (ImageTypes.Contains(contentType) ||
         purpose == Photo && string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase));

    public static bool HasMatchingFileSignature(string contentType, ReadOnlySpan<byte> prefix)
    {
        if (string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
            return prefix.Length >= 3 && prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF;
        if (string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
            return prefix.Length >= 8 && prefix[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        if (string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase))
            return prefix.Length >= 12 && prefix[..4].SequenceEqual("RIFF"u8) && prefix[8..12].SequenceEqual("WEBP"u8);
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return prefix.Length >= 5 && prefix[..5].SequenceEqual("%PDF-"u8);
        return false;
    }

    public static string SafeDisplayName(string? fileName)
    {
        var leaf = (fileName ?? "evidence").Replace('\\', '/').Split('/').LastOrDefault() ?? "evidence";
        var safe = new string(leaf.Where(c => !char.IsControl(c) && c is not ('<' or '>' or ':' or '"' or '|' or '?' or '*')).ToArray())
            .Trim().Trim('.');
        if (safe.Length > 120) safe = safe[^120..];
        return string.IsNullOrWhiteSpace(safe) ? "evidence" : safe;
    }

    public static void ValidateUpload(string purpose, string? contentType, long maxFileBytes)
    {
        if (!IsPurpose(purpose)) throw new ArgumentException("Choose Signature or Photo evidence.", nameof(purpose));
        if (!IsAllowedContentType(purpose, contentType))
            throw new ArgumentException("Unsupported evidence type. Use JPEG, PNG or WebP; PDF is allowed for Photo evidence.", nameof(contentType));
        if (maxFileBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxFileBytes));
    }

    public static bool CanPreview(M18Document document) =>
        document.ScanStatus == ScanClean && document.StorageStatus == StorageAvailable && document.SizeBytes > 0;

    public static bool CanBindToTrip(M18Document document, Guid tenantId, Guid tripId, Guid branchId, string purpose) =>
        document.TenantId == tenantId && document.TripId == tripId && document.BranchId == branchId &&
        document.Purpose == purpose && document.IsCurrent && CanPreview(document);
}