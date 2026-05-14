using Squash.Lib;
using Squash.Services;

namespace Squash.Forms;

public partial class MainForm : Form
{
    private bool HasInputFile  => !string.IsNullOrWhiteSpace(c_InputFileTextBox.Text);
    private bool HasOutputFile => !string.IsNullOrWhiteSpace(c_OutputFileTextBox.Text);
    
    private CancellationTokenSource? _cts;
    
    private const string MainButtonInitialText       = "&Squash it!";
    private const string MainButtonWorkingText       = "&Cancel";
    private const string StatusStripLabelInitialText = "Waiting";
    private const string StatusStripLabelWorkingText = "Processing...";
    
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
        FormClosing                     += OnFormClosing;
        c_InputFileTextBox.TextChanged  += TextBoxesOnTextChanged;
        c_OutputFileTextBox.TextChanged += TextBoxesOnTextChanged;
        c_MainButton.Click              += C_MainButtonOnClick;
        c_InputFileBrowseButton.Click   += C_InputFileBrowseButtonOnClick;
        c_OutputFileBrowseButton.Click  += C_OutputFileBrowseButtonOnClick;
        #endregion
        
        UpdateOutputFileBrowseButtonState();
        UpdateMainButtonState();
    }

    #region Control Event Handlers
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

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
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
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files && IsVideoFile(files[0]))
        {
            c_InputFileTextBox.Text = files[0];
        }
    }

    private void C_InputFileBrowseButtonOnClick(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        
        ofd.Title  = "Select video file to squash";
        ofd.Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.flv;*.mpeg;*.mpg|All Files|*.*";

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            c_InputFileTextBox.Text = ofd.FileName;
        }
    }

    private void C_OutputFileBrowseButtonOnClick(object? sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog();
        
        sfd.Title    = "Select output location";
        sfd.Filter   = "MP4 Video|*.mp4";
        sfd.FileName = Path.GetFileName(c_OutputFileTextBox.Text);

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            c_OutputFileTextBox.Text = sfd.FileName;
        }
    }

    private void TextBoxesOnTextChanged(object? sender, EventArgs e)
    {
        if (sender == c_InputFileTextBox && HasInputFile)
        {
            var inputPath = FilePath.From(c_InputFileTextBox.Text);
            var newName   = GetUniqueSquashedName(inputPath);
            var newPath   = inputPath.WithName(newName);

            c_OutputFileTextBox.Text = newPath.FullPath;
        }

        UpdateOutputFileBrowseButtonState();
        UpdateMainButtonState();
    }

    private async void C_MainButtonOnClick(object? sender, EventArgs e)
    {
        if (_workingFlag.IsTrue())
        {
            _cts?.Cancel();
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        c_MainButton.Text       = MainButtonWorkingText;
        c_StatusStripLabel.Text = StatusStripLabelWorkingText;

        try
        {
            _workingFlag.SetTrue();
            
            SetControlsEnabledState(false);

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
        catch (OperationCanceledException) { }
        finally
        {
            SetControlsEnabledState(true);
            UpdateMainButtonState();
            ResetControls();
            
            _workingFlag.Reset();
        }
    }
    #endregion

    #region Private
    private void UpdateMainButtonState()
    {
        c_MainButton.Enabled = HasInputFile && HasOutputFile;
    }

    private void UpdateOutputFileBrowseButtonState()
    {
        c_OutputFileBrowseButton.Enabled = HasInputFile;
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
        c_MainButton.Text              = MainButtonInitialText;
        c_StatusStripLabel.Text        = StatusStripLabelInitialText;
        c_StatusStripProgressBar.Value = 0;
    }

    private bool IsVideoFile(string path)
        => Path.GetExtension(path).ToLowerInvariant() is
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".webm" or ".flv" or ".mpeg" or ".mpg";

    private string GetUniqueSquashedName(FilePath inputFile)
    {
        var stem      = inputFile.Stem;
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var suffix    = $"-sqsh-{timestamp}.mp4";

        var directory     = inputFile.Parent.FullPath;
        var maxStemLength = 250 - directory.Length - 1 - suffix.Length;

        if (maxStemLength < 5)
        {
            maxStemLength = 5;
        }

        var safeStem = stem.Length > maxStemLength ? stem[..maxStemLength] : stem;
        
        return $"{safeStem}{suffix}";
    }
    #endregion
}
