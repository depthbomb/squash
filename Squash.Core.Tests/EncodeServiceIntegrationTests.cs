using Caprine.FilePath;
using System.Diagnostics;
using Squash.Core.Services;
using Squash.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Squash.Core.Tests;

[TestClass]
public sealed class EncodeServiceIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task EncodePreservesAllCompatibleAudioAndMeetsDecimalTarget()
    {
        var ffmpeg = FindExecutable("ffmpeg");
        var ffprobe = FindExecutable("ffprobe");
        if (ffmpeg is null || ffprobe is null)
        {
            Assert.Inconclusive("FFmpeg and FFprobe are required for integration tests.");
        }

        var directory = CreateTestDirectory();
        var input = Path.Combine(directory, "two-audio-input.mp4");
        var output = Path.Combine(directory, "two-audio-output.mp4");
        var silentOutput = Path.Combine(directory, "silent-output.mp4");

        try
        {
            await RunProcessAsync(ffmpeg, [
                "-hide_banner", "-loglevel", "error", "-y",
                "-f", "lavfi", "-i", "testsrc2=duration=4:size=320x240:rate=30",
                "-f", "lavfi", "-i", "sine=frequency=440:duration=4:sample_rate=48000",
                "-f", "lavfi", "-i", "sine=frequency=880:duration=4:sample_rate=48000",
                "-map", "0:v:0", "-map", "1:a:0", "-map", "2:a:0",
                "-c:v", "libx264", "-preset", "ultrafast", "-qp", "0",
                "-c:a", "aac", "-b:a", "96k",
                input
            ]);

            Assert.IsGreaterThan(1_000_000, new FileInfo(input).Length);

            var service = new EncodeService(new BinaryLocatorService());
            var result = await service.ResizeVideoToTargetAsync(
                FilePath.From(input),
                FilePath.From(output),
                1,
                5,
                5,
                1);

            Assert.AreEqual(1_000_000L, result.TargetSizeBytes);
            Assert.IsLessThanOrEqualTo(1_000_000, result.FileSizeBytes);
            Assert.AreEqual(2, await CountAudioStreamsAsync(ffprobe, output));

            for (var index = 0; index < 2; index++)
            {
                var inputHash = await GetDecodedAudioHashAsync(ffmpeg, input, index);
                var outputHash = await GetDecodedAudioHashAsync(ffmpeg, output, index);

                Assert.AreEqual(inputHash, outputHash, $"Decoded audio stream {index} changed.");
            }

            var silentResult = await service.ResizeVideoToTargetAsync(
                FilePath.From(input),
                FilePath.From(silentOutput),
                1,
                5,
                5,
                1,
                includeAudio: false);

            Assert.IsLessThanOrEqualTo(1_000_000, silentResult.FileSizeBytes);
            Assert.AreEqual(0, await CountAudioStreamsAsync(ffprobe, silentOutput));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task LosslessRemuxPreservesAllAudioWhenItFits()
    {
        var ffmpeg = FindExecutable("ffmpeg");
        var ffprobe = FindExecutable("ffprobe");
        if (ffmpeg is null || ffprobe is null)
        {
            Assert.Inconclusive("FFmpeg and FFprobe are required for integration tests.");
        }

        var directory = CreateTestDirectory();
        var input = Path.Combine(directory, "two-audio-input.ts");
        var referenceOutput = Path.Combine(directory, "reference-remux.mp4");
        var output = Path.Combine(directory, "service-remux.mp4");

        try
        {
            await RunProcessAsync(ffmpeg, [
                "-hide_banner", "-loglevel", "error", "-y",
                "-f", "lavfi", "-i", "testsrc2=duration=4:size=320x240:rate=30",
                "-f", "lavfi", "-i", "sine=frequency=440:duration=4:sample_rate=48000",
                "-f", "lavfi", "-i", "sine=frequency=880:duration=4:sample_rate=48000",
                "-map", "0:v:0", "-map", "1:a:0", "-map", "2:a:0",
                "-c:v", "libx264", "-preset", "ultrafast", "-crf", "18",
                "-c:a", "aac", "-b:a", "96k",
                "-f", "mpegts",
                input
            ]);
            await RunProcessAsync(ffmpeg, [
                "-hide_banner", "-loglevel", "error", "-y",
                "-i", input,
                "-map", "0:v:0", "-map", "0:a?",
                "-map_metadata", "0", "-map_metadata:s:v:0", "0:s:v:0", "-map_chapters", "0",
                "-c", "copy", "-movflags", "+faststart+write_colr",
                referenceOutput
            ]);

            var inputSize = new FileInfo(input).Length;
            var referenceSize = new FileInfo(referenceOutput).Length;

            Assert.IsGreaterThan(referenceSize, inputSize);

            var targetSize = referenceSize + (inputSize - referenceSize) / 2;
            var service = new EncodeService(new BinaryLocatorService());
            var result = await service.ResizeVideoToTargetBytesAsync(
                FilePath.From(input),
                FilePath.From(output),
                targetSize,
                1,
                3,
                1);

            Assert.AreEqual(0, result.Iteration);
            Assert.IsLessThanOrEqualTo(targetSize, result.FileSizeBytes);
            Assert.AreEqual(2, await CountAudioStreamsAsync(ffprobe, output));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ExperimentalAv1PresetUsesAnAvailableEncoder()
    {
        var ffmpeg = FindExecutable("ffmpeg");
        var ffprobe = FindExecutable("ffprobe");
        if (ffmpeg is null || ffprobe is null)
        {
            Assert.Inconclusive("FFmpeg and FFprobe are required for integration tests.");
        }

        var encoders = await RunProcessAsync(ffmpeg, ["-hide_banner", "-encoders"]);
        if (!encoders.StandardOutput.Contains("libsvtav1", StringComparison.Ordinal) &&
            !encoders.StandardOutput.Contains("libaom-av1", StringComparison.Ordinal))
        {
            Assert.Inconclusive("An SVT-AV1 or libaom AV1 encoder is required for this integration test.");
        }

        var directory = CreateTestDirectory();
        var input = Path.Combine(directory, "av1-input.mkv");
        var output = Path.Combine(directory, "av1-output.mp4");

        try
        {
            await RunProcessAsync(ffmpeg, [
                "-hide_banner", "-loglevel", "error", "-y",
                "-f", "lavfi", "-i", "testsrc2=duration=2:size=320x240:rate=24",
                "-c:v", "ffv1",
                input
            ]);

            var service = new EncodeService(new BinaryLocatorService());
            var result = await service.ResizeVideoToTargetBytesAsync(
                FilePath.From(input),
                FilePath.From(output),
                100_000,
                25,
                2,
                5,
                includeAudio: false);

            Assert.IsLessThanOrEqualTo(100_000, result.FileSizeBytes);
            Assert.AreEqual("av1", await GetVideoCodecAsync(ffprobe, output));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task IncompatibleMp4AudioFailsInsteadOfTranscoding()
    {
        var ffmpeg = FindExecutable("ffmpeg");
        if (ffmpeg is null || FindExecutable("ffprobe") is null)
        {
            Assert.Inconclusive("FFmpeg and FFprobe are required for integration tests.");
        }

        var directory = CreateTestDirectory();
        var input = Path.Combine(directory, "wavpack-input.mkv");
        var output = Path.Combine(directory, "wavpack-output.mp4");

        try
        {
            await RunProcessAsync(ffmpeg, [
                "-hide_banner", "-loglevel", "error", "-y",
                "-f", "lavfi", "-i", "testsrc2=duration=3:size=640x360:rate=30",
                "-f", "lavfi", "-i", "sine=frequency=440:duration=3:sample_rate=48000",
                "-map", "0:v:0", "-map", "1:a:0",
                "-c:v", "ffv1", "-c:a", "wavpack",
                input
            ]);

            Assert.IsGreaterThan(1_000_000, new FileInfo(input).Length);

            var service = new EncodeService(new BinaryLocatorService());

            await Assert.ThrowsAsync<IncompatibleAudioStreamsException>(
                () => service.ResizeVideoToTargetAsync(
                    FilePath.From(input),
                    FilePath.From(output),
                    1,
                    5,
                    3,
                    1));
            Assert.IsFalse(File.Exists(output));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTestDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"squash-integration-{Guid.NewGuid():N}");

        Directory.CreateDirectory(directory);

        return directory;
    }

    private static string? FindExecutable(string name)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];

        return paths.Select(path => Path.Combine(path, $"{name}.exe")).FirstOrDefault(File.Exists);
    }

    private static async Task<int> CountAudioStreamsAsync(string ffprobe, string path)
    {
        var output = await RunProcessAsync(
            ffprobe,
            ["-v", "error", "-select_streams", "a", "-show_entries", "stream=index", "-of", "csv=p=0", path]);

        return output.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static async Task<string> GetVideoCodecAsync(string ffprobe, string path)
    {
        var output = await RunProcessAsync(
            ffprobe,
            ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=codec_name", "-of", "csv=p=0", path]);

        return output.StandardOutput.Trim();
    }

    private static async Task<string> GetDecodedAudioHashAsync(string ffmpeg, string path, int audioIndex)
    {
        var output = await RunProcessAsync(ffmpeg, [
            "-hide_banner", "-loglevel", "error",
            "-i", path,
            "-map", $"0:a:{audioIndex}",
            "-c:a", "pcm_s16le",
            "-f", "hash", "-hash", "sha256", "pipe:1"
        ]);

        return output.StandardOutput.Trim();
    }

    private static async Task<ProcessOutput> RunProcessAsync(string executable, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {executable}.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = new ProcessOutput(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
        if (output.ExitCode != 0)
        {
            Assert.Fail($"{Path.GetFileName(executable)} failed with exit code {output.ExitCode}: {output.StandardError}");
        }

        return output;
    }

    private sealed record ProcessOutput(int ExitCode, string StandardOutput, string StandardError);
}
