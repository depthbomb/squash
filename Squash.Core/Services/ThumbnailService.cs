using Caprine.FilePath;
using System.Diagnostics;
using Squash.Core.Extensions;

namespace Squash.Core.Services;

public class ThumbnailService
{
    private readonly BinaryLocatorService _binaryLocator;

    public ThumbnailService(BinaryLocatorService binaryLocator)
    {
        _binaryLocator = binaryLocator;
    }

    public async Task<FilePath> GetVideoThumbnailAsync(FilePath          videoFilePath,
                                                       FilePath          thumbnailOutputPath,
                                                       CancellationToken ct = default)
    {
        if (!thumbnailOutputPath.IsDirectory)
        {
            throw new InvalidOperationException($"{nameof(thumbnailOutputPath)} should be a directory.");
        }

        var videoInfo = videoFilePath.FileInfo();
        var cacheIdentity = $"{videoFilePath.FullPath}|{videoInfo.Length}|{videoInfo.LastWriteTimeUtc.Ticks}";
        var thumbnailFileName = $"{cacheIdentity.CreateGuidFrom("B")}.jpg";
        var thumbnailFilePath = thumbnailOutputPath / thumbnailFileName;
        if (thumbnailFilePath.Exists)
        {
            return thumbnailFilePath;
        }

        var ffmpegBinary = await _binaryLocator.GetBinaryPathAsync("ffmpeg").ConfigureAwait(false);
        if (ffmpegBinary is null)
        {
            throw new InvalidOperationException("Unable to find FFmpeg binary on system.");
        }

        var psi = new ProcessStartInfo(ffmpegBinary.FullPath)
        {
            RedirectStandardError = true,
            UseShellExecute       = false,
            CreateNoWindow        = true
        };
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(videoFilePath.FullPath);
        psi.ArgumentList.Add("-vf");
        psi.ArgumentList.Add("thumbnail,scale=1280:720:force_original_aspect_ratio=increase,crop=1280:720");
        psi.ArgumentList.Add("-frames:v");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add(thumbnailFilePath.FullPath);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {ffmpegBinary}.");
        var stderrTask = proc.StandardError.ReadToEndAsync();

        try
        {
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);

            var error = await stderrTask.ConfigureAwait(false);
            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException($"FFmpeg failed: {error}");
            }

            if (!thumbnailFilePath.Exists)
            {
                throw new InvalidOperationException("Could not create thumbnail.");
            }

            return thumbnailFilePath;
        }
        catch
        {
            thumbnailFilePath.Unlink(true);
            throw;
        }
        finally
        {
            if (!proc.HasExited)
            {
                try
                {
                    proc.Kill(entireProcessTree: true);
                    await proc.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Best effort: the original failure or cancellation is more useful.
                }
            }

            try
            {
                await stderrTask.ConfigureAwait(false);
            }
            catch
            {
                // Best effort: the original failure or cancellation is more useful.
            }
        }
    }
}
