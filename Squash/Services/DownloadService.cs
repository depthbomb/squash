using Squash.Lib;

namespace Squash.Services;

public class DownloadService : IDisposable
{
    public event EventHandler<int>? ProgressChanged;
    
    private readonly HttpClient     _http;
    private readonly IProgress<int> _progress;

    public DownloadService(IHttpClientFactory httpClientFactory)
    {
        _http     = httpClientFactory.CreateClient("Default");
        _progress = new Progress<int>(p => ProgressChanged?.Invoke(this, p));
    }

    #region IDisposable
    public void Dispose()
    {
        _http.Dispose();
    }
    #endregion
    
    public async Task DownloadFileAsync(string url, FilePath destinationPath, CancellationToken ct = default)
    {
        using (var res = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            res.EnsureSuccessStatusCode();
            
            var totalBytes = res.Content.Headers.ContentLength;
            
            await using (var cs = await res.Content.ReadAsStreamAsync(ct))
            await using (var fs = destinationPath.Open(FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer    = new byte[81920];
                var totalRead = 0;

                int bytesRead;

                while ((bytesRead = await cs.ReadAsync(buffer, ct)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    
                    totalRead += bytesRead;

                    if (totalBytes is > 0)
                    {
                        var percent = (int)(totalRead * 100L / totalBytes.Value);
                        if (percent > 100)
                        {
                            percent = 100;
                        }
                        
                        _progress.Report(percent);
                    }
                    else
                    {
                        _progress.Report(-1);
                    }
                }
                
                _progress.Report(100);
            }
        }
    }
}
