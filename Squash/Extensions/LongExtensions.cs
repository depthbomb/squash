namespace Squash.Extensions;

public static class LongExtensions
{
    const double KB = 1024d;
    const double MB = KB * 1024d;
    const double GB = MB * 1024d;
    const double TB = GB * 1024d;
    const double PB = TB * 1024d;
    
    extension(long lng)
    {
        public string ToFileSizeString(int decimalPlaces = 2)
        {
            if (lng < 0)
            {
                lng = 0;
            }

            double abs = lng;
            return abs switch
            {
                >= PB => Format(abs / PB, "PB"),
                >= TB => Format(abs / TB, "TB"),
                >= GB => Format(abs / GB, "GB"),
                >= MB => Format(abs / MB, "MB"),
                >= KB => Format(abs / KB, "KB"),
                _     => $"{abs} B"
            };

            string Format(double value, string unit) => $"{value.ToString($"F{decimalPlaces}")}{unit}";
        }
    }
}
