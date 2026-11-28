namespace Berynx.Infrastructure.Documents;

public sealed class M18DocumentOptions
{
    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
}

public sealed class M18StorageOptions
{
    public string? RootPath { get; set; }
}

public sealed class ClamAvOptions
{
    public string Provider { get; set; } = "ClamAV";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3310;
    public int TimeoutSeconds { get; set; } = 45;
}