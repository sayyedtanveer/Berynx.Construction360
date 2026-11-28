namespace Berynx.Application.Gst;

public sealed record M13MarkFiledRequest(DateOnly PeriodStart, string? ExternalAcknowledgement);
public sealed record M13ExceptionReviewRequest(DateOnly PeriodStart, Guid InvoiceId, string ExceptionCode, string ReviewNote);
