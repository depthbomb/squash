namespace Squash.Controls;

public partial class EncodingQueuePanel : UserControl
{
    private const int ThumbnailMarginSize = 4;

    private CancellationTokenSource? _cts;

    private bool HasInputFile  => !c_InputFileTextBox.Text.IsNullOrWhiteSpace();
    private bool HasOutputFile => !c_OutputFileTextBox.Text.IsNullOrWhiteSpace();

    private const string MainButtonInitialText = "&Squash it!";
    private const string MainButtonWorkingText = "&Cancel";

    private readonly ThumbnailService _thumbnail;
    private readonly EncodeService    _encoder;
    private readonly Win32Service     _win32;

    private readonly Flag _workingFlag = new(false);

    public EncodingQueuePanel(ThumbnailService thumbnail, EncodeService encoder, Win32Service  win32)
    {
        _thumbnail = thumbnail;
        _encoder   = encoder;
        _win32     = win32;

        InitializeComponent();

        c_QualityPresetComboBox.SelectedIndex = 1;

        #region Control Events
        DragEnter += OnDragEnter;
        DragDrop  += OnDragDrop;

        c_ThumbnailTablePanel.Resize += C_ThumbnailTablePanelOnResize;

        c_InputFileTextBox.TextChanged  += TextBoxesOnTextChanged;
        c_OutputFileTextBox.TextChanged += TextBoxesOnTextChanged;
        c_MainButton.Click              += C_MainButtonOnClick;
        c_InputFileBrowseButton.Click   += C_InputFileBrowseButtonOnClick;
        c_OutputFileBrowseButton.Click  += C_OutputFileBrowseButtonOnClick;
        #endregion

        UpdateOutputFileBrowseButtonState();
        UpdateMainButtonState();
        UpdatePictureBoxLayout();
    }

    #region Control Event Handlers
    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        var valid = files.Length == 1 && IsVideoFile(files[0]);

        e.Effect = valid ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files && IsVideoFile(files[0]))
        {
            c_InputFileTextBox.Text = files[0];

            var image = await _thumbnail.GetVideoThumbnailAsync(
                FilePath.From(c_InputFileTextBox.Text),
                FilePath.TempDir());

            c_ThumbnailPictureBox.Image = Image.FromFile(image.FullPath);
        }
    }

    private void C_ThumbnailTablePanelOnResize(object? sender, EventArgs e)
    {
        UpdatePictureBoxLayout();
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

        _win32.PreventSleep();
        // _win32.SetTaskbarIndeterminate(this);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        c_MainButton.Text = MainButtonWorkingText;

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
                    // Text = p.TitleText;

                    // if (p.ProgressPercent >= 100)
                    // {
                    //     _win32.SetTaskbarIndeterminate(this);
                    // }
                    // else
                    // {
                    //     _win32.SetTaskbarProgress(this, p.ProgressPercent, 100);
                    // }
                },
                _cts.Token
            );

            // _win32.FlashUntilFocused(this);

            if (res.Success)
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
        }
        catch (OperationCanceledException)
        {
            /*Ignored*/
        }
        catch (Exception ex) when (ex is UnableToReachTargetSizeException or VideoSizeBelowTargetSizeException)
        {
            MessageBox.Show(ex.Message, "Operation complete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetControlsEnabledState(true);
            UpdateMainButtonState();
            ResetControls();

            _workingFlag.Reset();

            _win32.AllowSleep();
            // _win32.ClearTaskbarProgress(this);
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
        SetControlsEnabledStateRecursive(this, enabled);
    }

    private void SetControlsEnabledStateRecursive(Control parent, bool enabled)
    {
        foreach (Control control in parent.Controls)
        {
            if (control.Tag as string == "toggleable")
            {
                control.Enabled = enabled;
            }

            if (control.HasChildren)
            {
                SetControlsEnabledStateRecursive(control, enabled);
            }
        }
    }

    private void ResetControls()
    {
        c_MainButton.Text = MainButtonInitialText;
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

    private void UpdatePictureBoxLayout()
    {
        Control parent = c_ThumbnailTablePanel;

        int availableWidth  = parent.ClientSize.Width  - (ThumbnailMarginSize * 2);
        int availableHeight = parent.ClientSize.Height - (ThumbnailMarginSize * 2);

        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        const double aspect = 16.0 / 9.0;

        int width  = availableWidth;
        int height = (int)(width / aspect);

        if (height > availableHeight)
        {
            height = availableHeight;
            width  = (int)(height * aspect);
        }

        c_ThumbnailPictureBox.Size = new Size(width, height);

        c_ThumbnailPictureBox.Location = new Point(
            (parent.ClientSize.Width  - width)  / 2,
            (parent.ClientSize.Height - height) / 2);
    }
    #endregion
}
