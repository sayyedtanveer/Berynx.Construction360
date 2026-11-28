namespace Berynx.Application.Reports;

public sealed record M16ReportResult(
    string ReportCode, DateOnly From, DateOnly To, DateTimeOffset LastRefreshedAt,
    IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    IReadOnlyDictionary<string, object?> Totals);
