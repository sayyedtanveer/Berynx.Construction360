namespace Berynx.IntegrationTests;

internal sealed class TestEnvironment : IDisposable
{
    private readonly Dictionary<string, string?> previous = [];

    public TestEnvironment(IReadOnlyDictionary<string, string?> values)
    {
        foreach (var (name, value) in values)
        {
            previous[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    public void Dispose()
    {
        foreach (var (name, value) in previous)
            Environment.SetEnvironmentVariable(name, value);
    }
}
