namespace Squash.Forms;

public partial class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();

        c_HostOsLabel.Text             = $"Operating System: {RuntimeInformation.OSDescription}";
        c_HostArchitectureLabel.Text   = $"Architecture: {RuntimeInformation.OSArchitecture}";
        c_RuntimeDescriptionLabel.Text = $"Description: {RuntimeInformation.FrameworkDescription}";
        c_RuntimeIdentifierLabel.Text  = $"Identifier: {RuntimeInformation.RuntimeIdentifier}";

        c_CopyrightLinkLabel.Click += C_CopyrightLinkLabelOnClick;
    }

    private void C_CopyrightLinkLabelOnClick(object? sender, EventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName        = "https://github.com/depthbomb",
            UseShellExecute = true
        });
    }
}
