using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NotchBeat;

/// <summary>Extracts an accent color from album art so the waves match the song.</summary>
public static class ArtworkPalette
{
    /// <summary>
    /// Returns the most vivid color in the image, brightened to stay visible on black.
    /// Returns <c>null</c> for grayscale art.
    /// </summary>
    public static Color? AccentColor(BitmapSource image)
    {
        const int side = 12;
        try
        {
            var bgra = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            var small = new TransformedBitmap(bgra, new ScaleTransform(
                (double)side / bgra.PixelWidth, (double)side / bgra.PixelHeight));
            int width = small.PixelWidth, height = small.PixelHeight;
            var pixels = new byte[width * height * 4];
            small.CopyPixels(pixels, width * 4, 0);

            double bestScore = 0, bestHue = 0, bestSaturation = 0, bestValue = 0;
            for (int i = 0; i < width * height; i++)
            {
                double b = pixels[i * 4] / 255.0, g = pixels[i * 4 + 1] / 255.0, r = pixels[i * 4 + 2] / 255.0;
                var (hue, saturation, value) = ToHsv(r, g, b);
                double score = saturation * value;
                if (score > bestScore)
                {
                    (bestScore, bestHue, bestSaturation, bestValue) = (score, hue, saturation, value);
                }
            }

            if (bestScore <= 0.08) return null;
            return FromHsv(bestHue, bestSaturation, Math.Max(bestValue, 0.8));
        }
        catch
        {
            return null;
        }
    }

    private static (double Hue, double Saturation, double Value) ToHsv(double r, double g, double b)
    {
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
        double hue = 0;
        if (delta > 0)
        {
            if (max == r) hue = (g - b) / delta % 6;
            else if (max == g) hue = (b - r) / delta + 2;
            else hue = (r - g) / delta + 4;
            hue *= 60;
            if (hue < 0) hue += 360;
        }
        return (hue, max == 0 ? 0 : delta / max, max);
    }

    private static Color FromHsv(double hue, double saturation, double value)
    {
        double c = value * saturation;
        double x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        double m = value - c;
        var (r, g, b) = (int)(hue / 60) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }
}
