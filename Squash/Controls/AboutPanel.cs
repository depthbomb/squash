namespace Squash.Controls;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        InitializeComponent();

        c_VersionLabel.Text            = Application.ProductVersion;
        c_HostOsLabel.Text             = $"Operating system: {RuntimeInformation.OSDescription}";
        c_HostArchLabel.Text           = $"Architecture: {RuntimeInformation.OSArchitecture}";
        c_RuntimeDescriptionLabel.Text = $"Runtime description: {RuntimeInformation.FrameworkDescription}";
        c_RuntimeIdentifierLabel.Text  = $"Runtime identifier: {RuntimeInformation.RuntimeIdentifier}";
        c_LicenseTextBox.Text          = Resources.Strings.license;
    }
}

