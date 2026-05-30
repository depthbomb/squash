using Caprine.FilePath;
using Squash.Core.Exceptions;
using System.Diagnostics;

namespace Squash.Core.Services;

public class ExtractService(BinaryLocatorService binaryLocator)
{
    public async Task ExtractFilesFromArchiveAsync(FilePath          archivePath,
                                                   FilePath          destinationPath,
                                                   string[]          fileNames,
                                                   CancellationToken ct = default)
    {
        var extractorBinary = await binaryLocator.GetBinaryPathAsync("7za").ConfigureAwait(false);

        MissingSevenZipBinaryException.ThrowIf(extractorBinary is null);

        var psi = new ProcessStartInfo(extractorBinary.FullPath)
        {
            UseShellExecute = false,
            CreateNoWindow  = true,
        };
        psi.ArgumentList.Add("e");
        psi.ArgumentList.Add(archivePath.FullPath);
        psi.ArgumentList.Add("-r");
        psi.ArgumentList.Add($"-o{destinationPath.FullPath}");
        psi.ArgumentList.Add("-aoa");

        foreach (var fileName in fileNames)
        {
            psi.ArgumentList.Add(fileName);
        }

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start 7za.");

        try
        {
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException($"7za failed with exit code {proc.ExitCode}.");
            }
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
}
