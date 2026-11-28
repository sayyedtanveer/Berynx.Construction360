namespace Berynx.Domain.Gst;

public static class M13Rules
{
    public static DateOnly MonthEnd(DateOnly monthStart)
    {
        if (monthStart.Day != 1) throw new ArgumentException("GST period must start on the first day of a month.");
        return monthStart.AddMonths(1).AddDays(-1);
    }

    public static void RequireReadyToFile(int blockingErrors)
    {
        if (blockingErrors > 0)
            throw new InvalidOperationException("GST filing is blocked while export errors remain.");
    }

    public static int ReadinessPercent(int totalChecks, int passedChecks) =>
        totalChecks <= 0 ? 100 : (int)Math.Round(Math.Clamp(passedChecks, 0, totalChecks) * 100m / totalChecks,
            MidpointRounding.AwayFromZero);
}
