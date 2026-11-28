using Berynx.Domain.Tenancy;

namespace Berynx.Domain.Reports;

public sealed class M16ExportLog : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid ExportId { get; set; }
    public Guid UserId { get; set; }
    public string ReportCode { get; set; } = "";
    public string FiltersJson { get; set; } = "{}";
    public int RowCount { get; set; }
    public DateTimeOffset ExportedAt { get; set; }
}
