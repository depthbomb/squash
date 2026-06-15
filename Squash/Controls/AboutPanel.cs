using System.Reflection;

namespace Squash.Controls;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        var gitHash = Assembly
                      .GetEntryAssembly()?
                      .GetCustomAttributes<AssemblyMetadataAttribute>()
                      .FirstOrDefault(attr => attr.Key == "GitHashShort")?.Value;

        InitializeComponent();

        c_VersionLabel.Text            = $"{Assembly.GetExecutingAssembly().GetName().Version}+{gitHash}";
        c_HostOsLabel.Text             = $"Operating system: {RuntimeInformation.OSDescription}";
        c_HostArchLabel.Text           = $"Architecture: {RuntimeInformation.OSArchitecture}";
        c_RuntimeDescriptionLabel.Text = $"Runtime description: {RuntimeInformation.FrameworkDescription}";
        c_RuntimeIdentifierLabel.Text  = $"Runtime identifier: {RuntimeInformation.RuntimeIdentifier}";
        c_LicenseTextBox.Text          = Resources.Strings.license;
    }
}

