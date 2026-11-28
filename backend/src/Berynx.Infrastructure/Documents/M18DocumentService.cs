using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Berynx.Application.Documents;
using Berynx.Application.Tenancy;
using Berynx.Domain.Delivery;
using Berynx.Domain.Documents;
using Berynx.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Berynx.Infrastructure.Documents;

public sealed class M18DocumentService(
    BerynxDbContext db,
    ICurrentTenantContext tenant,
    OnboardingStore onboarding,
    IM18DocumentStorage storage,
    IM18MalwareScanner scanner,
    IDataProtectionProvider protection,
    IOptions<M18DocumentOptions> options) : IM18DocumentService
{
    private readonly M18DocumentOptions settings = options.Value;
    public M18DocumentUploadPolicy UploadPolicy => new(
        settings.MaxFileBytes is > 0 ? Math.Min(settings.MaxFileBytes, 10 * 1024 * 1024) : 0,
        ["image/jpeg", "image/png", "image/webp", "application/pdf"]);

    public async Task<M18DocumentOperationResult> UploadTripEvidenceAsync(
        M08Trip trip, Guid actorUserId, string purpose, string? fileName, string? contentType,
        Stream content, CancellationToken cancellationToken)
    {
        EnsureTripAccess(trip, actorUserId);
        if (trip.Status is not ("Unloading" or "Completed") || trip.EvidenceState == "Verified")
            throw new M18UploadRejectedException("Evidence can only be added to an active delivery or an unverified completed delivery.", 409);
        contentType = NormalizeContentType(contentType);
        var policy = UploadPolicy;
        M18DocumentRules.ValidateUpload(purpose, contentType, policy.MaxFileBytes);
        var setup = Berynx.Infrastructure.Persistence.OnboardingStore.Decode(await onboarding.Current());
        var used = await onboarding.Storage(setup) + await db.Set<M18Document>().SumAsync(x => (long?)x.SizeBytes, cancellationToken) ?? 0L;
        var remaining = setup.Limits.Storage - used;
        var maximum = Math.Min(policy.MaxFileBytes, remaining);
        if (maximum < 1)
            throw new M18UploadRejectedException("The tenant storage limit has been reached; no evidence can be uploaded.", 413);
        var documentId = Guid.NewGuid();
        long size;
        try
        {
            size = await storage.WriteQuarantinedAsync(tenant.TenantId, documentId, content, maximum, cancellationToken);
            if (size == 0)
                throw new M18UploadRejectedException("Evidence files cannot be empty.", 400);
            await ValidateQuarantinedSignatureAsync(documentId, contentType!, cancellationToken);
        }
        catch
        {
            await storage.DeleteQuarantinedAsync(tenant.TenantId, documentId, CancellationToken.None);
            throw;
        }

        var now = DateTimeOffset.UtcNow;
        M18Document document;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var versions = await db.Set<M18Document>().Where(x => x.TripId == trip.TripId && x.Purpose == purpose)
                .OrderByDescending(x => x.VersionNumber).ToListAsync(cancellationToken);
            var transactionStorage = await onboarding.Storage(setup) +
                (await db.Set<M18Document>().SumAsync(x => (long?)x.SizeBytes, cancellationToken) ?? 0L);
            if (transactionStorage + size > setup.Limits.Storage)
                throw new M18UploadRejectedException("The tenant storage limit was reached by a concurrent upload.", 413);
            var monthlyDocuments = await db.Set<M18Document>().LongCountAsync(
                x => x.UploadedAt >= new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero), cancellationToken);
            if (monthlyDocuments >= setup.Limits.Documents)
                throw new M18UploadRejectedException("The tenant's monthly document allowance has been reached.", 413);
            document = new M18Document
            {
                TenantId = tenant.TenantId,
                DocumentId = documentId,
                BranchId = trip.BranchId,
                TripId = trip.TripId,
                UploadedByUserId = actorUserId,
                PreviousDocumentId = versions.FirstOrDefault()?.DocumentId,
                Purpose = purpose,
                VersionNumber = versions.Count == 0 ? 1 : checked(versions[0].VersionNumber + 1),
                OriginalFileName = M18DocumentRules.SafeDisplayName(fileName),
                ContentType = contentType!,
                SizeBytes = size,
                StorageKey = documentId.ToString("N"),
                ScanStatus = M18DocumentRules.ScanPending,
                StorageStatus = M18DocumentRules.StorageQuarantined,
                IsCurrent = false,
                UploadedAt = now
            };
            db.Add(document);
            AddEvent(document, actorUserId, "Uploaded", new { document.VersionNumber, document.SizeBytes, document.ContentType });
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteQuarantinedAsync(tenant.TenantId, documentId, CancellationToken.None);
            throw;
        }

        return await ScanAndPromoteAsync(document, actorUserId, cancellationToken);
    }

    public async Task<IReadOnlyList<M18DocumentOperationResult>> GetPendingScansAsync(
        Guid tripId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tripId, cancellationToken)
            ?? throw new KeyNotFoundException("Delivery trip was not found.");
        EnsureTripAccess(trip, actorUserId);
        return await db.Set<M18Document>().AsNoTracking()
            .Where(x => x.TripId == tripId && x.ScanStatus == M18DocumentRules.ScanPending &&
                x.StorageStatus == M18DocumentRules.StorageQuarantined)
            .OrderBy(x => x.UploadedAt)
            .Select(x => new M18DocumentOperationResult(x.DocumentId, x.Purpose, x.VersionNumber,
                x.SizeBytes, x.ScanStatus, x.StorageStatus, "Evidence is quarantined until the malware scanner confirms it is clean.", false))
            .ToListAsync(cancellationToken);
    }
    public async Task<M18DocumentOperationResult> RetryScanAsync(
        Guid tripId, Guid documentId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var document = await db.Set<M18Document>().SingleOrDefaultAsync(
            x => x.TripId == tripId && x.DocumentId == documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Evidence document was not found.");
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tripId, cancellationToken)
            ?? throw new KeyNotFoundException("Delivery trip was not found.");
        EnsureTripAccess(trip, actorUserId);
        if (document.ScanStatus != M18DocumentRules.ScanPending || document.StorageStatus != M18DocumentRules.StorageQuarantined)
            throw new M18UploadRejectedException("Only quarantined evidence awaiting scan can be retried.", 409);
        return await ScanAndPromoteAsync(document, actorUserId, cancellationToken);
    }

    public async Task<M18DocumentContent?> OpenForReviewAsync(
        Guid tripId, Guid documentId, Guid actorUserId, bool download, CancellationToken cancellationToken)
    {
        var document = await db.Set<M18Document>().SingleOrDefaultAsync(
            x => x.TripId == tripId && x.DocumentId == documentId, cancellationToken);
        if (document is null || !M18DocumentRules.CanPreview(document)) return null;
        var trip = await db.Set<M08Trip>().SingleOrDefaultAsync(x => x.TripId == tripId, cancellationToken);
        if (trip is null || trip.BranchId != document.BranchId) return null;
        EnsureTripAccess(trip, actorUserId);
        if (!CurrentEvidenceForTrip(trip, document)) return null;

        try
        {
            await using var encrypted = await storage.OpenCleanReadAsync(tenant.TenantId, document.DocumentId, cancellationToken);
            await using var buffer = new MemoryStream();
            await encrypted.CopyToAsync(buffer, cancellationToken);
            var protector = CreateProtector(document.TenantId, document.DocumentId);
            var plain = protector.Unprotect(buffer.ToArray());
            AddEvent(document, actorUserId, download ? "Downloaded" : "Previewed", new { document.VersionNumber });
            await db.SaveChangesAsync(cancellationToken);
            return new M18DocumentContent(new MemoryStream(plain, writable: false), document.ContentType,
                document.OriginalFileName, document.SizeBytes);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Stored evidence could not be decrypted; access was denied and an operator must investigate storage integrity.", ex);
        }
    }

    public async Task RecordReviewAsync(Guid tripId, Guid actorUserId, string decision, string reason,
        IEnumerable<Guid> documentIds, CancellationToken cancellationToken)
    {
        var ids = documentIds.Distinct().ToArray();
        var documents = await db.Set<M18Document>()
            .Where(x => x.TripId == tripId && ids.Contains(x.DocumentId))
            .ToListAsync(cancellationToken);
        foreach (var document in documents)
            AddEvent(document, actorUserId, decision == "Verified" ? "SupervisorVerified" : "SupervisorRejected",
                new { decision, reason = reason.Trim(), document.VersionNumber });
    }
    private async Task<M18DocumentOperationResult> ScanAndPromoteAsync(
        M18Document document, Guid actorUserId, CancellationToken cancellationToken)
    {
        M18ScanVerdict verdict;
        try
        {
            await using var quarantine = await storage.OpenQuarantinedReadAsync(document.TenantId, document.DocumentId, cancellationToken);
            verdict = await scanner.ScanAsync(quarantine, cancellationToken);
        }
        catch (M18ScannerUnavailableException ex)
        {
            document.ScanStatus = M18DocumentRules.ScanPending;
            document.StorageStatus = M18DocumentRules.StorageQuarantined;
            db.Add(new M18DocumentEvent
            {
                TenantId = document.TenantId, EventId = Guid.NewGuid(), DocumentId = document.DocumentId,
                TripId = document.TripId, ActorUserId = actorUserId, EventType = "ScanUnavailable",
                DetailJson = JsonSerializer.Serialize(new { detail = ex.Message }), OccurredAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            return Result(document, ex.Message);
        }

        if (verdict == M18ScanVerdict.Infected)
        {
            document.ScanStatus = M18DocumentRules.ScanRejected;
            document.StorageStatus = M18DocumentRules.StorageDeleted;
            document.ScannedAt = DateTimeOffset.UtcNow;
            document.Version++;
            AddEvent(document, actorUserId, "ScanRejected", new { reason = "Malware detected" });
            await db.SaveChangesAsync(cancellationToken);
            await storage.DeleteQuarantinedAsync(document.TenantId, document.DocumentId, CancellationToken.None);
            return Result(document, "Malware was detected. The quarantined upload was rejected.");
        }

        byte[] protectedContent;
        await using (var quarantine = await storage.OpenQuarantinedReadAsync(document.TenantId, document.DocumentId, cancellationToken))
        {
            await using var plain = new MemoryStream();
            await quarantine.CopyToAsync(plain, cancellationToken);
            if (plain.Length != document.SizeBytes)
                throw new InvalidOperationException("Quarantined evidence size changed after upload; file promotion was refused.");
            protectedContent = CreateProtector(document.TenantId, document.DocumentId).Protect(plain.ToArray());
        }

        await storage.WriteCleanAsync(document.TenantId, document.DocumentId, protectedContent, cancellationToken);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var currentDocument = await db.Set<M18Document>().SingleAsync(x => x.DocumentId == document.DocumentId, cancellationToken);
            var trip = await db.Set<M08Trip>().SingleAsync(x => x.TripId == document.TripId, cancellationToken);
            EnsureTripAccess(trip, actorUserId);
            if (trip.EvidenceState == "Verified")
                throw new M18UploadRejectedException("Verified evidence is immutable; create an authorized correction workflow instead.", 409);
            var latestVersion = await db.Set<M18Document>()
                .Where(x => x.TripId == trip.TripId && x.Purpose == currentDocument.Purpose)
                .MaxAsync(x => (int?)x.VersionNumber, cancellationToken);
            var isLatestVersion = currentDocument.VersionNumber == latestVersion;
            if (isLatestVersion)
            {
                foreach (var prior in await db.Set<M18Document>().Where(x => x.TripId == trip.TripId && x.Purpose == currentDocument.Purpose && x.IsCurrent).ToListAsync(cancellationToken))
                {
                    prior.IsCurrent = false;
                    prior.Version++;
                }
            }
            currentDocument.ScanStatus = M18DocumentRules.ScanClean;
            currentDocument.StorageStatus = M18DocumentRules.StorageAvailable;
            currentDocument.IsCurrent = isLatestVersion;
            currentDocument.ScannedAt = DateTimeOffset.UtcNow;
            currentDocument.Version++;
            if (isLatestVersion)
            {
                if (currentDocument.Purpose == M18DocumentRules.Signature) trip.SignatureReference = currentDocument.DocumentId.ToString("D");
                else trip.PhotoReference = currentDocument.DocumentId.ToString("D");
                if (trip.Status == "Completed" && trip.EvidenceState is "Submitted")
                {
                    // An uploaded replacement to already-submitted evidence preserves the pending review state.
                }
                trip.Version++;
            }
            AddEvent(currentDocument, actorUserId, isLatestVersion ? "ScanClean" : "ScanCleanSuperseded",
                new { currentDocument.VersionNumber, currentDocument.ScannedAt, currentDocument.IsCurrent });
            db.Add(new M08TripEvent
            {
                TenantId = trip.TenantId, EventId = Guid.NewGuid(), TripId = trip.TripId, ActorUserId = actorUserId,
                EventType = isLatestVersion ? "EvidenceUploaded" : "EvidenceUploadSuperseded", FromStatus = trip.Status, ToStatus = trip.Status,
                DetailJson = JsonSerializer.Serialize(new { currentDocument.DocumentId, currentDocument.Purpose, currentDocument.VersionNumber, currentDocument.ScanStatus, currentDocument.IsCurrent }),
                DeviceAt = DateTimeOffset.UtcNow, ReceivedAt = DateTimeOffset.UtcNow, ActionId = "m18:" + currentDocument.DocumentId.ToString("N")
            });            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteCleanAsync(document.TenantId, document.DocumentId, CancellationToken.None);
            throw;
        }
        await storage.DeleteQuarantinedAsync(document.TenantId, document.DocumentId, CancellationToken.None);
        return Result(document, null);
    }

    private async Task ValidateQuarantinedSignatureAsync(Guid documentId, string contentType, CancellationToken cancellationToken)
    {
        await using var stream = await storage.OpenQuarantinedReadAsync(tenant.TenantId, documentId, cancellationToken);
        var prefix = new byte[12];
        var count = 0;
        while (count < prefix.Length)
        {
            var read = await stream.ReadAsync(prefix.AsMemory(count), cancellationToken);
            if (read == 0) break;
            count += read;
        }
        if (!M18DocumentRules.HasMatchingFileSignature(contentType, prefix.AsSpan(0, count)))
            throw new M18UploadRejectedException("The file content does not match its declared evidence type.", 415);
    }

    private void EnsureTripAccess(M08Trip trip, Guid actorUserId)
    {
        if (actorUserId == Guid.Empty || trip.TenantId != tenant.TenantId)
            throw new UnauthorizedAccessException("The delivery trip is outside the authenticated tenant scope.");
    }

    private static string NormalizeContentType(string? contentType) =>
        (contentType ?? "").Split(';', 2)[0].Trim().ToLowerInvariant();

    private static bool CurrentEvidenceForTrip(M08Trip trip, M18Document document) =>
        string.Equals(document.Purpose == M18DocumentRules.Signature ? trip.SignatureReference : trip.PhotoReference,
            document.DocumentId.ToString("D"), StringComparison.OrdinalIgnoreCase);

    private IDataProtector CreateProtector(Guid tenantId, Guid documentId) =>
        protection.CreateProtector("Berynx.M18.Document.v1", tenantId.ToString("N"), documentId.ToString("N"));

    private void AddEvent(M18Document document, Guid actorUserId, string eventType, object detail)
    {
        db.Add(new M18DocumentEvent
        {
            TenantId = document.TenantId, EventId = Guid.NewGuid(), DocumentId = document.DocumentId,
            TripId = document.TripId, ActorUserId = actorUserId, EventType = eventType,
            DetailJson = JsonSerializer.Serialize(detail), OccurredAt = DateTimeOffset.UtcNow
        });
    }

    private static M18DocumentOperationResult Result(M18Document document, string? detail) => new(
        document.DocumentId, document.Purpose, document.VersionNumber, document.SizeBytes,
        document.ScanStatus, document.StorageStatus, detail, document.IsCurrent);
}