using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Berynx.Application.Documents;
using Berynx.Domain.Documents;
using Berynx.Infrastructure.Documents;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Berynx.UnitTests.Documents;

[TestFixture]
public sealed class M18DocumentSecurityTests
{
    [Test]
    public void UploadRules_AllowOnlyPurposeBoundTypesAndActualFileSignatures()
    {
        Assert.That(M18DocumentRules.IsAllowedContentType(M18DocumentRules.Signature, "image/jpeg"), Is.True);
        Assert.That(M18DocumentRules.IsAllowedContentType(M18DocumentRules.Signature, "application/pdf"), Is.False);
        Assert.That(M18DocumentRules.IsAllowedContentType(M18DocumentRules.Photo, "application/pdf"), Is.True);
        Assert.That(M18DocumentRules.IsAllowedContentType(M18DocumentRules.Photo, "text/html"), Is.False);
        Assert.That(M18DocumentRules.HasMatchingFileSignature("image/jpeg", [0xFF, 0xD8, 0xFF, 0x00]), Is.True);
        Assert.That(M18DocumentRules.HasMatchingFileSignature("image/jpeg", "%PDF-1"u8), Is.False);
        Assert.That(M18DocumentRules.SafeDisplayName("../../receiver/photo.jpg"), Is.EqualTo("photo.jpg"));
        Assert.Throws<ArgumentException>(() => M18DocumentRules.ValidateUpload(M18DocumentRules.Signature, "application/pdf", 1024));
        Assert.Throws<ArgumentOutOfRangeException>(() => M18DocumentRules.ValidateUpload(M18DocumentRules.Photo, "image/png", 0));
    }

    [Test]
    public void PreviewBinding_RejectsUnscannedWrongTenantWrongBranchAndSupersededVersions()
    {
        var tenant = Guid.NewGuid();
        var trip = Guid.NewGuid();
        var branch = Guid.NewGuid();
        var document = new M18Document
        {
            TenantId = tenant, TripId = trip, BranchId = branch, Purpose = M18DocumentRules.Signature,
            IsCurrent = true, ScanStatus = M18DocumentRules.ScanClean, StorageStatus = M18DocumentRules.StorageAvailable,
            SizeBytes = 100
        };

        Assert.That(M18DocumentRules.CanBindToTrip(document, tenant, trip, branch, M18DocumentRules.Signature), Is.True);
        Assert.That(M18DocumentRules.CanBindToTrip(document, Guid.NewGuid(), trip, branch, M18DocumentRules.Signature), Is.False);
        Assert.That(M18DocumentRules.CanBindToTrip(document, tenant, trip, Guid.NewGuid(), M18DocumentRules.Signature), Is.False);
        Assert.That(M18DocumentRules.CanBindToTrip(document, tenant, Guid.NewGuid(), branch, M18DocumentRules.Signature), Is.False);
        document.ScanStatus = M18DocumentRules.ScanPending;
        Assert.That(M18DocumentRules.CanBindToTrip(document, tenant, trip, branch, M18DocumentRules.Signature), Is.False);
        document.ScanStatus = M18DocumentRules.ScanClean;
        document.IsCurrent = false;
        Assert.That(M18DocumentRules.CanBindToTrip(document, tenant, trip, branch, M18DocumentRules.Signature), Is.False);
    }

    [Test]
    public async Task FilesystemStorage_RejectsWebRootAndEnforcesMaximumBytes()
    {
        var temp = Path.Combine(Path.GetTempPath(), "m18-test-" + Guid.NewGuid().ToString("N"));
        var webRoot = Path.Combine(temp, "wwwroot");
        var privateRoot = Path.Combine(temp, "private");
        Directory.CreateDirectory(webRoot);
        try
        {
            Assert.Throws<InvalidOperationException>(() => new FileSystemM18DocumentStorage(
                Options.Create(new M18StorageOptions { RootPath = Path.Combine(webRoot, "documents") }), webRoot));

            var storage = new FileSystemM18DocumentStorage(Options.Create(new M18StorageOptions { RootPath = privateRoot }), webRoot);
            var tenant = Guid.NewGuid();
            var doc = Guid.NewGuid();
            var tooLarge = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var exception = await Assert.ThrowsAsync<M18UploadRejectedException>(async () =>
                await storage.WriteQuarantinedAsync(tenant, doc, tooLarge, 3, CancellationToken.None));
            Assert.That(exception!.StatusCode, Is.EqualTo(413));
            Assert.That(Directory.Exists(Path.Combine(privateRoot, "quarantine", tenant.ToString("N"))), Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(privateRoot, "quarantine", tenant.ToString("N"))), Is.Empty);

            var validDoc = Guid.NewGuid();
            var valid = new MemoryStream(new byte[] { 1, 2, 3 });
            Assert.That(await storage.WriteQuarantinedAsync(tenant, validDoc, valid, 3, CancellationToken.None), Is.EqualTo(3));
            await using var stored = await storage.OpenQuarantinedReadAsync(tenant, validDoc, CancellationToken.None);
            Assert.That(stored.Length, Is.EqualTo(3));
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [TestCase("stream: OK")]
    public async Task ClamAvAdapter_AcceptsOnlyCleanDaemonVerdict(string response)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = RespondToClamRequestAsync(listener, response);
        var scanner = CreateScanner(port);

        var verdict = await scanner.ScanAsync(new MemoryStream(Encoding.UTF8.GetBytes("small test payload")), CancellationToken.None);

        Assert.That(verdict, Is.EqualTo(M18ScanVerdict.Clean));
        await server;
    }

    [Test]
    public async Task ClamAvAdapter_RejectsMalwareVerdict()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = RespondToClamRequestAsync(listener, "stream: Eicar-Test-Signature FOUND");
        var scanner = CreateScanner(port);

        var verdict = await scanner.ScanAsync(new MemoryStream(Encoding.UTF8.GetBytes("test")), CancellationToken.None);

        Assert.That(verdict, Is.EqualTo(M18ScanVerdict.Infected));
        await server;
    }

    [Test]
    public void ClamAvAdapter_FailsClosedWhenDaemonIsUnavailable()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var scanner = CreateScanner(port);

        Assert.ThrowsAsync<M18ScannerUnavailableException>(async () =>
            await scanner.ScanAsync(new MemoryStream([1, 2, 3]), CancellationToken.None));
    }

    private static ClamAvMalwareScanner CreateScanner(int port) => new(Options.Create(new ClamAvOptions
    {
        Provider = "ClamAV", Host = "127.0.0.1", Port = port, TimeoutSeconds = 5
    }));

    private static async Task RespondToClamRequestAsync(TcpListener listener, string response)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        var command = new List<byte>();
        var one = new byte[1];
        while (await stream.ReadAsync(one) == 1 && one[0] != 0) command.Add(one[0]);
        Assert.That(Encoding.ASCII.GetString(command.ToArray()), Is.EqualTo("zINSTREAM"));

        var prefix = new byte[sizeof(int)];
        while (true)
        {
            await ReadExactlyAsync(stream, prefix);
            var count = BinaryPrimitives.ReadInt32BigEndian(prefix);
            if (count == 0) break;
            var payload = new byte[count];
            await ReadExactlyAsync(stream, payload);
        }
        await stream.WriteAsync(Encoding.ASCII.GetBytes(response + "\0"));
        await stream.FlushAsync();
    }

    private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[offset..]);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
        }
    }
}