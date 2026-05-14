using Squash.Lib;
using Squash.Services;

namespace Squash.Forms;

public partial class MainForm : Form
{
    private CancellationTokenSource? _cts;
    
    private const string MainButtonInitialText       = "&Squash it!";
    private const string MainButtonWorkingText       = "&Cancel";
    private const string StatusStripLabelInitialText = "Waiting";
    
    private readonly BinaryLocatorService             _binaryLocator;
    private readonly EncodeService                    _encoder;
    private readonly MissingBinariesTaskDialogService _missingBinariesTaskDialog;

    private readonly Flag _workingFlag = new(false);

    public MainForm(BinaryLocatorService             binaryLocator,
                    EncodeService                    encoder,
                    MissingBinariesTaskDialogService missingBinariesTaskDialog)
    {
        _binaryLocator             = binaryLocator;
        _encoder                   = encoder;
        _missingBinariesTaskDialog = missingBinariesTaskDialog;

        InitializeComponent();

        c_QualityPresetComboBox.SelectedIndex = 1;

        #region Control Events
        Shown                           += OnShown;
        c_InputFileTextBox.TextChanged  += TextBoxesOnTextChanged;
        c_OutputFileTextBox.TextChanged += TextBoxesOnTextChanged;
        c_MainButton.Click              += C_MainButtonOnClick;
        #endregion
    }

    #region Privates
    private bool IsVideoFile(string path) => Path.GetExtension(path).ToLowerInvariant() is
        ".mp4" or
        ".mkv" or
        ".avi" or
        ".mov" or
        ".wmv" or
        ".webm" or
        ".flv" or
        ".mpeg" or
        ".mpg";
    
    private void UpdateMainButtonState()
    {
        c_MainButton.Enabled =
            !string.IsNullOrWhiteSpace(c_InputFileTextBox.Text) &&
            !string.IsNullOrWhiteSpace(c_OutputFileTextBox.Text);
    }

    private void SetControlsEnabledState(bool enabled)
    {
        Controls.OfType<Control>()
                .Where(c => c.Tag as string == "toggleable")
                .ToList()
                .ForEach(c => c.Enabled = enabled);
    }
    
    private void ResetControls()
    {
        c_MainButton.Text = MainButtonInitialText;
        
        c_StatusStripProgressBar.Value = 0;
        c_StatusStripLabel.Text        = StatusStripLabelInitialText;
    }
    #endregion
    
    #region Control Event Subscribers
    private async void OnShown(object? sender, EventArgs e)
    {
        var results = await Task.WhenAll(new[]
        {
            _binaryLocator.HasBinaryAsync("ffmpeg"),
            _binaryLocator.HasBinaryAsync("ffprobe")
        });
        var allTrue = results.All(r => r);
        if (!allTrue)
        {
            var res = await _missingBinariesTaskDialog.ShowDialogAsync(this);
            if (res.Text == "No" || res == TaskDialogButton.Cancel)
            {
                Environment.Exit(0);
            }
        }
    }
    
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
        {
            e.Effect = DragDropEffects.None;
            return;
        }
        
        var valid = files.Length == 1 && IsVideoFile(files[0]);
        
        e.Effect = valid ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragDrop(object sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return;
        }

        if (files.Length != 1)
        {
            return;
        }

        var file = files[0];
        if (!IsVideoFile(file))
        {
            return;
        }
        
        c_InputFileTextBox.Text = file;
    }
    
    private void TextBoxesOnTextChanged(object? sender, EventArgs e)
    {
        if (sender == c_InputFileTextBox)
        {
            var input = c_InputFileTextBox.Text;
            if (!string.IsNullOrWhiteSpace(input))
            {
                var outputPath = FilePath.From(input);
                var newPath    = outputPath.WithName($"{outputPath.Stem}-squashed.mp4");

                c_OutputFileTextBox.Text = newPath.FullPath;
            }
        }

        UpdateMainButtonState();
    }
    
    private async void C_MainButtonOnClick(object? sender, EventArgs e)
    {
        if (_workingFlag.IsTrue())
        {
            _cts?.Cancel();
        }
        else
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            
            c_MainButton.Text = MainButtonWorkingText;
            
            try
            {
                var res = await _encoder.ResizeVideoToTargetAsync(
                    FilePath.From(c_InputFileTextBox.Text),
                    FilePath.From(c_OutputFileTextBox.Text),
                    Convert.ToInt32(c_TargetSizeInput.Value),
                    Convert.ToDouble(c_ToleranceInput.Value),
                    Convert.ToInt32(c_MaxIterationsInput.Value),
                    c_QualityPresetComboBox.SelectedIndex + 1,
                    p =>
                    {
                        c_StatusStripLabel.Text        = p.TitleText;
                        c_StatusStripProgressBar.Value = p.ProgressPercent;
                    },
                    _cts.Token
                );

                MessageBox.Show(this, $"Final file size: {res.FileSizeBytes}", "Operation complete");
            }
            catch (OperationCanceledException) {}
            finally
            {
                SetControlsEnabledState(true);
                UpdateMainButtonState();
                ResetControls();
                
                _workingFlag.Reset();
            }
        }
    }
    #endregion
}
