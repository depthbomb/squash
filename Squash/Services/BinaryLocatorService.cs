using Squash.Lib;
using System.Diagnostics;

namespace Squash.Services;

public class BinaryLocatorService
{
    public async Task<FilePath?> GetBinaryPathAsync(string name)
    {
        // 1.) check for binary alongside the assembly
        var localPath = FilePath.From(AppDomain.CurrentDomain.BaseDirectory) / $"{name}.exe";
        if (localPath.Exists() && localPath.IsFile())
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
                
            await proc.WaitForExitAsync();

            var stdout = await stdoutTask;

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
    
    public async Task<bool> HasBinaryAsync(string name) => await GetBinaryPathAsync(name) != null;
}
