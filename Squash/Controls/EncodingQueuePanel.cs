using WinRT.Interop;
using Windows.System;
using Windows.Storage;
using Caprine.FilePath;
using Squash.Core.Services;
using Windows.Storage.Pickers;
using Microsoft.Windows.AppNotifications;

namespace Squash.Controls;

public partial class EncodingQueuePanel : UserControl
{
    private bool HasInputFile  => !c_InputFileTextBox.Text.IsNullOrWhiteSpace();
    private bool HasOutputFile => !c_OutputFileTextBox.Text.IsNullOrWhiteSpace();

    private const string MainButtonInitialText = "&Squash it!";
    private const string MainButtonWorkingText = "&Cancel";

    private readonly ThumbnailService _thumbnail;
    private readonly EncodeService    _encoder;
    private readonly Flag             _workingFlag = new(false);

    public EncodingQueuePanel(ThumbnailService thumbnail, EncodeService encoder)
    {
        _thumbnail = thumbnail;
        _encoder   = encoder;

        InitializeComponent();

        c_QualityPresetComboBox.Items.AddRange("1. Fast, decent quality", "2. Slow, better quality (recommended)");
        if (Settings.Default.EnableAdditionalQualityPresets)
        {
            c_QualityPresetComboBox.Items.AddRange("3. Very slow, better quality", "4. Absurdly slow, better quality");
        }
        c_QualityPresetComboBox.SelectedIndex = 1;

        #region Control Events
        DragEnter += OnDragEnter;
        DragDrop  += OnDragDrop;

        c_ThumbnailPictureBox.DoubleClick += C_ThumbnailPictureBoxOnDoubleClick;

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

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files && IsVideoFile(files[0]))
        {
            c_InputFileTextBox.Text = files[0];
        }
    }

    private async void C_ThumbnailPictureBoxOnDoubleClick(object? sender, EventArgs e)
    {
        if (HasInputFile)
        {
            var videoPath = await StorageFile.GetFileFromPathAsync(c_InputFileTextBox.Text);

            await Launcher.LaunchFileAsync(videoPath);
        }
    }

    private async void C_InputFileBrowseButtonOnClick(object? sender, EventArgs e)
    {
        var picker = new FileOpenPicker();

        InitializeWithWindow.Initialize(picker, Handle);

        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.ViewMode               = PickerViewMode.Thumbnail;
        picker.FileTypeFilter.Add(".mp4");
        picker.FileTypeFilter.Add(".mkv");
        picker.FileTypeFilter.Add(".avi");
        picker.FileTypeFilter.Add(".mov");
        picker.FileTypeFilter.Add(".wmv");
        picker.FileTypeFilter.Add(".webm");
        picker.FileTypeFilter.Add(".flv");
        picker.FileTypeFilter.Add(".mpeg");
        picker.FileTypeFilter.Add(".mpg");

        var res = await picker.PickSingleFileAsync();
        if (res is null)
        {
            return;
        }

        c_InputFileTextBox.Text = res.Path;
    }

    private async void C_OutputFileBrowseButtonOnClick(object? sender, EventArgs e)
    {
        var picker = new FileSavePicker();

        InitializeWithWindow.Initialize(picker, Handle);

        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.SuggestedFileName      = Path.GetFileName(c_OutputFileTextBox.Text);
        picker.FileTypeChoices.Add("MP4 Video", new List<string> { ".mp4" });

        var res = await picker.PickSaveFileAsync();
        if (res is null)
        {
            return;
        }

        c_OutputFileTextBox.Text = res.Path;
    }

    private async void TextBoxesOnTextChanged(object? sender, EventArgs e)
    {
        if (sender == c_InputFileTextBox && HasInputFile)
        {
            var inputPath = FilePath.From(c_InputFileTextBox.Text);
            var newName   = GetUniqueSquashedName(inputPath);
            var newPath   = inputPath.WithName(newName);

            c_OutputFileTextBox.Text = newPath.FullPath;

            if (c_ThumbnailPictureBox.Cursor == Cursors.No)
            {
                c_ThumbnailPictureBox.Cursor = Cursors.Hand;
            }

            await SetVideoSizeAsync();
            await SetThumbnailAsync();
        }

        UpdateOutputFileBrowseButtonState();
        UpdateMainButtonState();
    }

    private async void C_MainButtonOnClick(object? sender, EventArgs e)
    {
        if (_workingFlag.IsTrue())
        {
            _encoder.CancelEncoding();
            return;
        }

        c_MainButton.Text = MainButtonWorkingText;

        try
        {
            _workingFlag.SetTrue();

            SetControlsEnabledState(false);

            _ = await _encoder.ResizeVideoToTargetAsync(
                FilePath.From(c_InputFileTextBox.Text),
                FilePath.From(c_OutputFileTextBox.Text),
                Convert.ToInt32(c_TargetSizeInput.Value),
                Convert.ToDouble(c_ToleranceInput.Value),
                Convert.ToInt32(c_MaxIterationsInput.Value),
                c_QualityPresetComboBox.SelectedIndex + 1);
        }
        catch (OperationCanceledException)
        {
            /*Ignored*/
        }
        catch (Exception ex) when (ex is UnableToReachTargetSizeException or VideoSizeBelowTargetSizeException)
        {
            MessageBox.Show(ex.Message, "Operation complete", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            await AppNotificationManager.Default.RemoveAllAsync();
        }
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
        var stem          = inputFile.Stem;
        var timestamp     = DateTime.Now.ToString("yyyyMMddHHmmss");
        var suffix        = $"-sqsh-{timestamp}.mp4";
        var directory     = inputFile.Parent.FullPath;
        var maxStemLength = 250 - directory.Length - 1 - suffix.Length;
        if (maxStemLength < 5)
        {
            maxStemLength = 5;
        }

        var safeStem = stem.Length > maxStemLength ? stem[..maxStemLength] : stem;

        return $"{safeStem}{suffix}";
    }

    private async Task SetThumbnailAsync()
    {
        var image = await _thumbnail.GetVideoThumbnailAsync(
            FilePath.From(c_InputFileTextBox.Text),
            FilePath.TempDir());

        c_ThumbnailPictureBox.Image = Image.FromFile(image.FullPath);
    }

    private async Task SetVideoSizeAsync()
    {
        var videoLength = await Task.Run(() =>
        {
            var videoFile = FilePath.From(c_InputFileTextBox.Text);

            return videoFile.FileInfo().Length.ToFileSizeString();
        });

        c_VideoSizeLabel.Text = videoLength;
    }
    #endregion
}
