using System.Text;
using System.Text.Json;
using Caprine.FilePath;
using System.Diagnostics;
using System.Globalization;
using Squash.Core.Extensions;
using Squash.Core.Exceptions;

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
    internal enum AudioMode
    {
        None,
        Copy
    }

    public record EncodeResult(
        bool Success,
        FilePath FilePath,
        long FileSizeBytes,
        long TargetSizeBytes,
        int Iteration,
        double VideoBitrateKbps,
        double ElapsedSeconds);

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
        string? ChromaLocation,
        double? BitrateKbps);

    internal sealed record AudioStreamInfo(
        int Index,
        string CodecName,
        int Channels,
        string? ChannelLayout,
        double? BitrateKbps);

    internal sealed record MediaInfo(
        double DurationSeconds,
        double? FormatBitrateKbps,
        VideoStreamInfo Video,
        IReadOnlyList<AudioStreamInfo> AudioStreams);

    internal sealed record AudioPlan(AudioMode Mode, long FixedBytes, int EstimatedBitrateKbps);

    private record Sample(double BitrateKbps, long FileSize, long VideoPayloadBytes, int Iteration);

    private record ProcessResult(int ExitCode, string StandardError);

    private const long BytesPerMegabyte = 1_000_000L;
    private const int MinVideoBitrate = 100;
    private const long MinimumMuxReserveBytes = 64L * 1024L;
    private const long MinimumRefinementSafetyBytes = 4L * 1024L;
    private const double InitialMuxReserveFraction = 0.005;
    private const double RefinementSafetyFraction = 0.0005;

    public event EventHandler? Started;
    public event EventHandler<ProgressEventArgs>? Progress;
    public event EventHandler<EncodeResult?>? Finished;

    private CancellationTokenSource? _cts;

    public Task<EncodeResult> ResizeVideoToTargetAsync(FilePath inputFile,
                                                       FilePath outputFile,
                                                       int targetSizeMb,
                                                       double tolerancePercent,
                                                       int maxIterations,
                                                       int qualityPreset,
                                                       bool includeAudio = true)
    {
        if (targetSizeMb < 1)
            throw new ArgumentException("Target size must be greater than 0.");

        return ResizeVideoToTargetBytesAsync(
            inputFile,
            outputFile,
            checked(targetSizeMb * BytesPerMegabyte),
            tolerancePercent,
            maxIterations,
            qualityPreset,
            includeAudio);
    }

    public async Task<EncodeResult> ResizeVideoToTargetBytesAsync(FilePath inputFile,
                                                                  FilePath outputFile,
                                                                  long targetSizeBytes,
                                                                  double tolerancePercent,
                                                                  int maxIterations,
                                                                  int qualityPreset,
                                                                  bool includeAudio = true)
    {
        if (!inputFile.Exists)
            throw new ArgumentException("Input file does not exist.");

        if (targetSizeBytes <= 0)
            throw new ArgumentException("Target size must be greater than 0.");

        if (tolerancePercent is < 0.0 or > 50.0)
            throw new ArgumentException("Tolerance must be between 0 and 50.");

        if (maxIterations <= 0)
            throw new ArgumentException("Max iterations must be greater than 0.");

        if (qualityPreset is < 1 or > 5)
            throw new ArgumentException("Quality preset must be between 1 and 5.");

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
                includeAudio,
                ct).ConfigureAwait(false);
            if (remuxedSize is not null)
            {
                completedResult = remuxedSize;

                return remuxedSize;
            }

            var encoderName = await ResolveEncoderNameAsync(ffmpegPath, qualityPreset, ct).ConfigureAwait(false);

            var audioPlan = await PrepareAudioPlanAsync(
                ffmpegPath,
                inputFile,
                mediaInfo,
                targetSizeBytes,
                includeAudio,
                ct).ConfigureAwait(false);
            var totalAudioBitrate = mediaInfo.AudioStreams.Sum(stream => stream.BitrateKbps ?? 0.0);
            var sourceBitrate = mediaInfo.Video.BitrateKbps ??
                                Math.Max(0.0, (mediaInfo.FormatBitrateKbps ?? currentVideoSize * 8.0 / duration / 1_000.0) -
                                              totalAudioBitrate);

            double targetBitrate = CalculateTargetBitrate(duration, targetSizeBytes, audioPlan.FixedBytes);
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
            // Analysis is input- and preset-specific, so refinement passes can reuse it at a new target bitrate.
            var passLogPrefix = Path.Combine(Path.GetTempPath(), $"squash-pass-{Guid.NewGuid():N}");

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
                        encoderName,
                        duration,
                        iteration,
                        maxIterations,
                        passLogPrefix,
                        runFirstPass: iteration == 1,
                        ct
                    ).ConfigureAwait(false);

                    long newFileSize = tempOutput.FileInfo().Length;
                    var videoPayloadBytes = await GetPacketPayloadSizeAsync(
                        ffprobePath,
                        tempOutput,
                        "v:0",
                        ct).ConfigureAwait(false);

                    lastEncodedSize = newFileSize;

                    if (newFileSize <= targetSizeBytes)
                    {
                        if (bestUnder == null || newFileSize > bestUnder.FileSize)
                        {
                            bestUnder = new Sample(currentBitrate, newFileSize, videoPayloadBytes, iteration);
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
                            bestOver = new Sample(currentBitrate, newFileSize, videoPayloadBytes, iteration);
                        }

                        maxBitrate = currentBitrate;
                    }

                    currentBitrate = EstimateNextBitrate(
                        currentBitrate: currentBitrate,
                        currentSize: newFileSize,
                        targetSize: targetSizeBytes,
                        duration: duration,
                        videoPayloadBytes: videoPayloadBytes,
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
                DeletePassLogFiles(passLogPrefix);
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
                                                            bool includeAudio,
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
                "-map", "0:v:0"
            };
            if (includeAudio)
            {
                args.AddRange(["-map", "0:a?"]);
            }
            else
            {
                args.Add("-an");
            }

            args.AddRange([
                "-map_metadata", "0",
                "-map_metadata:s:v:0", "0:s:v:0",
                "-map_chapters", "0",
                "-c", "copy",
                "-movflags", "+faststart+write_colr",
                temporaryOutput.AsPosix()
            ]);

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
            "-show_entries", "format=duration,bit_rate:stream=index,codec_type,codec_name,width,height,pix_fmt,avg_frame_rate,bit_rate,channels,channel_layout,color_range,color_space,color_transfer,color_primaries,chroma_location",
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
        var audioStreams = new List<AudioStreamInfo>();
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
                        ChromaLocation: GetString(stream, "chroma_location"),
                        BitrateKbps: GetBitrateKbps(stream));
                }
                else if (codecType == "audio")
                {
                    audioStreams.Add(new AudioStreamInfo(
                        Index: GetInt32(stream, "index"),
                        CodecName: GetString(stream, "codec_name") ?? "unknown",
                        Channels: GetInt32(stream, "channels"),
                        ChannelLayout: GetString(stream, "channel_layout"),
                        BitrateKbps: GetBitrateKbps(stream)));
                }
            }
        }

        return new MediaInfo(
            DurationSeconds: duration,
            FormatBitrateKbps: GetBitrateKbps(format),
            Video: video ?? throw new InvalidOperationException("FFprobe did not find a video stream."),
            AudioStreams: audioStreams);

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

    private static async Task<AudioPlan> PrepareAudioPlanAsync(string ffmpegPath,
                                                               FilePath inputFile,
                                                               MediaInfo mediaInfo,
                                                               long targetSizeBytes,
                                                               bool includeAudio,
                                                               CancellationToken ct)
    {
        if (!includeAudio || mediaInfo.AudioStreams.Count == 0)
        {
            return new AudioPlan(AudioMode.None, 0, 0);
        }

        var temporaryOutput = CreateTemporaryMp4Path();
        var args = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-i", inputFile.FullPath,
            "-vn",
            "-map", "0:a?",
            "-map_metadata", "0",
            "-map_chapters", "0",
            "-c:a", "copy",
            "-movflags", "+faststart",
            temporaryOutput.AsPosix()
        };

        try
        {
            var result = await ExecuteProcessAsync(ffmpegPath, args, null, ct).ConfigureAwait(false);
            if (result.ExitCode != 0 || !temporaryOutput.Exists)
            {
                var streams = string.Join(", ", mediaInfo.AudioStreams.Select(stream => $"#{stream.Index} {stream.CodecName}"));
                var detail = result.StandardError.Trim();
                var message = $"The selected MP4 output cannot preserve all audio streams without re-encoding ({streams}).";
                if (!detail.IsNullOrWhiteSpace())
                {
                    message += $" {detail}";
                }

                throw new IncompatibleAudioStreamsException(message);
            }

            return SelectAudioPlan(mediaInfo, targetSizeBytes, temporaryOutput.FileInfo().Length);
        }
        finally
        {
            temporaryOutput.Unlink(true);
        }
    }

    private static async Task<long> GetPacketPayloadSizeAsync(string ffprobePath,
                                                              FilePath inputFile,
                                                              string streamSpecifier,
                                                              CancellationToken ct)
    {
        var args = new List<string>
        {
            "-v", "error",
            "-select_streams", streamSpecifier,
            "-show_entries", "packet=size",
            "-of", "csv=p=0",
            inputFile.FullPath
        };
        long totalBytes = 0;
        var result = await ExecuteProcessAsync(ffprobePath, args, line =>
        {
            if (long.TryParse(line.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var packetSize))
            {
                totalBytes = checked(totalBytes + packetSize);
            }

            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"FFprobe failed to measure encoded video payload. {result.StandardError.Trim()}");
        }

        return totalBytes;
    }

    private static async Task<string> ResolveEncoderNameAsync(string ffmpegPath, int qualityPreset, CancellationToken ct)
    {
        var preferredEncoders = qualityPreset == 5
            ? new[] { "libsvtav1", "libaom-av1" }
            : [GetEncoderName(qualityPreset)];
        var availableEncoders = new HashSet<string>(StringComparer.Ordinal);
        var result = await ExecuteProcessAsync(
            ffmpegPath,
            ["-hide_banner", "-encoders"],
            line =>
            {
                foreach (var encoder in preferredEncoders)
                {
                    if (line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(encoder, StringComparer.Ordinal))
                    {
                        availableEncoders.Add(encoder);
                    }
                }

                return Task.CompletedTask;
            },
            ct).ConfigureAwait(false);
        var selectedEncoder = preferredEncoders.FirstOrDefault(availableEncoders.Contains);
        if (result.ExitCode != 0 || selectedEncoder is null)
        {
            throw new InvalidOperationException(
                $"The selected quality preset requires one of these FFmpeg encoders: {string.Join(", ", preferredEncoders)}.");
        }

        return selectedEncoder;
    }

    private async Task EncodeVideoAsync(string ffmpegPath,
                                        FilePath inputFile,
                                        FilePath outputFile,
                                        double videoBitrate,
                                        AudioPlan audioPlan,
                                        MediaInfo mediaInfo,
                                        int qualityPreset,
                                        string encoderName,
                                        double duration,
                                        int iteration,
                                        int maxIterations,
                                        string passLogPrefix,
                                        bool runFirstPass,
                                        CancellationToken ct)
    {
        var commonArgs = new List<string>
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-i", inputFile.FullPath,
            "-map", "0:v:0",
            "-b:v", string.Format(CultureInfo.InvariantCulture, "{0:F0}k", videoBitrate)
        };

        commonArgs.AddRange(GetEncodeSettings(qualityPreset, mediaInfo, encoderName));

        if (runFirstPass)
        {
            var firstPassArgs = new List<string>(commonArgs);
            firstPassArgs.AddRange([
                "-an", "-pass", "1", "-passlogfile", passLogPrefix,
                "-f", "null", "-progress", "pipe:1", "-nostats", "NUL"
            ]);

            await RunPassAsync(firstPassArgs, passNumber: 1, passCount: 2).ConfigureAwait(false);
        }

        var secondPassArgs = new List<string>(commonArgs);
        AddAudioArguments(secondPassArgs, audioPlan);
        secondPassArgs.AddRange([
            "-map_metadata", "0",
            "-map_metadata:s:v:0", "0:s:v:0",
            "-map_chapters", "0",
            "-fps_mode:v", "passthrough",
            "-pass", "2", "-passlogfile", passLogPrefix,
            "-progress", "pipe:1", "-nostats", outputFile.AsPosix()
        ]);

        var secondPassNumber = runFirstPass ? 2 : 1;
        var passCount = runFirstPass ? 2 : 1;

        await RunPassAsync(secondPassArgs, secondPassNumber, passCount).ConfigureAwait(false);

        Progress?.Invoke(this, new ProgressEventArgs(iteration, maxIterations, 100, "Iteration complete"));

        async Task RunPassAsync(List<string> args, int passNumber, int passCount)
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
                    var percent = ((passNumber - 1) * 100 + passPercent) / passCount;
                    var status = BuildProgressStatus(progressData, duration);
                    var title = $"Pass {passNumber}/{passCount}{(status.IsNullOrWhiteSpace() ? "" : $": {status}")}";

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

    private static void DeletePassLogFiles(string passLogPrefix)
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

    internal static IEnumerable<string> GetEncodeSettings(
        int qualityPreset,
        MediaInfo mediaInfo,
        string? encoderOverride = null)
    {
        var codec = encoderOverride ?? GetEncoderName(qualityPreset);
        var preset = qualityPreset switch
        {
            1 or 2 => "medium",
            3 => "slow",
            4 => "veryslow",
            5 => "6",
            _ => throw new ArgumentException($"Unexpected quality preset: {qualityPreset}")
        };
        var pixelFormat = SelectOutputPixelFormat(codec, mediaInfo.Video);
        var settings = new List<string>
        {
            "-c:v", codec,
            "-movflags", "+faststart+write_colr",
            "-pix_fmt", pixelFormat
        };

        if (codec == "libx264")
        {
            settings.AddRange(["-preset", preset]);
            settings.AddRange(["-profile:v", GetX264Profile(pixelFormat)]);
            settings.AddRange(["-fastfirstpass", "1"]);
        }
        else if (codec == "libx265")
        {
            settings.AddRange(["-preset", preset]);
            settings.AddRange(["-tag:v", "hvc1"]);
        }
        else if (codec == "libsvtav1")
        {
            settings.AddRange(["-preset", preset, "-tag:v", "av01", "-svtav1-params", "tune=0"]);
        }
        else
        {
            settings.AddRange(["-cpu-used", "2", "-usage", "good", "-row-mt", "1", "-tag:v", "av01"]);
        }

        AddColorMetadata(settings, mediaInfo.Video);

        return settings;
    }

    internal static string SelectOutputPixelFormat(string encoder, VideoStreamInfo video)
    {
        var source = video.PixelFormat.ToLowerInvariant();
        var highBitDepth = ShouldUseTenBit(video);
        if (encoder == "libsvtav1")
        {
            return highBitDepth ? "yuv420p10le" : "yuv420p";
        }

        if (source.StartsWith("yuv420p", StringComparison.Ordinal) || source.StartsWith("yuva420p", StringComparison.Ordinal))
        {
            return highBitDepth ? NormalizeBitDepth(source, "yuv420p") : "yuv420p";
        }

        if (source.StartsWith("yuv422p", StringComparison.Ordinal) || source.StartsWith("yuva422p", StringComparison.Ordinal))
        {
            return highBitDepth ? NormalizeBitDepth(source, "yuv422p") : "yuv422p";
        }

        if (source.StartsWith("yuv444p", StringComparison.Ordinal) || source.StartsWith("yuva444p", StringComparison.Ordinal))
        {
            return highBitDepth ? NormalizeBitDepth(source, "yuv444p") : "yuv444p";
        }

        if (encoder == "libx265" && source.StartsWith("gbrp", StringComparison.Ordinal))
        {
            return highBitDepth ? NormalizeBitDepth(source, "gbrp") : "gbrp";
        }

        return highBitDepth ? "yuv420p10le" : "yuv420p";

        static string NormalizeBitDepth(string source, string prefix)
        {
            if (source.Contains("12", StringComparison.Ordinal))
            {
                return $"{prefix}12le";
            }

            return $"{prefix}10le";
        }
    }

    private static string GetEncoderName(int qualityPreset) => qualityPreset switch
    {
        1 => "libx264",
        2 or 3 or 4 => "libx265",
        5 => "libsvtav1",
        _ => throw new ArgumentException($"Unexpected quality preset: {qualityPreset}")
    };

    private static string GetX264Profile(string pixelFormat)
    {
        if (pixelFormat.StartsWith("yuv444", StringComparison.Ordinal))
        {
            return "high444";
        }

        if (pixelFormat.StartsWith("yuv422", StringComparison.Ordinal))
        {
            return "high422";
        }

        return pixelFormat.Contains("10", StringComparison.Ordinal) ? "high10" : "high";
    }

    private static void AddAudioArguments(List<string> arguments, AudioPlan audioPlan)
    {
        switch (audioPlan.Mode)
        {
            case AudioMode.None:
                arguments.Add("-an");
                break;
            case AudioMode.Copy:
                arguments.AddRange(["-map", "0:a?", "-c:a", "copy"]);
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
        AddIfKnown("-chroma_sample_location", video.ChromaLocation);

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

    internal static double CalculateTargetBitrate(double duration, long targetSizeBytes, long fixedBytes)
    {
        if (duration <= 0.0)
        {
            return MinVideoBitrate;
        }

        var availableBytes = Math.Max(0L, targetSizeBytes - fixedBytes - CalculateInitialMuxReserve(targetSizeBytes));

        return Math.Max(MinVideoBitrate, availableBytes * 8.0 / duration / 1_000.0);
    }

    internal static AudioPlan SelectAudioPlan(MediaInfo mediaInfo, long targetSizeBytes, long fixedBytes)
    {
        if (mediaInfo.AudioStreams.Count == 0)
        {
            return new AudioPlan(AudioMode.None, 0, 0);
        }

        var minimumVideoBytes = (long)Math.Ceiling(MinVideoBitrate * 1_000.0 * mediaInfo.DurationSeconds / 8.0);
        var requiredBytes = checked(fixedBytes + CalculateInitialMuxReserve(targetSizeBytes) + minimumVideoBytes);
        if (requiredBytes >= targetSizeBytes)
        {
            throw new UnableToReachTargetSizeException(
                $"The preserved audio streams and minimum viable video require at least {requiredBytes.ToFileSizeString()}, " +
                $"which does not fit the {targetSizeBytes.ToFileSizeString()} target.");
        }

        var estimatedBitrate = mediaInfo.DurationSeconds > 0.0
            ? (int)Math.Ceiling(fixedBytes * 8.0 / mediaInfo.DurationSeconds / 1_000.0)
            : 0;

        return new AudioPlan(AudioMode.Copy, fixedBytes, estimatedBitrate);
    }

    private static double EstimateNextBitrate(double currentBitrate,
                                              long currentSize,
                                              long targetSize,
                                              double duration,
                                              long videoPayloadBytes,
                                              double minBitrate,
                                              double maxBitrate,
                                              Sample? under,
                                              Sample? over)
    {
        var effectiveTarget = Math.Max(1L, targetSize - CalculateRefinementSafety(targetSize));
        double nextBitrate = (under != null && over != null)
            ? over.FileSize - under.FileSize > 0
                ? under.BitrateKbps + (effectiveTarget - under.FileSize) * (over.BitrateKbps - under.BitrateKbps) / (over.FileSize - under.FileSize)
                : (minBitrate + maxBitrate) / 2.0
            : videoPayloadBytes > 0
                ? CalculatePayloadAdjustedBitrate()
                : (minBitrate + maxBitrate) / 2.0;

        nextBitrate = Math.Clamp(nextBitrate, minBitrate, maxBitrate);

        return Math.Abs(nextBitrate - currentBitrate) < 1.0 ? (minBitrate + maxBitrate) / 2.0 : nextBitrate;

        double CalculatePayloadAdjustedBitrate()
        {
            var residualBytes = Math.Max(0L, currentSize - videoPayloadBytes);
            var desiredVideoBytes = Math.Max(1L, effectiveTarget - residualBytes);
            var payloadRatioBitrate = currentBitrate * desiredVideoBytes / videoPayloadBytes;
            var absoluteBudgetBitrate = desiredVideoBytes * 8.0 / Math.Max(duration, 0.001) / 1_000.0;

            return Math.Min(payloadRatioBitrate, absoluteBudgetBitrate * 1.02);
        }
    }

    private static long CalculateInitialMuxReserve(long targetSizeBytes) =>
        Math.Max(MinimumMuxReserveBytes, (long)Math.Ceiling(targetSizeBytes * InitialMuxReserveFraction));

    private static long CalculateRefinementSafety(long targetSizeBytes) =>
        Math.Max(MinimumRefinementSafetyBytes, (long)Math.Ceiling(targetSizeBytes * RefinementSafetyFraction));

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
