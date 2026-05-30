using Caprine.FilePath;
using Squash.Core.Exceptions;
using Squash.Core.Extensions;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Squash.Core.Services;

public class ProgressEventArgs : EventArgs
{
    public int    CurrentIteration { get; }
    public int    MaxIterations    { get; }
    public int    ProgressPercent  { get; }
    public string ProgressStatus   { get; }

    public ProgressEventArgs(int currentIteration, int maxIterations, int percent, string status)
    {
        CurrentIteration = currentIteration;
        MaxIterations    = maxIterations;
        ProgressPercent  = percent;
        ProgressStatus   = status;
    }
}

public class EncodeService(BinaryLocatorService binaryLocatorService)
{
    public record EncodeResult(
        bool     Success,
        FilePath FilePath,
        long     FileSizeBytes,
        long     TargetSizeBytes,
        int      Iteration,
        double   VideoBitrateKbps,
        double   ElapsedSeconds);

    public event EventHandler?                    Started;
    public event EventHandler<ProgressEventArgs>? Progress;
    public event EventHandler<EncodeResult?>?     Finished;

    private record VideoInfo(double DurationSeconds, double? VideoBitrateKbps);

    private record Sample(double BitrateKbps, long FileSize);

    private record ProcessResult(int ExitCode, string StandardError);

    private const long   BytesPerMegabyte    = 1024L * 1024L;
    private const int    MinVideoBitrate     = 100;
    private const int    MinAudioBitrate     = 32;
    private const int    DefaultAudioBitrate = 128;
    private const double ContainerOverhead   = 0.97;

    private CancellationTokenSource? _cts;

    public async Task<EncodeResult> ResizeVideoToTargetAsync(FilePath inputFile,
                                                             FilePath outputFile,
                                                             int      targetSizeMb,
                                                             double   tolerancePercent,
                                                             int      maxIterations,
                                                             int      qualityPreset)
    {
        if (!inputFile.Exists)
            throw new ArgumentException("Input file does not exist.");

        if (targetSizeMb < 1)
            throw new ArgumentException("Target size must be greater than 0.");

        if (tolerancePercent is <= 0.0 or > 50.0)
            throw new ArgumentException("Tolerance must be between 0 and 50.");

        if (maxIterations <= 0)
            throw new ArgumentException("Max iterations must be greater than 0.");

        if (qualityPreset is < 1 or > 4)
            throw new ArgumentException("Quality preset must be between 1 and 4.");

        if (inputFile.FullPath == outputFile.FullPath)
            throw new ArgumentException("Output file cannot be the same as input file.");

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var ct = _cts.Token;

        Started?.Invoke(this, EventArgs.Empty);

        var startedAt        = Stopwatch.GetTimestamp();
        var ffprobePath      = await RequireBinaryPathAsync("ffprobe", "FFprobe was not found.").ConfigureAwait(false);
        var ffmpegPath       = await RequireBinaryPathAsync("ffmpeg", "FFmpeg was not found.").ConfigureAwait(false);
        var targetSizeBytes  = targetSizeMb    * BytesPerMegabyte;
        var toleranceBytes   = targetSizeBytes * (tolerancePercent / 100.0);
        var currentVideoSize = inputFile.FileInfo().Length;

        VideoSizeBelowTargetSizeException.ThrowIf(currentVideoSize <= targetSizeBytes, "Video file size is at or below target file size.");

        var (duration, videoBitrateKbps) = await GetVideoInfoAsync(ffprobePath, inputFile, ct).ConfigureAwait(false);
        if (duration <= 0.0)
        {
            throw new InvalidOperationException("Input video duration is invalid or unavailable.");
        }

        var sourceBitrate = videoBitrateKbps ?? currentVideoSize * 8.0 / duration / 1_000.0;

        int    audioBitrate  = SelectAudioBitrate(duration, targetSizeBytes, DefaultAudioBitrate);
        double targetBitrate = CalculateTargetBitrate(duration, targetSizeBytes, audioBitrate);
        double minBitrate    = MinVideoBitrate;
        double maxBitrate    = targetBitrate * 2;

        if (sourceBitrate > 0.0)
        {
            var sourceVideoCap = Math.Max(MinVideoBitrate, sourceBitrate - audioBitrate);
            maxBitrate = Math.Min(maxBitrate, sourceVideoCap * 1.1);
        }

        maxBitrate = Math.Max(maxBitrate, targetBitrate);

        double  currentBitrate     = targetBitrate;
        Sample? bestUnder          = null;
        Sample? bestOver           = null;
        long?   lastEncodedSize    = null;
        double? lastEncodedBitrate = null;

        var tempOutput = FilePath.TempFile().WithSuffix(".mp4");

        try
        {
            int iteration = 0;
            while (iteration < maxIterations)
            {
                _cts.Token.ThrowIfCancellationRequested();

                iteration++;

                Progress?.Invoke(this, new ProgressEventArgs(iteration, maxIterations, 0, $"Encoding at {currentBitrate:F0} kbps"));

                await EncodeVideoAsync(
                    ffmpegPath,
                    inputFile,
                    tempOutput,
                    currentBitrate,
                    audioBitrate,
                    qualityPreset,
                    duration,
                    iteration,
                    maxIterations,
                    ct
                ).ConfigureAwait(false);

                long newFileSize = tempOutput.FileInfo().Length;

                lastEncodedSize    = newFileSize;
                lastEncodedBitrate = currentBitrate;

                if (newFileSize < targetSizeBytes)
                {
                    if (bestUnder == null || newFileSize > bestUnder.FileSize)
                    {
                        bestUnder = new Sample(currentBitrate, newFileSize);
                    }

                    var gapToTarget = targetSizeBytes - newFileSize;
                    if (gapToTarget < toleranceBytes)
                    {
                        outputFile.Unlink(true);

                        tempOutput.Rename(outputFile);

                        var result = new EncodeResult(
                            Success: true,
                            FilePath: outputFile,
                            FileSizeBytes: newFileSize,
                            TargetSizeBytes: targetSizeBytes,
                            Iteration: iteration,
                            VideoBitrateKbps: currentBitrate,
                            ElapsedSeconds: ElapsedSecondsSince(startedAt));

                        Finished?.Invoke(this, result);

                        return result;
                    }

                    minBitrate = currentBitrate;
                }
                else
                {
                    if (bestOver == null || newFileSize < bestOver.FileSize)
                    {
                        bestOver = new Sample(currentBitrate, newFileSize);
                    }

                    maxBitrate = currentBitrate;
                }

                currentBitrate = EstimateNextBitrate(
                    currentBitrate: currentBitrate,
                    currentSize: newFileSize,
                    targetSize: targetSizeBytes,
                    minBitrate: minBitrate,
                    maxBitrate: maxBitrate,
                    under: bestUnder,
                    over: bestOver);

                if (currentBitrate < MinVideoBitrate)
                    break;
            }

            var finalSize    = lastEncodedSize    ?? tempOutput.FileInfo().Length;
            var finalBitrate = lastEncodedBitrate ?? currentBitrate;
            if (finalSize <= targetSizeBytes)
            {
                outputFile.Unlink(true);

                tempOutput.Rename(outputFile);

                var result = new EncodeResult(
                    Success: false,
                    FilePath: outputFile,
                    FileSizeBytes: finalSize,
                    TargetSizeBytes: targetSizeBytes,
                    Iteration: maxIterations,
                    VideoBitrateKbps: finalBitrate,
                    ElapsedSeconds: ElapsedSecondsSince(startedAt));

                Finished?.Invoke(this, result);

                return result;
            }

            UnableToReachTargetSizeException.ThrowIf(
                bestOver != null,
                $"Could not reach target after {maxIterations} iterations. Closest over-target result was {bestOver!.FileSize.ToFileSizeString()} at {bestOver.BitrateKbps:F0} kbps."
            );

            throw new UnableToReachTargetSizeException($"Could not reach target after {maxIterations} iterations. Final result was {finalSize.ToFileSizeString()}.");
        }
        finally
        {
            tempOutput.Unlink(true);
        }
    }

    public void CancelEncoding()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        Finished?.Invoke(this, null);
    }

    private static async Task<VideoInfo> GetVideoInfoAsync(string ffprobePath, FilePath inputFile, CancellationToken ct)
    {
        var args = new List<string>
        {
            "-v", "error",
            "-show_entries", "format=duration,bit_rate",
            "-of", "default=noprint_wrappers=1:nokey=1",
            inputFile.FullPath
        };
        var lines = new List<string>();
        var result = await ExecuteProcessAsync(ffprobePath, args, line =>
        {
            if (!line.IsNullOrWhiteSpace())
            {
                lines.Add(line.Trim());
            }

            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);

        if (result.ExitCode != 0 || lines.Count == 0)
        {
            throw new InvalidOperationException("FFprobe failed to read input video metadata.");
        }

        if (!double.TryParse(lines[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double duration))
        {
            throw new InvalidOperationException("FFprobe returned an invalid duration value.");
        }

        double? bitrateKbps = null;
        if (lines.Count > 1)
        {
            var raw = lines[1];
            if (!raw.Equals("N/A", StringComparison.OrdinalIgnoreCase) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double bps))
            {
                bitrateKbps = bps / 1_000.0;
            }
        }

        return new VideoInfo(duration, bitrateKbps);
    }

    private async Task EncodeVideoAsync(string            ffmpegPath,
                                        FilePath          inputFile,
                                        FilePath          outputFile,
                                        double            videoBitrate,
                                        int               audioBitrate,
                                        int               qualityPreset,
                                        double            duration,
                                        int               iteration,
                                        int               maxIterations,
                                        CancellationToken ct)
    {
        var args = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-i", inputFile.FullPath,
            "-b:v", string.Format(CultureInfo.InvariantCulture, "{0:F0}k", videoBitrate),
            "-b:a", $"{audioBitrate}k"
        };

        args.AddRange(GetEncodeSettings(qualityPreset));
        args.AddRange(["-progress", "pipe:1", "-nostats", outputFile.AsPosix()]);

        var progressData = new Dictionary<string, string>(StringComparer.Ordinal);

        string? lastMsgLine = null;

        var result = await ExecuteProcessAsync(ffmpegPath, args, line =>
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return Task.CompletedTask;
            }

            var sep = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (sep <= 0)
            {
                lastMsgLine = trimmed;
                return Task.CompletedTask;
            }

            var key   = trimmed[..sep];
            var value = trimmed[(sep + 1)..];

            progressData[key] = value;

            if (key == "progress" && value == "continue")
            {
                var percent = ComputePercent(progressData, duration);
                var status  = BuildProgressStatus(progressData, duration);
                var title   = $"{(status.IsNullOrWhiteSpace() ? "" : status)}";

                Progress?.Invoke(this, new ProgressEventArgs(iteration, maxIterations, percent, title));
            }

            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            var message = $"FFmpeg failed with exit code {result.ExitCode}.";
            if (!lastMsgLine.IsNullOrWhiteSpace())
            {
                message += $" {lastMsgLine}";
            }
            else if (!result.StandardError.IsNullOrWhiteSpace())
            {
                message += $" {result.StandardError.Trim()}";
            }

            throw new InvalidOperationException(message);
        }

        Progress?.Invoke(this, new ProgressEventArgs(iteration, maxIterations, 100, "Iteration complete"));
    }

    private static async Task<ProcessResult> ExecuteProcessAsync(string              executable,
                                                                 List<string>        arguments,
                                                                 Func<string, Task>? onStdoutLine,
                                                                 CancellationToken   ct
    )
    {
        var psi = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            StandardOutputEncoding = Encoding.UTF8,
            CreateNoWindow         = true
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {executable}.");

        var stderrTask = proc.StandardError.ReadToEndAsync(ct);

        try
        {
            if (onStdoutLine != null)
            {
                while (await proc.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
                {
                    ct.ThrowIfCancellationRequested();
                    await onStdoutLine(line).ConfigureAwait(false);
                }
            }

            await proc.WaitForExitAsync(ct).ConfigureAwait(false);

            return new ProcessResult(proc.ExitCode, await stderrTask.ConfigureAwait(false));
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
                    /*Ignored*/
                }
            }
        }
    }

    private static IEnumerable<string> GetEncodeSettings(int qualityPreset) => qualityPreset switch
    {
        1 => ["-c:v", "libx264", "-preset", "medium", "-c:a", "aac", "-profile:v", "main", "-movflags", "+faststart", "-pix_fmt", "yuv420p"],
        2 => ["-c:v", "libx265", "-preset", "medium", "-c:a", "aac", "-profile:v", "main", "-movflags", "+faststart", "-pix_fmt", "yuv420p"],
        3 => ["-c:v", "libx265", "-preset", "slow", "-c:a", "aac", "-profile:v", "main", "-movflags", "+faststart", "-pix_fmt", "yuv420p"],
        4 => ["-c:v", "libx265", "-preset", "veryslow", "-c:a", "aac", "-profile:v", "main", "-movflags", "+faststart", "-pix_fmt", "yuv420p"],
        _ => throw new ArgumentException($"Unexpected quality preset: {qualityPreset}")
    };

    private static int ComputePercent(Dictionary<string, string> progress, double duration)
    {
        if (duration <= 0.0 || !progress.TryGetValue("out_time_ms", out var raw) || !long.TryParse(raw, out var micros))
        {
            return 0;
        }

        return (int)Math.Clamp(micros / 1_000_000.0 / duration * 100.0, 0.0, 100.0);
    }

    private static string BuildProgressStatus(Dictionary<string, string> progress, double duration)
    {
        progress.TryGetValue("out_time_ms", out var outTimeMicros);
        progress.TryGetValue("speed", out var speed);
        progress.TryGetValue("fps", out var fps);
        progress.TryGetValue("bitrate", out var bitrate);

        var speedMultiplier = ParseSpeedMultiplier(speed);
        var parts           = new List<string>();
        if (!speed.IsNullOrWhiteSpace())
        {
            parts.Add($"Speed {speed.Trim()}");
        }

        if (!fps.IsNullOrWhiteSpace())
        {
            parts.Add($"FPS {fps.Trim()}");
        }

        if (!bitrate.IsNullOrWhiteSpace())
        {
            parts.Add($"BR {bitrate.Trim()}");
        }

        if (speedMultiplier is > 0 && outTimeMicros != null && long.TryParse(outTimeMicros, out var micros))
        {
            var remaining = Math.Max(0.0, duration - micros / 1_000_000.0);
            parts.Add($"ETA {FormatDuration(remaining / speedMultiplier.Value)}");
        }

        return string.Join(", ", parts);
    }

    private static double? ParseSpeedMultiplier(string? speed)
    {
        if (speed.IsNullOrWhiteSpace())
        {
            return null;
        }

        var trimmed = speed.Trim().TrimEnd('x');

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static double CalculateTargetBitrate(double duration, long targetSizeBytes, int audioBitrate)
    {
        if (duration <= 0.0)
        {
            return MinVideoBitrate;
        }

        return Math.Max(MinVideoBitrate, (targetSizeBytes * 8.0 / duration / 1_000.0 - audioBitrate) * ContainerOverhead);
    }

    private static int SelectAudioBitrate(double duration, long targetSizeBytes, int defaultAudioBitrate)
    {
        if (duration <= 0.0)
        {
            return defaultAudioBitrate;
        }

        var totalBitrate = targetSizeBytes                * 8.0 / duration / 1_000.0;
        var maxAudio     = totalBitrate - MinVideoBitrate / ContainerOverhead;

        return maxAudio >= defaultAudioBitrate ? defaultAudioBitrate : (maxAudio <= 0.0 ? MinAudioBitrate : Math.Max(MinAudioBitrate, (int)maxAudio));
    }

    private static double EstimateNextBitrate(double  currentBitrate,
                                              long    currentSize,
                                              long    targetSize,
                                              double  minBitrate,
                                              double  maxBitrate,
                                              Sample? under,
                                              Sample? over)
    {
        double nextBitrate = (under != null && over != null)
            ? over.FileSize - under.FileSize > 0
                ? under.BitrateKbps + (targetSize - under.FileSize) * (over.BitrateKbps - under.BitrateKbps) / (over.FileSize - under.FileSize)
                : (minBitrate + maxBitrate) / 2.0
            : currentSize > 0
                ? currentBitrate            * ((double)targetSize / currentSize)
                : (minBitrate + maxBitrate) / 2.0;

        nextBitrate = Math.Clamp(nextBitrate, minBitrate, maxBitrate);

        return Math.Abs(nextBitrate - currentBitrate) < 1.0 ? (minBitrate + maxBitrate) / 2.0 : nextBitrate;
    }

    private static string FormatDuration(double totalSeconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0.0, totalSeconds));

        var parts = new List<string>();

        if (t.Days > 0)
            parts.Add($"{t.Days}d");

        if (t.Hours > 0)
            parts.Add($"{t.Hours}h");

        if (t.Minutes > 0)
            parts.Add($"{t.Minutes}m");

        parts.Add($"{t.Seconds}s");

        return string.Join(" ", parts);
    }

    private async Task<string> RequireBinaryPathAsync(string name, string missingMessage)
    {
        var found = await binaryLocatorService.GetBinaryPathAsync(name).ConfigureAwait(false);
        return found?.FullPath ?? throw new InvalidOperationException(missingMessage);
    }

    private static double ElapsedSecondsSince(long startTimestamp) => (double)(Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency;
}
