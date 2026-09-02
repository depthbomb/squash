namespace Squash.Services;

public class MissingBinariesTaskDialogService
{
    private const string DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z";
    private const string ChecksumUrl = $"{DownloadUrl}.sha256";

    private readonly DownloadService _downloader;
    private readonly ExtractService  _extractor;
    private readonly BinaryLocatorService _binaryLocator;

    public MissingBinariesTaskDialogService(DownloadService downloader,
                                            ExtractService extractor,
                                            BinaryLocatorService binaryLocator)
    {
        _downloader = downloader;
        _extractor = extractor;
        _binaryLocator = binaryLocator;
    }

    public async Task<TaskDialogButton> ShowDialogAsync(IWin32Window owner)
    {
        using var cts = new CancellationTokenSource();
        Task? operationTask = null;

        #region Control
        var yesButton    = new TaskDialogCommandLinkButton("Yes", "Download the required binaries for me", allowCloseDialog: false);
        var noButton     = new TaskDialogCommandLinkButton("No", "Close Squash");
        var cancelButton = TaskDialogButton.Cancel;

        var downloadProgressBar = new TaskDialogProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            State   = TaskDialogProgressBarState.Marquee
        };
        #endregion

        #region Page
        var initialPage = new TaskDialogPage
        {
            Caption = "Squash",
            Heading = "Missing required binaries",
            Text    = "You are missing one or more required binaries.\nWould you like to let Squash download these for you?",
            Icon    = TaskDialogIcon.Error,
            Expander = new TaskDialogExpander
            {
                Text = "Squash uses the latest checksum-verified FFmpeg Essentials build for video encoding and probing. Without FFmpeg and FFprobe, Squash cannot function.",
            },
            Buttons =
            {
                yesButton,
                noButton
            },
            DefaultButton = yesButton,
        };
        var downloadPage = new TaskDialogPage
        {
            Caption     = "Squash",
            Heading     = "Downloading required binaries",
            Text        = "Starting download...",
            Icon        = TaskDialogIcon.Information,
            ProgressBar = downloadProgressBar,
            Buttons =
            {
                cancelButton
            }
        };
        var successPage = new TaskDialogPage
        {
            Caption = "Squash",
            Heading = "Success",
            Text    = "Required binaries have been successfully downloaded!",
            Icon    = TaskDialogIcon.ShieldSuccessGreenBar,
            Buttons =
            {
                TaskDialogButton.OK
            }
        };
        var failurePage = new TaskDialogPage
        {
            Caption = "Squash",
            Heading = "Download failed",
            Icon = TaskDialogIcon.Error,
            Buttons =
            {
                TaskDialogButton.Close
            }
        };
        #endregion

        #region Event subscribers
        yesButton.Click += (_, _) => initialPage.Navigate(downloadPage);
        cancelButton.Click += (_, _) => cts.Cancel();

        downloadPage.Created += (_, _) => operationTask = DownloadAndExtractAsync();
        #endregion

        var result = await TaskDialog.ShowDialogAsync(owner, initialPage);
        cts.Cancel();

        if (operationTask is not null)
        {
            try
            {
                await operationTask;
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // Cancellation is represented by the dialog result.
            }
        }

        return result;

        async Task DownloadAndExtractAsync()
        {
            var temp = FilePath.TempFile();
            var checksumFile = FilePath.TempFile();

            void OnProgressChanged(object? sender, int progress)
            {
                if (progress is < 0 or >= 100)
                {
                    downloadProgressBar.State = TaskDialogProgressBarState.Marquee;
                }
                else
                {
                    downloadProgressBar.State = TaskDialogProgressBarState.Normal;
                    downloadProgressBar.Value = progress;

                    downloadPage.Text = $"Downloading... ({progress}%)";
                }
            }

            _downloader.ProgressChanged += OnProgressChanged;

            try
            {
                downloadPage.Text = "Retrieving checksum...";

                await _downloader.DownloadFileAsync(ChecksumUrl, checksumFile, cts.Token);

                var expectedHash = (await File.ReadAllTextAsync(checksumFile.FullPath, cts.Token)).Trim();
                if (expectedHash.Length != 64 || !expectedHash.All(Uri.IsHexDigit))
                {
                    throw new InvalidDataException("The FFmpeg checksum response was invalid.");
                }

                downloadPage.Text = "Downloading FFmpeg...";

                await _downloader.DownloadFileAsync(DownloadUrl, temp, cts.Token);

                downloadPage.Text = "Verifying download...";

                await DownloadService.VerifySha256Async(temp, expectedHash, cts.Token);

                downloadPage.Text = "Extracting...";

                await _extractor.ExtractFilesFromArchiveAsync(
                    temp,
                    FilePath.From(AppDomain.CurrentDomain.BaseDirectory),
                    ["ffmpeg.exe", "ffprobe.exe"],
                    cts.Token
                );

                _binaryLocator.Invalidate("ffmpeg", "ffprobe");

                downloadPage.Navigate(successPage);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failurePage.Text = ex.Message;
                downloadPage.Navigate(failurePage);
            }
            finally
            {
                _downloader.ProgressChanged -= OnProgressChanged;
                temp.Unlink(true);
                checksumFile.Unlink(true);
            }
        }
    }
}
