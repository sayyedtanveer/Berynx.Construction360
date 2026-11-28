namespace Berynx.Domain.Quality;

public static class M19Rules
{
    public const string ReviewRequired = "ReviewRequired";
    public const string Scheduled = "Scheduled";
    public const string Pass = "Pass";
    public const string Fail = "Fail";

    public static DateTimeOffset DueAt(DateTimeOffset castAt, int testAgeDays)
    {
        if (testAgeDays is < 1 or > 365) throw new ArgumentOutOfRangeException(nameof(testAgeDays));
        return castAt.AddDays(testAgeDays);
    }

    public static string Evaluate(decimal strengthMpa, decimal? thresholdMpa) =>
        thresholdMpa is > 0 ? strengthMpa >= thresholdMpa.Value ? Pass : Fail : ReviewRequired;

    public static string NormalizeMaterialType(string? materialType)
    {
        var value = materialType?.Trim();
        if (value is not ("Concrete" or "TMT" or "Cement" or "Other"))
            throw new ArgumentException("Choose Concrete, TMT, Cement or Other as the material type.", nameof(materialType));
        return value;
    }

    public static string NormalizeGrade(string? grade)
    {
        var value = grade?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80)
            throw new ArgumentException("A product grade is required (maximum 80 characters).", nameof(grade));
        return value;
    }

    public static void ValidateCriteria(string materialType, string grade, int ageDays, decimal threshold, string reference, string reason)
    {
        _ = NormalizeMaterialType(materialType);
        _ = NormalizeGrade(grade);
        if (ageDays is < 1 or > 365 || threshold <= 0 || threshold > 1000)
            throw new ArgumentException("Test age and acceptance threshold must be positive, valid values.");
        if (string.IsNullOrWhiteSpace(reference) || reference.Trim().Length > 240)
            throw new ArgumentException("A standard or project acceptance reference is required.", nameof(reference));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("Explain the acceptance-criteria change.", nameof(reason));
    }

    public static string RecomputeSampleStatus(IEnumerable<M19QualityTest> tests)
    {
        var rows = tests.ToArray();
        if (rows.Length == 0 || rows.Any(x => x.Status == ReviewRequired || x.Outcome == ReviewRequired))
            return ReviewRequired;
        if (rows.Any(x => x.Outcome == Fail)) return "Failed";
        if (rows.All(x => x.Outcome == Pass)) return "Passed";
        return Scheduled;
    }

    public static bool IsActiveDispute(string status) => status is "Open" or "Investigating" or "Escalated";
}