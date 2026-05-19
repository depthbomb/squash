namespace Squash.Services;

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
        if (!thumbnailOutputPath.IsDir())
        {
            throw new InvalidOperationException($"{nameof(thumbnailOutputPath)} should be a directory.");
        }

        var thumbnailFileName = $"{videoFilePath.Name.CreateGuidFrom("B")}.jpg";
        var thumbnailFilePath = thumbnailOutputPath / thumbnailFileName;
        if (thumbnailFilePath.Exists())
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
        psi.ArgumentList.Add("thumbnail");
        psi.ArgumentList.Add("-frames:v");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add(thumbnailFilePath.FullPath);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {ffmpegBinary}.");

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            var error = await proc.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"FFmpeg failed: {error}");
        }

        return !thumbnailFilePath.Exists() ? throw new Exception("Could not create thumbnail.") : thumbnailFilePath;
    }
}
