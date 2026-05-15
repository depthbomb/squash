using System.ComponentModel;

namespace Squash.Controls;

public class NumericUpDownEx : NumericUpDown
{
    [Category("Appearance")]
    [DefaultValue("")]
    public string Prefix { get; set; } = string.Empty;

    [Category("Appearance")]
    [DefaultValue("")]
    public string Suffix { get; set; } = string.Empty;

    protected override void UpdateEditText()
    {
        ChangingText = true;

        Text = $"{Prefix}{Value}{Suffix}";

        ChangingText = false;
    }
}
