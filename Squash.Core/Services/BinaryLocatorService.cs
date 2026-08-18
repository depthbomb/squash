using Caprine.FilePath;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Squash.Core.Services;

public class BinaryLocatorService
{
    private readonly ConcurrentDictionary<string, Lazy<Task<FilePath?>>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<FilePath?> GetBinaryPathAsync(string name)
    {
        var lookup = _cache.GetOrAdd(
            name,
            static binaryName => new Lazy<Task<FilePath?>>(
                () => FindBinaryPathAsync(binaryName),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var path = await lookup.Value.ConfigureAwait(false);
            if (path is null)
            {
                _cache.TryRemove(name, out _);
            }

            return path;
        }
        catch
        {
            _cache.TryRemove(name, out _);
            throw;
        }
    }

    public void Invalidate(params ReadOnlySpan<string> names)
    {
        foreach (var name in names)
        {
            _cache.TryRemove(name, out _);
        }
    }

    private static async Task<FilePath?> FindBinaryPathAsync(string name)
    {
        // 1.) check for binary alongside the assembly
        var localPath = FilePath.From(AppDomain.CurrentDomain.BaseDirectory) / $"{name}.exe";
        if (localPath is { Exists: true, IsFile: true })
        {
            return localPath;
        }

        // 2.) check in PATH
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "where",
                Arguments              = name,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            using var proc = Process.Start(psi);
            if (proc is null)
            {
                return null;
            }

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();

            await proc.WaitForExitAsync().ConfigureAwait(false);

            var stdout = await stdoutTask.ConfigureAwait(false);

            if (proc.ExitCode != 0)
            {
                return null;
            }

            var firstMatch = stdout
                             .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                             .FirstOrDefault();

            return firstMatch is null ? null : FilePath.From(firstMatch.Trim());
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> HasBinaryAsync(string name) => await GetBinaryPathAsync(name).ConfigureAwait(false) != null;
}
