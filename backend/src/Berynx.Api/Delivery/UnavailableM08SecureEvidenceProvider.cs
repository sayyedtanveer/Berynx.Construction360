using Berynx.Application.Delivery;

namespace Berynx.Api.Delivery;

/// <summary>Fails closed until the tenant's M18 encrypted storage/scanning provider is configured.</summary>
public sealed class UnavailableM08SecureEvidenceProvider : IM08SecureEvidenceProvider
{
    public Task<M08SecureEvidenceReviewResult> GetForReviewAsync(
        Guid tenantId,
        Guid tripId,
        IReadOnlyCollection<Guid> assetIds,
        CancellationToken cancellationToken) =>
        Task.FromResult(new M08SecureEvidenceReviewResult(
            false, [], "Secure document storage and malware scanning are not configured."));
}