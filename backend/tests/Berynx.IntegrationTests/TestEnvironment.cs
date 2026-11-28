namespace Berynx.IntegrationTests;

internal sealed class TestEnvironment : IDisposable
{
    private readonly Dictionary<string, string?> previous = [];
    private string? dataProtectionKeyDirectory;

    public TestEnvironment(IReadOnlyDictionary<string, string?> values)
    {
        var effectiveValues = values.ToDictionary(item => item.Key, item => item.Value);
        dataProtectionKeyDirectory = Path.Combine(
            AppContext.BaseDirectory,
            $"test-data-protection-{Guid.NewGuid():N}");
        effectiveValues["DataProtection__KeyDirectory"] = dataProtectionKeyDirectory;

        foreach (var (name, value) in effectiveValues)
        {
            previous[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    public void Dispose()
    {
        foreach (var (name, value) in previous)
            Environment.SetEnvironmentVariable(name, value);

        if (dataProtectionKeyDirectory is not null && Directory.Exists(dataProtectionKeyDirectory))
            Directory.Delete(dataProtectionKeyDirectory, recursive: true);
    }
}
