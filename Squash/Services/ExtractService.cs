using Squash.Lib;
using Squash.Exceptions;
using System.Diagnostics;

namespace Squash.Services;

public class ExtractService
{
    private readonly BinaryLocatorService _binaryLocator;

    public ExtractService(BinaryLocatorService binaryLocator)
    {
        _binaryLocator = binaryLocator;
    }

    public async Task ExtractFilesFromArchiveAsync(FilePath archivePath,
                                                   FilePath destinationPath,
                                                   string[] fileNames,
                                                   CancellationToken ct = default
    )
    {
        var extractorBinary = await _binaryLocator.GetBinaryPathAsync("7za");
        
        MissingSevenZipBinaryException.ThrowIf(extractorBinary is null);

        var psi = new ProcessStartInfo
        {
            FileName        = extractorBinary.FullPath,
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
        
        await proc.WaitForExitAsync(ct);
    }
}
