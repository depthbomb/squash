using Caprine.FilePath;
using System.Text.Json;
using System.Globalization;
using Squash.Core.Services;

namespace Squash.Tools;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args is not ["encode", _, _, _, _, ..])
        {
            Console.Error.WriteLine(
                "Usage: Squash.Tools encode <input> <output> <target-bytes> <preset> [tolerance-percent] [max-iterations] [include-audio]");

            return 2;
        }

        if (!long.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var targetBytes) ||
            !int.TryParse(args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var preset))
        {
            Console.Error.WriteLine("Target bytes and preset must be integers.");

            return 2;
        }

        var tolerance = args.Length > 5
            ? double.Parse(args[5], NumberStyles.Float, CultureInfo.InvariantCulture)
            : 0.25;
        var maxIterations = args.Length > 6
            ? int.Parse(args[6], NumberStyles.Integer, CultureInfo.InvariantCulture)
            : 8;
        var includeAudio = args.Length <= 7 || bool.Parse(args[7]);
        var service = new EncodeService(new BinaryLocatorService());

        service.Progress += (_, progress) =>
            Console.Error.WriteLine($"[{progress.CurrentIteration}/{progress.MaxIterations}] {progress.ProgressPercent}% {progress.ProgressStatus}");

        try
        {
            var result = await service.ResizeVideoToTargetBytesAsync(
                FilePath.From(args[1]),
                FilePath.From(args[2]),
                targetBytes,
                tolerance,
                maxIterations,
                preset,
                includeAudio);

            var output = new
            {
                result.Success,
                FilePath = result.FilePath.FullPath,
                result.FileSizeBytes,
                result.TargetSizeBytes,
                result.Iteration,
                result.VideoBitrateKbps,
                result.ElapsedSeconds
            };

            Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));

            return result.FileSizeBytes <= result.TargetSizeBytes ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);

            return 1;
        }
    }
}
