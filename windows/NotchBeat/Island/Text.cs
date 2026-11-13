using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace NotchBeat;

/// <summary>Helpers for drawing text in custom-drawn elements.</summary>
internal static class Text
{
    public static FormattedText Format(Visual visual, string text, double size, FontWeight weight, Brush brush,
                                       FontFamily? family = null)
    {
        var typeface = new Typeface(family ?? Theme.Typography.Text, FontStyles.Normal, weight, FontStretches.Normal);
        return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, size, brush,
            VisualTreeHelper.GetDpi(visual).PixelsPerDip);
    }

    public static Brush White(double opacity) => Frozen(new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), 255, 255, 255)));

    public static Brush Solid(Color color, double opacity = 1) => Frozen(new SolidColorBrush(WaveRenderer.WithOpacity(color, opacity)));

    private static Brush Frozen(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
