using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Gst;

public sealed class M13GstPeriod : ITenantOwned
{
    public Guid TenantId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = "Open";
    public DateTimeOffset? FiledAt { get; set; }
    public Guid? FiledByUserId { get; set; }
    public string ExternalAcknowledgement { get; set; } = "";
    public string FilingSnapshotJson { get; set; } = "{}";
}

public sealed class M13ExceptionReview : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ReviewId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public Guid InvoiceId { get; set; }
    public string ExceptionCode { get; set; } = "";
    public string ReviewNote { get; set; } = "";
    public Guid ReviewedByUserId { get; set; }
    public DateTimeOffset ReviewedAt { get; set; }
}
