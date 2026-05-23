namespace Squash.Controls;

public partial class SettingsPanel : UserControl
{
    public SettingsPanel()
    {
        InitializeComponent();

        c_EnableNotificationsCheckBox.DataBindings.Add(
            "Checked",
            Settings.Default,
            nameof(Settings.Default.EnableNotifications),
            false,
            DataSourceUpdateMode.OnPropertyChanged
        );
        c_EnableAdditionalQualityPresetsCheckBox.DataBindings.Add(
            "Checked",
            Settings.Default,
            nameof(Settings.Default.EnableAdditionalQualityPresets),
            false,
            DataSourceUpdateMode.OnPropertyChanged
        );

        c_SaveButton.Click  += C_SaveButtonOnClick;
        c_ResetButton.Click += C_ResetButtonOnClick;
    }

    private void C_SaveButtonOnClick(object? sender, EventArgs e)
    {
        Settings.Default.Save();
    }

    private void C_ResetButtonOnClick(object? sender, EventArgs e)
    {
        Settings.Default.Reset();
        Settings.Default.Reload();
    }
}
