using Caprine.FilePath;
using Squash.Core.Exceptions;
using Squash.Core.Extensions;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Squash.Core.Services;

public class ProgressEventArgs : EventArgs
{
    public int CurrentIteration { get; }
    public int MaxIterations { get; }
    public int ProgressPercent { get; }
    public string ProgressStatus { get; }

    public ProgressEventArgs(int currentIteration, int maxIterations, int percent, string status)
    {
        CurrentIteration = currentIteration;
        MaxIterations = maxIterations;
        ProgressPercent = percent;
        ProgressStatus = status;
    }
}

public class EncodeService(BinaryLocatorService binaryLocatorService)
{
    public record EncodeResult(
        bool Success,
        FilePath FilePath,
        long FileSizeBytes,
        long TargetSizeBytes,
        int Iteration,
        double VideoBitrateKbps,
        double ElapsedSeconds);

    public event EventHandler? Started;
    public event EventHandler<ProgressEventArgs>? Progress;
    public event EventHandler<EncodeResult?>? Finished;

    internal sealed record VideoStreamInfo(
        string CodecName,
        int Width,
        int Height,
        string PixelFormat,
        string? FrameRate,
        string? ColorRange,
        string? ColorSpace,
        string? ColorTransfer,
        string? ColorPrimaries,
        double? BitrateKbps);

    internal sealed record AudioStreamInfo(string CodecName, int Channels, double? BitrateKbps);

    internal sealed record MediaInfo(
        double DurationSeconds,
        double? FormatBitrateKbps,
        VideoStreamInfo Video,
        AudioStreamInfo? Audio);

    internal enum AudioMode
    {
        None,
        Copy,
        Encode
    }

    internal sealed record AudioPlan(AudioMode Mode, int EstimatedBitrateKbps);

    private record Sample(double BitrateKbps, long FileSize, int Iteration);

    private record ProcessResult(int ExitCode, string StandardError);

    private const long BytesPerMegabyte = 1024L * 1024L;
    private const int MinVideoBitrate = 100;
    private const int MinAudioBitrate = 32;
    private const double TargetUtilization = 0.99;

    private CancellationTokenSource? _cts;

    public async Task<EncodeResult> ResizeVideoToTargetAsync(FilePath inputFile,
                                                             FilePath outputFile,
                                                             int targetSizeMb,
                                                             double tolerancePercent,
                                                             int maxIterations,
                                                             int qualityPreset)
    {
        if (!inputFile.Exists)
            throw new ArgumentException("Input file does not exist.");

        if (targetSizeMb < 1)
            throw new ArgumentException("Target size must be greater than 0.");

        if (tolerancePercent is < 0.0 or > 50.0)
            throw new ArgumentException("Tolerance must be between 0 and 50.");

        if (maxIterations <= 0)
            throw new ArgumentException("Max iterations must be greater than 0.");

        if (qualityPreset is < 1 or > 4)
            throw new ArgumentException("Quality preset must be between 1 and 4.");

        if (inputFile.FullPath == outputFile.FullPath)
            throw new ArgumentException("Output file cannot be the same as input file.");

        _cts?.Cancel();
        _cts?.Dispose();
        var operationCts = new CancellationTokenSource();
        _cts = operationCts;

        var ct = operationCts.Token;
        EncodeResult? completedResult = null;

        Started?.Invoke(this, EventArgs.Empty);

        try
        {
            var startedAt = Stopwatch.GetTimestamp();
            var ffprobePath = await RequireBinaryPathAsync("ffprobe", "FFprobe was not found.").ConfigureAwait(false);
            var ffmpegPath = await RequireBinaryPathAsync("ffmpeg", "FFmpeg was not found.").ConfigureAwait(false);
            var targetSizeBytes = targetSizeMb * BytesPerMegabyte;
            var currentVideoSize = inputFile.FileInfo().Length;

            VideoSizeBelowTargetSizeException.ThrowIf(currentVideoSize <= targetSizeBytes, "Video file size is at or below target file size.");

            var mediaInfo = await GetMediaInfoAsync(ffprobePath, inputFile, ct).ConfigureAwait(false);
            var duration = mediaInfo.DurationSeconds;
            if (duration <= 0.0)
            {
                throw new InvalidOperationException("Input video duration is invalid or unavailable.");
            }

            var remuxedSize = await TryLosslessRemuxAsync(
                ffmpegPath,
                inputFile,
                outputFile,
                targetSizeBytes,
                mediaInfo,
                startedAt,
                ct).ConfigureAwait(false);
            if (remuxedSize is not null)
            {
                completedResult = remuxedSize;
                return remuxedSize;
            }

            var audioPlan = SelectAudioPlan(mediaInfo, targetSizeBytes);
            var sourceBitrate = mediaInfo.Video.BitrateKbps ??
                                Math.Max(0.0, (mediaInfo.FormatBitrateKbps ?? currentVideoSize * 8.0 / duration / 1_000.0) -
                                              (mediaInfo.Audio?.BitrateKbps ?? 0.0));

            double targetBitrate = CalculateTargetBitrate(duration, targetSizeBytes, audioPlan.EstimatedBitrateKbps);
            double minBitrate = MinVideoBitrate;
            double maxBitrate = targetBitrate * 2;

            if (sourceBitrate > 0.0)
            {
                var sourceVideoCap = Math.Max(MinVideoBitrate, sourceBitrate);
                maxBitrate = Math.Min(maxBitrate, sourceVideoCap * 1.1);
            }

            maxBitrate = Math.Max(maxBitrate, targetBitrate);

            double currentBitrate = targetBitrate;
            Sample? bestUnder = null;
            Sample? bestOver = null;
            long? lastEncodedSize = null;

            var tempOutput = CreateTemporaryMp4Path();
            var bestUnderOutput = CreateTemporaryMp4Path();

            try
            {
                int iteration = 0;
                while (iteration < maxIterations)
                {
                    ct.ThrowIfCancellationRequested();

                    iteration++;

                    Progress?.Invoke(this, new ProgressEventArgs(iteration, maxIterations, 0, $"Encoding at {currentBitrate:F0} kbps"));

                    await EncodeVideoAsync(
                        ffmpegPath,
                        inputFile,
                        tempOutput,
                        currentBitrate,
                        audioPlan,
                        mediaInfo,
                        qualityPreset,
                        duration,
                        iteration,
                        maxIterations,
                        ct
                    ).ConfigureAwait(false);

                    long newFileSize = tempOutput.FileInfo().Length;

                    lastEncodedSize = newFileSize;

                    if (newFileSize <= targetSizeBytes)
                    {
                        if (bestUnder == null || newFileSize > bestUnder.FileSize)
                        {
                            bestUnder = new Sample(currentBitrate, newFileSize, iteration);
                            File.Move(tempOutput.FullPath, bestUnderOutput.FullPath, overwrite: true);
                        }

                        if (IsWithinTolerance(newFileSize, targetSizeBytes, tolerancePercent))
                        {
                            PublishOutput(bestUnderOutput, outputFile);

                            var result = new EncodeResult(
                                Success: true,
                                FilePath: outputFile,
                                FileSizeBytes: newFileSize,
                                TargetSizeBytes: targetSizeBytes,
                                Iteration: iteration,
                                VideoBitrateKbps: currentBitrate,
                                ElapsedSeconds: ElapsedSecondsSince(startedAt));

                            completedResult = result;

                            return result;
                        }

                        minBitrate = currentBitrate;
                    }
                    else
                    {
                        if (bestOver == null || newFileSize < bestOver.FileSize)
                        {
                            bestOver = new Sample(currentBitrate, newFileSize, iteration);
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

                if (bestUnder is not null)
                {
                    PublishOutput(bestUnderOutput, outputFile);

                    var result = new EncodeResult(
                        Success: false,
                        FilePath: outputFile,
                        FileSizeBytes: bestUnder.FileSize,
                        TargetSizeBytes: targetSizeBytes,
                        Iteration: bestUnder.Iteration,
                        VideoBitrateKbps: bestUnder.BitrateKbps,
                        ElapsedSeconds: ElapsedSecondsSince(startedAt));

                    completedResult = result;

                    return result;
                }

                UnableToReachTargetSizeException.ThrowIf(
                    bestOver != null,
                    $"Could not reach target after {maxIterations} iterations. Closest over-target result was {bestOver!.FileSize.ToFileSizeString()} at {bestOver.BitrateKbps:F0} kbps."
                );

                var finalSize = lastEncodedSize ?? 0;
                throw new UnableToReachTargetSizeException($"Could not reach target after {maxIterations} iterations. Final result was {finalSize.ToFileSizeString()}.");
            }
            finally
            {
                tempOutput.Unlink(true);
                bestUnderOutput.Unlink(true);
            }
        }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _cts, null, operationCts), operationCts))
            {
                operationCts.Dispose();
                Finished?.Invoke(this, completedResult);
            }
        }
    }

    public void CancelEncoding()
    {
        _cts?.Cancel();
    }

    private async Task<EncodeResult?> TryLosslessRemuxAsync(string ffmpegPath,
                                                            FilePath inputFile,
                                                            FilePath outputFile,
                                                            long targetSizeBytes,
                                                            MediaInfo mediaInfo,
                                                            long startedAt,
                                                            CancellationToken ct)
    {
        var temporaryOutput = CreateTemporaryMp4Path();
        Progress?.Invoke(this, new ProgressEventArgs(0, 1, 0, "Checking lossless remux"));

        try
        {
            var args = new List<string>
            {
                "-hide_banner", "-loglevel", "error", "-y",
                "-i", inputFile.FullPath,
                "-map", "0:v:0",
                "-map", "0:a:0?",
                "-map_metadata", "0",
                "-map_chapters", "0",
                "-c", "copy",
                "-movflags", "+faststart",
                temporaryOutput.AsPosix()
            };
            var processResult = await ExecuteProcessAsync(ffmpegPath, args, null, ct).ConfigureAwait(false);
            if (processResult.ExitCode != 0 || !temporaryOutput.Exists)
            {
                return null;
            }

            var remuxedSize = temporaryOutput.FileInfo().Length;
            if (remuxedSize > targetSizeBytes)
            {
                return null;
            }

            PublishOutput(temporaryOutput, outputFile);
            return new EncodeResult(
                Success: true,
                FilePath: outputFile,
                FileSizeBytes: remuxedSize,
                TargetSizeBytes: targetSizeBytes,
                Iteration: 0,
                VideoBitrateKbps: mediaInfo.Video.BitrateKbps ?? 0,
                ElapsedSeconds: ElapsedSecondsSince(startedAt));
        }
        finally
        {
            temporaryOutput.Unlink(true);
        }
    }

    private static async Task<MediaInfo> GetMediaInfoAsync(string ffprobePath, FilePath inputFile, CancellationToken ct)
    {
        var args = new List<string>
        {
            "-v", "error",
            "-show_entries", "format=duration,bit_rate:stream=codec_type,codec_name,width,height,pix_fmt,avg_frame_rate,bit_rate,channels,color_range,color_space,color_transfer,color_primaries",
            "-of", "json",
            inputFile.FullPath
        };
        var json = new StringBuilder();
        var result = await ExecuteProcessAsync(ffprobePath, args, line =>
        {
            json.AppendLine(line);
            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);

        if (result.ExitCode != 0 || json.Length == 0)
        {
            throw new InvalidOperationException("FFprobe failed to read input video metadata.");
        }

        using var document = JsonDocument.Parse(json.ToString());
        var root = document.RootElement;
        if (!root.TryGetProperty("format", out var format) ||
            !TryGetDouble(format, "duration", out var duration))
        {
            throw new InvalidOperationException("FFprobe returned an invalid duration value.");
        }

        VideoStreamInfo? video = null;
        AudioStreamInfo? audio = null;
        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var stream in streams.EnumerateArray())
            {
                var codecType = GetString(stream, "codec_type");
                if (video is null && codecType == "video")
                {
                    video = new VideoStreamInfo(
                        CodecName: GetString(stream, "codec_name") ?? "unknown",
                        Width: GetInt32(stream, "width"),
                        Height: GetInt32(stream, "height"),
                        PixelFormat: GetString(stream, "pix_fmt") ?? "yuv420p",
                        FrameRate: GetString(stream, "avg_frame_rate"),
                        ColorRange: GetString(stream, "color_range"),
                        ColorSpace: GetString(stream, "color_space"),
                        ColorTransfer: GetString(stream, "color_transfer"),
                        ColorPrimaries: GetString(stream, "color_primaries"),
                        BitrateKbps: GetBitrateKbps(stream));
                }
                else if (audio is null && codecType == "audio")
                {
                    audio = new AudioStreamInfo(
                        CodecName: GetString(stream, "codec_name") ?? "unknown",
                        Channels: GetInt32(stream, "channels"),
                        BitrateKbps: GetBitrateKbps(stream));
                }
            }
        }

        return new MediaInfo(
            DurationSeconds: duration,
            FormatBitrateKbps: GetBitrateKbps(format),
            Video: video ?? throw new InvalidOperationException("FFprobe did not find a video stream."),
            Audio: audio);

        static string? GetString(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        static int GetInt32(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

        static bool TryGetDouble(JsonElement element, string name, out double value) =>
            double.TryParse(GetString(element, name), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        static double? GetBitrateKbps(JsonElement element) =>
            TryGetDouble(element, "bit_rate", out var bitrate) ? bitrate / 1_000.0 : null;
    }

    private async Task EncodeVideoAsync(string ffmpegPath,
                                        FilePath inputFile,
                                        FilePath outputFile,
                                        double videoBitrate,
                                        AudioPlan audioPlan,
                                        MediaInfo mediaInfo,
                                        int qualityPreset,
                                        double duration,
                                        int iteration,
                                        int maxIterations,
                                        CancellationToken ct)
    {
        var commonArgs = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-i", inputFile.FullPath,
            "-map", "0:v:0",
            "-b:v", string.Format(CultureInfo.InvariantCulture, "{0:F0}k", videoBitrate)
        };

        commonArgs.AddRange(GetEncodeSettings(qualityPreset, mediaInfo));

        var passLogPrefix = Path.Combine(Path.GetTempPath(), $"squash-pass-{Guid.NewGuid():N}");

        try
        {
            var firstPassArgs = new List<string>(commonArgs);
            firstPassArgs.AddRange([
                "-an", "-pass", "1", "-passlogfile", passLogPrefix,
                "-f", "null", "-progress", "pipe:1", "-nostats", "NUL"
            ]);
            await RunPassAsync(firstPassArgs, passNumber: 1).ConfigureAwait(false);

            var secondPassArgs = new List<string>(commonArgs);
            AddAudioArguments(secondPassArgs, audioPlan);
            secondPassArgs.AddRange([
                "-map_metadata", "0", "-fps_mode:v", "passthrough",
                "-pass", "2", "-passlogfile", passLogPrefix,
                "-progress", "pipe:1", "-nostats", outputFile.AsPosix()
            ]);
            await RunPassAsync(secondPassArgs, passNumber: 2).ConfigureAwait(false);
        }
        finally
        {
            foreach (var passLogFile in Directory.EnumerateFiles(Path.GetTempPath(), $"{Path.GetFileName(passLogPrefix)}*"))
            {
                try
                {
                    File.Delete(passLogFile);
                }
                catch (IOException)
                {
                    // A failed cleanup must not hide the encode result.
                }
                catch (UnauthorizedAccessException)
                {
                    // A failed cleanup must not hide the encode result.
                }
            }
        }

        Progress?.Invoke(this, new ProgressEventArgs(iteration, maxIterations, 100, "Iteration complete"));

        async Task RunPassAsync(List<string> args, int passNumber)
        {
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

                var key = trimmed[..sep];
                var value = trimmed[(sep + 1)..];

                progressData[key] = value;

                if (key == "progress" && value == "continue")
                {
                    var passPercent = ComputePercent(progressData, duration);
                    var percent = (passNumber - 1) * 50 + passPercent / 2;
                    var status = BuildProgressStatus(progressData, duration);
                    var title = $"Pass {passNumber}/2{(status.IsNullOrWhiteSpace() ? "" : $": {status}")}";

                    Progress?.Invoke(this, new ProgressEventArgs(iteration, maxIterations, percent, title));
                }

                return Task.CompletedTask;
            }, ct).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                var message = $"FFmpeg pass {passNumber} failed with exit code {result.ExitCode}.";
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
        }
    }

    private static async Task<ProcessResult> ExecuteProcessAsync(string executable,
                                                                 List<string> arguments,
                                                                 Func<string, Task>? onStdoutLine,
                                                                 CancellationToken ct
    )
    {
        var psi = new ProcessStartInfo(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            CreateNoWindow = true
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

    private static IEnumerable<string> GetEncodeSettings(int qualityPreset, MediaInfo mediaInfo)
    {
        var useTenBit = ShouldUseTenBit(mediaInfo.Video);
        var (codec, preset) = qualityPreset switch
        {
            1 => ("libx264", "medium"),
            2 => ("libx265", "medium"),
            3 => ("libx265", "slow"),
            4 => ("libx265", "veryslow"),
            _ => throw new ArgumentException($"Unexpected quality preset: {qualityPreset}")
        };

        var profile = codec == "libx265"
            ? useTenBit ? "main10" : "main"
            : useTenBit ? "high10" : "high";
        var settings = new List<string>
        {
            "-c:v", codec,
            "-preset", preset,
            "-profile:v", profile,
            "-movflags", "+faststart",
            "-pix_fmt", useTenBit ? "yuv420p10le" : "yuv420p"
        };

        if (codec == "libx264")
        {
            settings.AddRange(["-fastfirstpass", "0"]);
        }

        AddColorMetadata(settings, mediaInfo.Video);
        return settings;
    }

    private static void AddAudioArguments(List<string> arguments, AudioPlan audioPlan)
    {
        switch (audioPlan.Mode)
        {
            case AudioMode.None:
                arguments.Add("-an");
                break;
            case AudioMode.Copy:
                arguments.AddRange(["-map", "0:a:0?", "-c:a", "copy"]);
                break;
            case AudioMode.Encode:
                arguments.AddRange(["-map", "0:a:0?", "-c:a", "aac", "-b:a", $"{audioPlan.EstimatedBitrateKbps}k"]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(audioPlan));
        }
    }

    private static void AddColorMetadata(List<string> arguments, VideoStreamInfo video)
    {
        AddIfKnown("-color_range", video.ColorRange);
        AddIfKnown("-colorspace", video.ColorSpace);
        AddIfKnown("-color_trc", video.ColorTransfer);
        AddIfKnown("-color_primaries", video.ColorPrimaries);
        return;

        void AddIfKnown(string option, string? value)
        {
            if (!value.IsNullOrWhiteSpace() && value != "unknown")
            {
                arguments.AddRange([option, value!]);
            }
        }
    }

    internal static bool ShouldUseTenBit(VideoStreamInfo video) =>
        video.PixelFormat.Contains("10", StringComparison.OrdinalIgnoreCase) ||
        video.PixelFormat.Contains("12", StringComparison.OrdinalIgnoreCase) ||
        video.ColorTransfer is "smpte2084" or "arib-std-b67";

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
        var parts = new List<string>();
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

        return Math.Max(MinVideoBitrate, targetSizeBytes * 8.0 / duration / 1_000.0 * TargetUtilization - audioBitrate);
    }

    internal static AudioPlan SelectAudioPlan(MediaInfo mediaInfo, long targetSizeBytes)
    {
        if (mediaInfo.Audio is null)
        {
            return new AudioPlan(AudioMode.None, 0);
        }

        var totalBitrate = targetSizeBytes * 8.0 / mediaInfo.DurationSeconds / 1_000.0 * TargetUtilization;
        var maxAudioBitrate = Math.Max(MinAudioBitrate, (int)Math.Floor(totalBitrate - MinVideoBitrate));
        var preferredBitrate = mediaInfo.Audio.Channels switch
        {
            <= 1 => 64,
            2 => 128,
            _ => 192
        };
        if (mediaInfo.Audio.BitrateKbps is > 0)
        {
            preferredBitrate = Math.Min(preferredBitrate, (int)Math.Ceiling(mediaInfo.Audio.BitrateKbps.Value));
        }

        var selectedBitrate = Math.Clamp(preferredBitrate, MinAudioBitrate, maxAudioBitrate);
        var canCopy = mediaInfo.Audio.CodecName.Equals("aac", StringComparison.OrdinalIgnoreCase) &&
                      mediaInfo.Audio.BitrateKbps is > 0 &&
                      mediaInfo.Audio.BitrateKbps <= selectedBitrate;

        return canCopy
            ? new AudioPlan(AudioMode.Copy, (int)Math.Ceiling(mediaInfo.Audio.BitrateKbps!.Value))
            : new AudioPlan(AudioMode.Encode, selectedBitrate);
    }

    private static double EstimateNextBitrate(double currentBitrate,
                                              long currentSize,
                                              long targetSize,
                                              double minBitrate,
                                              double maxBitrate,
                                              Sample? under,
                                              Sample? over)
    {
        double nextBitrate = (under != null && over != null)
            ? over.FileSize - under.FileSize > 0
                ? under.BitrateKbps + (targetSize - under.FileSize) * (over.BitrateKbps - under.BitrateKbps) / (over.FileSize - under.FileSize)
                : (minBitrate + maxBitrate) / 2.0
            : currentSize > 0
                ? currentBitrate * ((double)targetSize / currentSize)
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

    internal static bool IsWithinTolerance(long fileSize, long targetSize, double tolerancePercent) =>
        fileSize <= targetSize && targetSize - fileSize <= targetSize * (tolerancePercent / 100.0);

    private static FilePath CreateTemporaryMp4Path() =>
        FilePath.From(Path.Combine(Path.GetTempPath(), $"squash-{Guid.NewGuid():N}.mp4"));

    private static void PublishOutput(FilePath source, FilePath destination)
    {
        destination.Parent.Mkdir(true, true);
        File.Copy(source.FullPath, destination.FullPath, overwrite: true);
    }
}
