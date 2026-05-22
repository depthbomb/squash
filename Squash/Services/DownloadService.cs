namespace Squash.Services;

public class DownloadService
{
    public event EventHandler<int>? ProgressChanged;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProgress<int>     _progress;

    public DownloadService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _progress          = new Progress<int>(p => ProgressChanged?.Invoke(this, p));
    }

    public async Task DownloadFileAsync(string url, FilePath destinationPath, CancellationToken ct = default)
    {
        using var http = _httpClientFactory.CreateClient("Default");
        using var res  = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        res.EnsureSuccessStatusCode();

        var totalBytes = res.Content.Headers.ContentLength;

        await using var cs = await res.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var fs = destinationPath.Open(FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer    = new byte[81920];
        var totalRead = 0L;

        int bytesRead;
        while ((bytesRead = await cs.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);

            totalRead += bytesRead;

            if (totalBytes is > 0)
            {
                var percent = (int)(totalRead * 100 / totalBytes.Value);
                _progress.Report(Math.Min(100, percent));
            }
            else
            {
                _progress.Report(-1);
            }
        }

        _progress.Report(100);
    }
}
