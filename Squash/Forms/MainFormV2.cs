using Squash.Interop;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Squash.Forms;

public partial class MainFormV2 : Form
{
    private const string DisclaimerKey     = "disclaimer-v2";
    private const string InitialStatusText = "Waiting";
    private const string NotificationGroup = "squash.encoding";

    private string? _notificationTag;
    private int     _notificationSequence = 1;

    private readonly PersistentStateService           _persistentState;
    private readonly EncodeService                    _encoder;
    private readonly BinaryLocatorService             _binaryLocator;
    private readonly FirstRunTaskDialogService        _firstRunTaskDialog;
    private readonly MissingBinariesTaskDialogService _missingBinariesTaskDialog;

    public MainFormV2(PersistentStateService           persistentState,
                      EncodeService                    encoder,
                      BinaryLocatorService             binaryLocator,
                      EncodingQueuePanel               encodingQueuePanel,
                      SettingsPanel                    settingsPanel,
                      AboutPanel                       aboutPanel,
                      FirstRunTaskDialogService        firstRunTaskDialog,
                      MissingBinariesTaskDialogService missingBinariesTaskDialog)
    {
        _persistentState           = persistentState;
        _encoder                   = encoder;
        _binaryLocator             = binaryLocator;
        _firstRunTaskDialog        = firstRunTaskDialog;
        _missingBinariesTaskDialog = missingBinariesTaskDialog;

        InitializeComponent();

        c_NavigationView.AddPage("Encoding", encodingQueuePanel, Resources.Images.film_add);
        c_NavigationView.AddPage("Settings", settingsPanel, Resources.Images.cog);
        c_NavigationView.AddPage("About", aboutPanel, Resources.Images.help);

        #region Control Events
        Shown       += OnShown;
        FormClosing += OnFormClosing;
        #endregion

        #region Service Events
        _encoder.Started  += EncoderOnStarted;
        _encoder.Progress += EncoderOnProgress;
        _encoder.Finished += EncoderOnFinished;
        #endregion

        #region Events
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        #endregion

        AppNotificationManager.Default.Register();
    }

    #region Control Event Handlers
    private async void OnShown(object? sender, EventArgs e)
    {
        if (!_persistentState.HasCompleted(DisclaimerKey))
        {
            var firstRunRes = await _firstRunTaskDialog.ShowDialogAsync(this);
            if (firstRunRes == TaskDialogButton.Cancel)
            {
                Application.Exit();
                return;
            }

            await _persistentState.MarkCompletedAsync(DisclaimerKey);
        }

        var results = await Task.WhenAll(new[]
        {
            _binaryLocator.HasBinaryAsync("ffmpeg"),
            _binaryLocator.HasBinaryAsync("ffprobe")
        });

        var allTrue = results.All(r => r);
        if (!allTrue)
        {
            var missingBinariesRes = await _missingBinariesTaskDialog.ShowDialogAsync(this);
            if (missingBinariesRes.Text == "No" || missingBinariesRes == TaskDialogButton.Cancel)
            {
                Application.Exit();
            }
        }
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _encoder.CancelEncoding();

        await AppNotificationManager.Default.RemoveAllAsync();

        AppNotificationManager.Default.Unregister();
    }
    #endregion

    #region Service Event Handlers
    private void EncoderOnStarted(object? sender, EventArgs e)
    {
        Native.PreventSleep();
        Native.SetTaskbarIndeterminate(this);

        if (!Settings.Default.EnableNotifications)
            return;

        _notificationTag      = Guid.NewGuid().ToString("B");
        _notificationSequence = 1;

        var notification = new AppNotificationBuilder()
                           .AddText("Squashing in progress")
                           .SetGroup(NotificationGroup)
                           .SetTag(_notificationTag)
                           .AddProgressBar(new AppNotificationProgressBar()
                                           .BindTitle()
                                           .BindValue()
                                           .BindStatus())
                           .AddButton(new AppNotificationButton("Cancel")
                                      .SetButtonStyle(AppNotificationButtonStyle.Critical)
                                      .AddArgument("action", "cancel"))
                           .BuildNotification();

        notification.Progress = new AppNotificationProgressData((uint)++_notificationSequence)
        {
            Title  = "Encoding...",
            Status = "Starting...",
            Value  = 0.0
        };

        AppNotificationManager.Default.Show(notification);
    }

    private async void EncoderOnProgress(object? sender, ProgressEventArgs e)
    {
        c_StatusLabel.Text = e.ProgressStatus;

        if (e.ProgressPercent >= 100)
        {
            Native.SetTaskbarIndeterminate(this);
        }
        else
        {
            Native.SetTaskbarProgress(this, e.ProgressPercent, 100);

            if (!Settings.Default.EnableNotifications)
                return;

            var prog = new AppNotificationProgressData((uint)++_notificationSequence)
            {
                Title  = "Encoding...",
                Status = e.ProgressStatus,
                Value  = e.ProgressPercent / 100.0
            };

            await AppNotificationManager.Default.UpdateAsync(prog, _notificationTag, NotificationGroup);
        }
    }

    private async void EncoderOnFinished(object? sender, EncodeService.EncodeResult? res)
    {
        c_StatusLabel.Text = InitialStatusText;

        Native.AllowSleep();
        Native.ClearTaskbarProgress(this);

        if (res is null)
            return;

        Native.FlashUntilFocused(this);

        if (res.Success)
        {
            if (!Settings.Default.EnableNotifications)
            {
                MessageBox.Show(
                    this,
                    $"Successfully compressed video to {res.FileSizeBytes.ToFileSizeString()} in " +
                    $"{res.ElapsedSeconds.ToDurationString()} after {res.Iteration} iteration(s).",
                    "Operation complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                var notification = new AppNotificationBuilder()
                                   .AddText($"Successfully compressed video to {res.FileSizeBytes.ToFileSizeString()} in " +
                                            $"{res.ElapsedSeconds.ToDurationString()} after {res.Iteration} iteration(s).")
                                   .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
        }
        else
        {
            MessageBox.Show(
                this,
                $"Could not reach target size within tolerance after {res.Iteration} iteration(s). Saved best " +
                $"result of {res.FileSizeBytes.ToFileSizeString()} (target was "                                +
                $"{res.TargetSizeBytes.ToFileSizeString()}) in {res.ElapsedSeconds.ToDurationString()}.",
                "Operation complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        await AppNotificationManager.Default.RemoveByGroupAsync(NotificationGroup);
    }
    #endregion

    #region Event Handlers
    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        if (args.Argument == "action=cancel")
        {
            _encoder.CancelEncoding();
        }
        else
        {
            BringToFrontFromActivation();
        }
    }
    #endregion

    public void BringToFrontFromActivation()
    {
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;

        Show();
        TopMost = true;
        TopMost = false;
        Activate();
    }
}
