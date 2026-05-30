using Windows.UI.ViewManagement;

namespace Squash.Theme;

public static class AccentHelper
{
    public static event EventHandler? ThemeUpdated;

    private static readonly UISettings Ui = new();

    static AccentHelper()
    {
        Ui.ColorValuesChanged += (_, _) => ThemeUpdated?.Invoke(typeof(AccentHelper), EventArgs.Empty);
    }

    public static Color GetBackground(UIColorType type)
    {
        var c = Ui.GetColorValue(type);

        return Color.FromArgb(c.A, c.R, c.G, c.B);
    }

    public static Color GetForeground(UIColorType type)
    {
        var color = GetBackground(type);
        return 5 * color.G + 2 * color.R + color.B > 8 * 128 ? Color.Black : Color.White;
    }
}
