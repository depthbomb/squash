using Squash.Lib;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace Squash.Services;

public class EncodeService(BinaryLocatorService binaryLocatorService)
{
    public record EncodeResult(
        bool     Success,
        FilePath FilePath,
        long     FileSizeBytes,
        long     TargetSizeBytes,
        int      Iteration,
        double   VideoBitrateKbps,
        double   ElapsedSeconds
    );
    
    public record ProgressUpdate(int ProgressPercent, string TitleText);
    
    public delegate void ProgressListener(ProgressUpdate update);
    
    private record VideoInfo(double DurationSeconds, double? VideoBitrateKbps);

    private record Sample(double BitrateKbps, long FileSize);

    private const long   BytesPerMegabyte    = 1024L * 1024L;
    private const int    MinVideoBitrate     = 100;
    private const int    MinAudioBitrate     = 32;
    private const int    DefaultAudioBitrate = 128;
    private const double ContainerOverhead   = 0.97;

    public async Task<EncodeResult> ResizeVideoToTargetAsync(FilePath          inputFile,
                                                             FilePath          outputFile,
                                                             int               targetSizeMb,
                                                             double            tolerancePercent,
                                                             int               maxIterations,
                                                             int               qualityPreset,
                                                             ProgressListener  progressListener,
                                                             CancellationToken ct = default)
    {
        if (!inputFile.Exists())
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

        var startedAt        = Stopwatch.GetTimestamp();
        var ffprobePath      = await RequireBinaryPathAsync("ffprobe", "FFprobe was not found.");
        var ffmpegPath       = await RequireBinaryPathAsync("ffmpeg",  "FFmpeg was not found.");
        var targetSizeBytes  = targetSizeMb * BytesPerMegabyte;
        var toleranceBytes   = targetSizeBytes      * (tolerancePercent / 100.0);
        var currentVideoSize = inputFile.FileInfo().Length;
        if (currentVideoSize <= targetSizeBytes)
        {
            return new EncodeResult(
                Success: true,
                FilePath: inputFile,
                FileSizeBytes: currentVideoSize,
                TargetSizeBytes: targetSizeBytes,
                Iteration: 0,
                VideoBitrateKbps: 0.0,
                ElapsedSeconds: ElapsedSecondsSince(startedAt));
        }

        var (duration, videoBitrateKbps) = await GetVideoInfoAsync(ffprobePath, inputFile, ct);
        if (duration <= 0.0)
        {
            throw new InvalidOperationException("Input video duration is invalid or unavailable.");
        }

        var sourceBitrate = videoBitrateKbps ?? (currentVideoSize * 8.0) / duration / 1_000.0;

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
        var     tempOutput         = FilePath.TempFile();

        // Ensure the temp file has an .mp4 extension for ffmpeg.
        var tempOutputMp4 = tempOutput.WithSuffix(".mp4");
        tempOutput.Rename(tempOutputMp4);
        tempOutput = tempOutputMp4;

        try
        {
            int iteration = 0;

            while (iteration < maxIterations)
            {
                ct.ThrowIfCancellationRequested();
                iteration++;

                progressListener(new ProgressUpdate(
                    0,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Iteration {0}/{1} at {2:F0} kbps",
                        iteration, maxIterations, currentBitrate)));

                await EncodeVideoAsync(
                    ffmpegPath: ffmpegPath,
                    inputFile: inputFile,
                    outputFile: tempOutput,
                    videoBitrate: currentBitrate,
                    audioBitrate: audioBitrate,
                    qualityPreset: qualityPreset,
                    duration: duration,
                    iteration: iteration,
                    maxIterations: maxIterations,
                    progressListener: progressListener,
                    cancellationToken: ct
                );

                long newFileSize = tempOutput.FileInfo().Length;
                lastEncodedSize    = newFileSize;
                lastEncodedBitrate = currentBitrate;

                if (newFileSize < targetSizeBytes)
                {
                    if (bestUnder == null || newFileSize > bestUnder.FileSize)
                        bestUnder = new Sample(currentBitrate, newFileSize);

                    double gapToTarget = targetSizeBytes - newFileSize;
                    if (gapToTarget < toleranceBytes)
                    {
                        tempOutput.Rename(outputFile);
                        return new EncodeResult(
                            Success: true,
                            FilePath: outputFile,
                            FileSizeBytes: newFileSize,
                            TargetSizeBytes: targetSizeBytes,
                            Iteration: iteration,
                            VideoBitrateKbps: currentBitrate,
                            ElapsedSeconds: ElapsedSecondsSince(startedAt));
                    }

                    minBitrate = currentBitrate;
                }
                else
                {
                    if (bestOver == null || newFileSize < bestOver.FileSize)
                        bestOver = new Sample(currentBitrate, newFileSize);

                    maxBitrate = currentBitrate;
                }

                currentBitrate = EstimateNextBitrate(
                    currentBitrate: currentBitrate,
                    currentSize:    newFileSize,
                    targetSize:     targetSizeBytes,
                    minBitrate:     minBitrate,
                    maxBitrate:     maxBitrate,
                    under:          bestUnder,
                    over:           bestOver);

                if (currentBitrate < MinVideoBitrate)
                    break;
            }

            var finalSize    = lastEncodedSize    ?? tempOutput.FileInfo().Length;
            var finalBitrate = lastEncodedBitrate ?? currentBitrate;

            if (finalSize <= targetSizeBytes)
            {
                tempOutput.Rename(outputFile);
                return new EncodeResult(
                    Success: false,
                    FilePath: outputFile,
                    FileSizeBytes: finalSize,
                    TargetSizeBytes: targetSizeBytes,
                    Iteration: maxIterations,
                    VideoBitrateKbps: finalBitrate,
                    ElapsedSeconds: ElapsedSecondsSince(startedAt));
            }

            if (bestOver != null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Could not reach target after {0} iterations. Closest over-target result was {1} at {2:F0} kbps.",
                    maxIterations, FormatBytes(bestOver.FileSize), bestOver.BitrateKbps));
            }

            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "Could not reach target after {0} iterations. Final result was {1}.",
                maxIterations, FormatBytes(finalSize)));
        }
        finally
        {
            tempOutput.Unlink(true);
        }
    }

    private static async Task<VideoInfo> GetVideoInfoAsync(FilePath          ffprobePath,
                                                           FilePath          inputFile,
                                                           CancellationToken ct)
    {
        var psi = new ProcessStartInfo(ffprobePath.FullPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            StandardOutputEncoding = Encoding.UTF8,
            CreateNoWindow         = true
        };

        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-show_entries");
        psi.ArgumentList.Add("format=duration,bit_rate");
        psi.ArgumentList.Add("-of");
        psi.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
        psi.ArgumentList.Add(inputFile.FullPath);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start FFprobe.");

        var lines = new List<string>();
        while (await proc.StandardOutput.ReadLineAsync(ct) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line.Trim());
        }

        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0 || lines.Count == 0)
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

    private static async Task EncodeVideoAsync(
        FilePath          ffmpegPath,
        FilePath          inputFile,
        FilePath          outputFile,
        double            videoBitrate,
        int               audioBitrate,
        int               qualityPreset,
        double            duration,
        int               iteration,
        int               maxIterations,
        ProgressListener  progressListener,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(ffmpegPath.FullPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            StandardOutputEncoding = Encoding.UTF8,
            CreateNoWindow         = true
        };

        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-loglevel");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(inputFile.FullPath);
        psi.ArgumentList.Add("-b:v");
        psi.ArgumentList.Add(string.Format(CultureInfo.InvariantCulture, "{0:F0}k", videoBitrate));
        psi.ArgumentList.Add("-b:a");
        psi.ArgumentList.Add($"{audioBitrate}k");

        foreach (string arg in GetEncodeSettings(qualityPreset))
        {
            psi.ArgumentList.Add(arg);
        }

        psi.ArgumentList.Add("-progress");
        psi.ArgumentList.Add("pipe:1");
        psi.ArgumentList.Add("-nostats");
        psi.ArgumentList.Add(outputFile.FullPath);

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start FFmpeg.");

        var stderrTask   = proc.StandardError.ReadToEndAsync(cancellationToken);
        var progressData = new Dictionary<string, string>(StringComparer.Ordinal);

        string? lastMsgLine = null;

        while (await proc.StandardOutput.ReadLineAsync(cancellationToken) is { } rawLine)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                proc.Kill(entireProcessTree: true);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var sep = line.IndexOf('=', StringComparison.Ordinal);
            if (sep <= 0)
            {
                lastMsgLine = line;
                continue;
            }

            var key   = line[..sep];
            var value = line[(sep + 1)..];
            
            progressData[key] = value;

            if (key == "progress" && value == "continue")
            {
                var percent = ComputePercent(progressData, duration);
                var status  = BuildProgressStatus(progressData, duration);
                var title   = string.Format(
                    CultureInfo.InvariantCulture,
                    "Iteration {0}/{1}{2}",
                    iteration, maxIterations,
                    string.IsNullOrWhiteSpace(status) ? "" : $" - {status}");

                progressListener(new ProgressUpdate(percent, title));
            }
        }

        await proc.WaitForExitAsync(cancellationToken);

        if (proc.ExitCode != 0)
        {
            var stderr  = await stderrTask;
            var message = $"FFmpeg failed with exit code {proc.ExitCode}.";
            if (!string.IsNullOrWhiteSpace(lastMsgLine))
            {
                message += $" {lastMsgLine}";
            }
            else if (!string.IsNullOrWhiteSpace(stderr))
            {
                message += $" {stderr.Trim()}";
            }

            throw new InvalidOperationException(message);
        }

        progressListener(new ProgressUpdate(
            100,
            string.Format(CultureInfo.InvariantCulture, "Iteration {0}/{1} complete", iteration, maxIterations)));
    }

    private static IEnumerable<string> GetEncodeSettings(int qualityPreset)
    {
        var args = new List<string>
        {
            "-c:a", "aac",
            "-profile:v", "main",
            "-movflags", "+faststart",
            "-pix_fmt", "yuv420p",
        };

        switch (qualityPreset)
        {
            case 1: args.AddRange(["-c:v", "libx264", "-preset", "medium"]);   break;
            case 2: args.AddRange(["-c:v", "libx265", "-preset", "medium"]);   break;
            case 3: args.AddRange(["-c:v", "libx265", "-preset", "slow"]);     break;
            case 4: args.AddRange(["-c:v", "libx265", "-preset", "veryslow"]); break;
            default: throw new ArgumentException($"Unexpected quality preset: {qualityPreset}");
        }

        return args;
    }

    private static int ComputePercent(IReadOnlyDictionary<string, string> progress, double duration)
    {
        if (duration <= 0.0)
        {
            return 0;
        }

        if (!progress.TryGetValue("out_time_ms", out string? raw))
        {
            return 0;
        }

        if (long.TryParse(raw, out long outTimeMicros))
        {
            var outSeconds = outTimeMicros / 1_000_000.0;
            
            return (int)Math.Clamp(outSeconds / duration * 100.0, 0.0, 100.0);
        }

        return 0;
    }

    private static string BuildProgressStatus(IReadOnlyDictionary<string, string> progress, double duration)
    {
        progress.TryGetValue("out_time_ms", out string? outTimeMicros);
        progress.TryGetValue("speed", out string? speed);
        progress.TryGetValue("fps", out string? fps);
        progress.TryGetValue("bitrate", out string? bitrate);

        double? etaSeconds     = null;
        double? speedMultiplier = ParseSpeedMultiplier(speed);
        if (speedMultiplier is > 0 && outTimeMicros != null && long.TryParse(outTimeMicros, out long micros))
        {
            var outSeconds = micros / 1_000_000.0;
            var remaining  = Math.Max(0.0, duration - outSeconds);

            etaSeconds = remaining / speedMultiplier.Value;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(speed))
        {
            parts.Add($"Speed {speed.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(fps))
        {
            parts.Add($"FPS {fps.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(bitrate))
        {
            parts.Add($"BR {bitrate.Trim()}");
        }

        if (etaSeconds.HasValue)
        {
            parts.Add($"ETA {FormatDuration(etaSeconds.Value)}");
        }

        return string.Join(", ", parts);
    }

    private static double? ParseSpeedMultiplier(string? speed)
    {
        if (string.IsNullOrWhiteSpace(speed))
        {
            return null;
        }

        var trimmed = speed.Trim().TrimEnd('x');
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
            ? v
            : null;
    }

    private static double CalculateTargetBitrate(double duration, long targetSizeBytes, int audioBitrate)
    {
        if (duration <= 0.0)
        {
            return MinVideoBitrate;
        }

        var totalBitrate       = targetSizeBytes * 8.0         / duration / 1_000.0;
        var targetVideoBitrate = (totalBitrate - audioBitrate) * ContainerOverhead;
        
        return Math.Max(MinVideoBitrate, targetVideoBitrate);
    }

    private static int SelectAudioBitrate(double duration, long targetSizeBytes, int defaultAudioBitrate)
    {
        if (duration <= 0.0)
        {
            return defaultAudioBitrate;
        }

        var totalBitrate  = targetSizeBytes * 8.0 / duration / 1_000.0;
        var minVideoTotal = MinVideoBitrate       / ContainerOverhead;
        var maxAudio      = totalBitrate - minVideoTotal;

        if (maxAudio >= defaultAudioBitrate)
        {
            return defaultAudioBitrate;
        }

        return maxAudio <= 0.0 ? MinAudioBitrate : Math.Max(MinAudioBitrate, (int)maxAudio);
    }

    private static double EstimateNextBitrate(double currentBitrate,
                                              long   currentSize,
                                              long   targetSize,
                                              double minBitrate,
                                              double maxBitrate,
                                              Sample? under,
                                              Sample? over)
    {
        double nextBitrate;

        if (under != null && over != null)
        {
            long sizeSpan = over.FileSize - under.FileSize;
            nextBitrate = sizeSpan > 0
                ? under.BitrateKbps
                  + (targetSize       - under.FileSize)
                  * (over.BitrateKbps - under.BitrateKbps)
                  / sizeSpan
                : (minBitrate + maxBitrate) / 2.0;
        }
        else
        {
            nextBitrate = currentSize > 0
                ? currentBitrate            * ((double)targetSize / currentSize)
                : (minBitrate + maxBitrate) / 2.0;
        }

        nextBitrate = Math.Clamp(nextBitrate, minBitrate, maxBitrate);
        if (Math.Abs(nextBitrate - currentBitrate) < 1.0)
        {
            var midpoint = (minBitrate + maxBitrate) / 2.0;
            
            nextBitrate = midpoint;
        }

        return nextBitrate;
    }

    private static string FormatDuration(double totalSeconds)
    {
        var seconds   = (int)Math.Max(0.0, totalSeconds);
        var hours     = seconds        / 3600;
        var minutes   = seconds % 3600 / 60;
        var remainder = seconds        % 60;

        if (hours > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}h {1:D2}m {2:D2}s", hours, minutes, remainder);
        }
        
        return minutes > 0 ? string.Format(CultureInfo.InvariantCulture, "{0}m {1:D2}s", minutes, remainder) : $"{remainder}s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes}B";
        }

        var value = (double)bytes;
        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        int index = 0;

        while (value >= 1024.0 && index < units.Length - 1)
        {
            value /= 1024.0;
            index++;
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:F2}{1}", value, units[index]);
    }

    private async Task<string> RequireBinaryPathAsync(string name, string missingMessage)
    {
        var found = await binaryLocatorService.HasBinaryAsync(name);
        return !found ? throw new InvalidOperationException(missingMessage) :
            // Return the binary name; on PATH it resolves automatically via ProcessStartInfo.
            name;
    }

    private static double ElapsedSecondsSince(long startTimestamp) => (double)(Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency;
}