using System.Text;
using Caprine.FilePath;
using Squash.Core.Services;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Squash.Core.Tests;

[TestClass]
public sealed class DownloadServiceTests
{
    [TestMethod]
    public async Task MatchingSha256IsAccepted()
    {
        var path = Path.Combine(Path.GetTempPath(), $"squash-checksum-{Guid.NewGuid():N}.tmp");
        var bytes = Encoding.UTF8.GetBytes("verified FFmpeg archive");

        try
        {
            await File.WriteAllBytesAsync(path, bytes);

            var expectedHash = Convert.ToHexString(SHA256.HashData(bytes));

            await DownloadService.VerifySha256Async(FilePath.From(path), expectedHash);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task MismatchedSha256IsRejected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"squash-checksum-{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(path, "unexpected archive");

            await Assert.ThrowsAsync<InvalidDataException>(
                () => DownloadService.VerifySha256Async(FilePath.From(path), new string('0', 64)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
