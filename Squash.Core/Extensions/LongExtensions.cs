namespace Squash.Core.Extensions;

public static class LongExtensions
{
    // ReSharper disable InconsistentNaming
    private const double KB = 1024d;
    private const double MB = KB * 1024d;
    private const double GB = MB * 1024d;
    private const double TB = GB * 1024d;
    private const double PB = TB * 1024d;
    // ReSharper enable InconsistentNaming
    
    extension(long value)
    {
        public string ToFileSizeString(int decimalPlaces = 2)
        {
            if (value < 0)
            {
                value = 0;
            }

            double abs = value;
            return abs switch
            {
                >= PB => Format(abs / PB, "PB"),
                >= TB => Format(abs / TB, "TB"),
                >= GB => Format(abs / GB, "GB"),
                >= MB => Format(abs / MB, "MB"),
                >= KB => Format(abs / KB, "KB"),
                _     => $"{abs}B"
            };

            string Format(double value, string unit) => $"{value.ToString($"F{decimalPlaces}")}{unit}";
        }
    }
}
