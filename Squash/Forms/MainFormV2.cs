namespace Squash.Forms;

public partial class MainFormV2 : Form
{
    private const string DisclaimerKey = "disclaimer-v1";

    private readonly PersistentStateService           _persistentState;
    private readonly BinaryLocatorService             _binaryLocator;
    private readonly FirstRunTaskDialogService        _firstRunTaskDialog;
    private readonly MissingBinariesTaskDialogService _missingBinariesTaskDialog;

    public MainFormV2(PersistentStateService           persistentState,
                      BinaryLocatorService             binaryLocator,
                      EncodingQueuePanel               encodingQueuePanel,
                      AboutPanel                       aboutPanel,
                      FirstRunTaskDialogService        firstRunTaskDialog,
                      MissingBinariesTaskDialogService missingBinariesTaskDialog)
    {
        _persistentState           = persistentState;
        _binaryLocator             = binaryLocator;
        _firstRunTaskDialog        = firstRunTaskDialog;
        _missingBinariesTaskDialog = missingBinariesTaskDialog;

        InitializeComponent();

        c_NavigationView.AddPage(
            "Encode queue",
            encodingQueuePanel,
            Resources.Images.film_add);
        c_NavigationView.AddPage(
            "Settings",
            new Label
            {
                Text = "Coming Soon",
                TextAlign = ContentAlignment.MiddleCenter,
            },
            Resources.Images.cog);
        c_NavigationView.AddPage(
            "About Squash",
            aboutPanel,
            Resources.Images.help);

        Shown       += OnShown;
        FormClosing += OnFormClosing;
    }

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

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        //
    }
}

