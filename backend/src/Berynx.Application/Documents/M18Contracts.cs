using Berynx.Domain.Delivery;
using Berynx.Domain.Documents;

namespace Berynx.Application.Documents;

public sealed record M18DocumentUploadPolicy(long MaxFileBytes, string[] AllowedContentTypes);
public sealed record M18DocumentOperationResult(
    Guid DocumentId, string Purpose, int VersionNumber, long SizeBytes,
    string ScanStatus, string StorageStatus, string? Detail, bool IsCurrent);
public sealed record M18DocumentContent(Stream Content, string ContentType, string FileName, long SizeBytes);

public interface IM18MalwareScanner
{
    Task<M18ScanVerdict> ScanAsync(Stream content, CancellationToken cancellationToken);
}

public enum M18ScanVerdict { Clean, Infected }

public sealed class M18ScannerUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class M18UploadRejectedException(string message, int statusCode)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>Storage abstraction; cloud object-store adapters can replace the private filesystem implementation.</summary>
public interface IM18DocumentStorage
{
    Task<long> WriteQuarantinedAsync(Guid tenantId, Guid documentId, Stream source, long maximumBytes, CancellationToken cancellationToken);
    Task<Stream> OpenQuarantinedReadAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
    Task WriteCleanAsync(Guid tenantId, Guid documentId, ReadOnlyMemory<byte> protectedContent, CancellationToken cancellationToken);
    Task<Stream> OpenCleanReadAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
    Task DeleteQuarantinedAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
    Task DeleteCleanAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
}

public interface IM18DocumentService
{
    M18DocumentUploadPolicy UploadPolicy { get; }
    Task<M18DocumentOperationResult> UploadTripEvidenceAsync(
        M08Trip trip, Guid actorUserId, string purpose, string? fileName, string? contentType,
        Stream content, CancellationToken cancellationToken);
    Task<IReadOnlyList<M18DocumentOperationResult>> GetPendingScansAsync(
        Guid tripId, Guid actorUserId, CancellationToken cancellationToken);
    Task<M18DocumentOperationResult> RetryScanAsync(
        Guid tripId, Guid documentId, Guid actorUserId, CancellationToken cancellationToken);
    Task<M18DocumentContent?> OpenForReviewAsync(
        Guid tripId, Guid documentId, Guid actorUserId, bool download, CancellationToken cancellationToken);
    Task RecordReviewAsync(Guid tripId, Guid actorUserId, string decision, string reason,
        IEnumerable<Guid> documentIds, CancellationToken cancellationToken);
}