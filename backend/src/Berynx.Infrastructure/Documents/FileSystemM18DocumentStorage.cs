using System.Security.AccessControl;
using System.Security.Principal;
using Berynx.Application.Documents;
using Microsoft.Extensions.Options;

namespace Berynx.Infrastructure.Documents;

/// <summary>Private, non-web-root filesystem object store for local development and single-node deployments.</summary>
public sealed class FileSystemM18DocumentStorage : IM18DocumentStorage
{
    private readonly string root;
    private readonly StringComparison pathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public FileSystemM18DocumentStorage(IOptions<M18StorageOptions> options, string? webRootPath)
    {
        var configured = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(local))
                throw new InvalidOperationException("Configure M18:Storage:RootPath to a private directory outside the web root.");
            configured = Path.Combine(local, "BerynxConstruction360", "private-documents");
        }

        root = Path.GetFullPath(configured);
        var webRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(webRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "wwwroot") : webRootPath);
        if (IsSameOrChild(root, webRoot))
            throw new InvalidOperationException("M18 storage must be outside the web root and must never be served as static content.");

        EnsurePrivateDirectory(root);
        foreach (var area in new[] { "quarantine", "objects" })
            EnsurePrivateDirectory(Path.Combine(root, area));
    }

    public async Task<long> WriteQuarantinedAsync(
        Guid tenantId, Guid documentId, Stream source, long maximumBytes, CancellationToken cancellationToken)
    {
        var path = QuarantinePath(tenantId, documentId);
        EnsurePrivateDirectory(Path.GetDirectoryName(path)!);
        long written = 0;
        try
        {
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough);
            RestrictPermissions(path, directory: false);
            var buffer = new byte[64 * 1024];
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken);
                if (read == 0) break;
                written = checked(written + read);
                if (written > maximumBytes)
                    throw new M18UploadRejectedException("The file exceeds the configured tenant upload limit.", 413);
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            await target.FlushAsync(cancellationToken);
            return written;
        }
        catch
        {
            TryDelete(path);
            throw;
        }
    }

    public Task<Stream> OpenQuarantinedReadAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(OpenPrivateRead(QuarantinePath(tenantId, documentId)));

    public async Task WriteCleanAsync(Guid tenantId, Guid documentId, ReadOnlyMemory<byte> protectedContent, CancellationToken cancellationToken)
    {
        var path = ObjectPath(tenantId, documentId);
        EnsurePrivateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                64 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                RestrictPermissions(temporary, directory: false);
                await stream.WriteAsync(protectedContent, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporary, path, overwrite: false);
        }
        finally
        {
            TryDelete(temporary);
        }
    }

    public Task<Stream> OpenCleanReadAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(OpenPrivateRead(ObjectPath(tenantId, documentId)));

    public Task DeleteQuarantinedAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken)
    {
        TryDelete(QuarantinePath(tenantId, documentId));
        return Task.CompletedTask;
    }

    public Task DeleteCleanAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken)
    {
        TryDelete(ObjectPath(tenantId, documentId));
        return Task.CompletedTask;
    }

    private FileStream OpenPrivateRead(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("M18 storage objects must not be symbolic links or reparse points.");
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private string QuarantinePath(Guid tenantId, Guid documentId) =>
        Path.Combine(root, "quarantine", tenantId.ToString("N"), documentId.ToString("N") + ".bin");

    private string ObjectPath(Guid tenantId, Guid documentId) =>
        Path.Combine(root, "objects", tenantId.ToString("N"), documentId.ToString("N") + ".bin");

    private bool IsSameOrChild(string candidate, string parent)
    {
        var relative = Path.GetRelativePath(parent, candidate);
        return relative == "." || (!Path.IsPathRooted(relative) &&
            !relative.Equals("..", pathComparison) &&
            !relative.StartsWith(".." + Path.DirectorySeparatorChar, pathComparison) &&
            !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, pathComparison));
    }

    private static void EnsurePrivateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("M18 storage directories must not be symbolic links or reparse points.");
        RestrictPermissions(path, directory: true);
    }

    private static void RestrictPermissions(string path, bool directory)
    {
        if (OperatingSystem.IsWindows())
        {
            var userSid = WindowsIdentity.GetCurrent().User
                ?? throw new InvalidOperationException("The current Windows identity has no security identifier.");
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            if (directory)
            {
                var security = new DirectorySecurity();
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new FileSystemAccessRule(userSid, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(systemSid, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                new DirectoryInfo(path).SetAccessControl(security);
            }
            else
            {
                var security = new FileSecurity();
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new FileSystemAccessRule(userSid, FileSystemRights.FullControl, AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(systemSid, FileSystemRights.FullControl, AccessControlType.Allow));
                new FileInfo(path).SetAccessControl(security);
            }
            return;
        }

        File.SetUnixFileMode(path, directory
            ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            : UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}