using System.Drawing.Imaging;

namespace Squash;

public static class Extensions
{
    extension(Image image)
    {
        public Color GetDominantColor(Color fallback)
        {
            using var bitmap = new Bitmap(image, 64, 64);

            // 4 bits per channel → 12-bit key → exactly 4096 possible buckets
            var buckets = new int[4096];
            var rect    = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                var stride = bmpData.Stride;
                var pixels = new byte[stride * bitmap.Height];

                Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);

                for (var y = 0; y < bitmap.Height; y++)
                {
                    var row = y * stride;
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        var i = row + x * 4;
                        var b = pixels[i]; // Format32bppArgb is BGRA in memory
                        var g = pixels[i + 1];
                        var r = pixels[i + 2];

                        if (r < 30 && g < 30 && b < 30)
                        {
                            continue;
                        }

                        buckets[r >> 4 << 8 | g >> 4 << 4 | b >> 4]++;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            var dominant = 0;
            var maxCount = 0;

            for (var i = 1; i < buckets.Length; i++)
            {
                if (buckets[i] > maxCount)
                {
                    maxCount = buckets[i];
                    dominant = i;
                }
            }

            if (maxCount == 0)
            {
                return fallback;
            }

            return Color.FromArgb(
                (dominant >> 8 & 0xF) * 17,
                (dominant >> 4 & 0xF) * 17,
                (dominant      & 0xF) * 17);
        }
    }
}
