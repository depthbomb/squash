namespace Squash.Extensions;

public static class DoubleExtensions
{
    extension(double value)
    {
        public string ToDurationString()
        {
            if (value < 0)
            {
                value = 0;
            }
            
            var ts      = TimeSpan.FromSeconds(value);
            var hours   = (int)ts.TotalHours;
            var minutes = ts.Minutes;
            var seconds = ts.Seconds;
            var parts   = new List<string>(3);
            
            if (hours > 0)
                parts.Add($"{hours} hour{(hours == 1 ? "" : "s")}");

            if (minutes > 0)
                parts.Add($"{minutes} minute{(minutes == 1 ? "" : "s")}");

            if (seconds > 0 || parts.Count == 0)
                parts.Add($"{seconds} second{(seconds == 1 ? "" : "s")}");

            return string.Join(", ", parts);
        }
    }
}
