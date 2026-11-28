namespace Berynx.Application.Quality;

public sealed record M19CreateSampleRequest(Guid IdempotencyKey, Guid TripId, string MaterialType, string Grade, DateTimeOffset CastAt, int CubeCount, Guid? OwnerUserId);
public sealed record M19SaveCriteriaRequest(string MaterialType, string Grade, int TestAgeDays, decimal MinimumStrengthMpa, string StandardReference, DateOnly EffectiveFrom, DateOnly? EffectiveUntil, string ChangeReason);
public sealed record M19RecordTestRequest(decimal StrengthMpa, DateTimeOffset TestedAt, Guid? ReportDocumentId, string Reason, long ExpectedVersion);
public sealed record M19CreateDisputeRequest(Guid IdempotencyKey, string Type, string Description, Guid? CustomerPartyId, Guid? SupplierPartyId, Guid? SiteId, Guid? SalesOrderId, Guid? TripId, Guid? ChallanId, Guid? InvoiceId, Guid? SampleId, Guid? OwnerUserId);
public sealed record M19ChangeDisputeRequest(string Status, string Resolution, Guid? CreditNoteId, string Reason, long ExpectedVersion);