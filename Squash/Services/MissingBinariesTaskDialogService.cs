using Caprine.FilePath;

namespace Squash.Services;

public class MissingBinariesTaskDialogService
{
    private const string DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z";

    private readonly DownloadService _downloader;
    private readonly ExtractService  _extractor;

    public MissingBinariesTaskDialogService(DownloadService downloader,
                                            ExtractService  extractor)
    {
        _downloader = downloader;
        _extractor  = extractor;
    }

    public async Task<TaskDialogButton> ShowDialogAsync(IWin32Window owner)
    {
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
                Text = "Squash uses the third-party tools FFmpeg and FFprobe to work with video files. Without these, Squash cannot function.",
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
        #endregion

        #region Event subscribers
        yesButton.Click += (_, _) => initialPage.Navigate(downloadPage);

        downloadPage.Created += async (_, _) =>
        {
            var temp = FilePath.TempFile();

            _downloader.ProgressChanged += (_, progress) =>
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
            };

            await _downloader.DownloadFileAsync(DownloadUrl, temp);

            downloadPage.Text = "Extracting...";

            await _extractor.ExtractFilesFromArchiveAsync(
                temp,
                FilePath.From(AppDomain.CurrentDomain.BaseDirectory),
                ["ffmpeg.exe", "ffprobe.exe"]
            );

            temp.Unlink(true);

            downloadPage.Navigate(successPage);
        };
        #endregion

        return await TaskDialog.ShowDialogAsync(owner, initialPage);
    }
}
