using Berynx.Application.Delivery;
using Berynx.Domain.Documents;
using NUnit.Framework;

namespace Berynx.UnitTests.Delivery;

[TestFixture]
public sealed class M08EvidenceRulesTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid TripId = Guid.NewGuid();
    private static readonly Guid SignatureId = Guid.NewGuid();
    private static readonly Guid PhotoId = Guid.NewGuid();

    private static M08SecureEvidenceReviewResult CleanAssets(params M08SecureEvidenceAsset[] assets) =>
        new(true, assets, null);

    private static M08SecureEvidenceAsset Asset(Guid id, Guid? tenantId = null, string purpose = "Signature",
        string type = "image/png", string scan = "Clean", string storage = "Available", Guid? tripId = null,
        bool current = true, string? previewPath = null) =>
        new(id, tenantId ?? TenantId, purpose, type, scan, storage,
            previewPath ?? $"/api/v1/m08/trips/{tripId ?? TripId:D}/evidence/{id:D}/content",
            "evidence.png", 1, current);

    [Test]
    public void Allows_only_clean_tenant_and_trip_owned_current_assets_with_authenticated_preview_path()
    {
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), null, CleanAssets(Asset(SignatureId))), Is.True);
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), null, CleanAssets(Asset(SignatureId, tenantId: Guid.NewGuid()))), Is.False);
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), null, CleanAssets(Asset(SignatureId, scan: "Pending"))), Is.False);
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), null, CleanAssets(Asset(SignatureId, current: false))), Is.False);
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), null, CleanAssets(Asset(SignatureId, tripId: Guid.NewGuid()))), Is.False);
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), null, CleanAssets(Asset(SignatureId, previewPath: "https://files.example.invalid/public"))), Is.False);
    }

    [Test]
    public void Requires_receiver_evidence_asset_ids_and_rejects_unsafe_references()
    {
        Assert.That(M08SecureEvidenceReviewRules.TryParseAssetIds(null, null, out _), Is.False);
        Assert.That(M08SecureEvidenceReviewRules.TryParseAssetIds("https://example.invalid/photo.jpg", null, out _), Is.False);
        Assert.That(M08SecureEvidenceReviewRules.TryParseAssetIds(SignatureId.ToString("D"), SignatureId.ToString("D"), out _), Is.False);
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), null, new M08SecureEvidenceReviewResult(false, [], "Not configured")), Is.False);
    }

    [Test]
    public void Requires_each_evidence_asset_to_match_its_declared_purpose()
    {
        var assets = CleanAssets(Asset(SignatureId), Asset(PhotoId, purpose: "Photo", type: "image/jpeg"));
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), PhotoId.ToString("D"), assets), Is.True);
        Assert.That(M08SecureEvidenceReviewRules.IsReviewable(TenantId, TripId,
            SignatureId.ToString("D"), PhotoId.ToString("D"),
            CleanAssets(Asset(SignatureId), Asset(PhotoId, purpose: "Signature"))), Is.False);
    }
}