namespace Berynx.Application.Invoicing;

public sealed record M10CreateDraftRequest(
    Guid[] ChallanIds, DateOnly InvoiceDate, string? Notes, string IdempotencyKey,
    string? BillingModeOverride = null, string? BillingModeOverrideReason = null);

public sealed record M10InvoiceActionRequest(long Version, string Reason);
