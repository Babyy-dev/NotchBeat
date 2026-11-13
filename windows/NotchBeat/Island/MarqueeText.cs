using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace NotchBeat;

/// <summary>Single-line text that scrolls sideways when it's too long to fit, with faded edges.</summary>
public sealed class MarqueeText : FrameworkElement
{
    private const double Gap = 40;
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private string _text = "";
    private FormattedText? _formatted;

    public double FontSize { get; set; } = Theme.Typography.TitleSize;
    public FontWeight FontWeight { get; set; } = FontWeights.SemiBold;
    public Brush Foreground { get; set; } = Brushes.White;

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            _formatted = null;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    public MarqueeText()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
        var fade = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
        fade.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
        fade.GradientStops.Add(new GradientStop(Colors.Black, 0.08));
        fade.GradientStops.Add(new GradientStop(Colors.Black, 0.92));
        fade.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
        fade.Freeze();
        OpacityMask = fade;
    }

    /// <summary>Whether the text is wider than the space and needs redrawing every frame.</summary>
    public bool IsScrolling => Formatted().Width > ActualWidth;

    public void Animate()
    {
        if (IsScrolling) InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var formatted = Formatted();
        double width = double.IsInfinity(availableSize.Width) ? formatted.Width : availableSize.Width;
        return new Size(width, Math.Ceiling(formatted.Height));
    }

    protected override void OnRender(DrawingContext context)
    {
        var formatted = Formatted();
        double width = ActualWidth;
        if (formatted.Width <= width)
        {
            context.DrawText(formatted, new Point((width - formatted.Width) / 2, 0));
            return;
        }

        // Waits MarqueePause, scrolls one full distance, then repeats. A second copy follows for a seamless loop.
        double distance = formatted.Width + Gap;
        double scrollDuration = distance / Theme.Typography.MarqueeSpeed;
        double cycle = Theme.Typography.MarqueePause + scrollDuration;
        double phase = Clock.Elapsed.TotalSeconds % cycle;
        double offset = -Math.Max(0, phase - Theme.Typography.MarqueePause) * Theme.Typography.MarqueeSpeed;

        context.DrawText(formatted, new Point(offset, 0));
        context.DrawText(formatted, new Point(offset + distance, 0));
    }

    private FormattedText Formatted() =>
        _formatted ??= NotchBeat.Text.Format(this, _text, FontSize, FontWeight, Foreground);
}
