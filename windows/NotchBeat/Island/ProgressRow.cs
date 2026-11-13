using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NotchBeat;

/// <summary>
/// Samsung-style progress: wave hills ride on the played part, a glowing thumb, then a gray track,
/// with times underneath. Drag anywhere on it to seek. While the volume changes, it shows a volume bar instead.
/// </summary>
public sealed class ProgressRow : FrameworkElement
{
    private const double BarHeight = 5;
    private const double ThumbSize = 13;
    /// <summary>Space between the thumb and the unplayed track.</summary>
    private const double ThumbGap = 9;
    private const double LabelSpacing = 3;

    public IslandModel? Model { get; set; }

    public ProgressRow()
    {
        Cursor = Cursors.Hand;
        Height = Theme.Waves.ProgressHeight + ThumbSize / 2 + LabelSpacing + 13;
    }

    public void Animate() => InvalidateVisual();

    protected override void OnRender(DrawingContext context)
    {
        if (Model == null) return;
        // An invisible background makes the whole row draggable.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

        if (Model.IsShowingVolume) DrawVolume(context, Model.NowPlaying);
        else DrawProgress(context, Model);
    }

    private void DrawProgress(DrawingContext context, IslandModel model)
    {
        var nowPlaying = model.NowPlaying;
        var accent = nowPlaying.Accent;
        double width = ActualWidth;
        double duration = nowPlaying.Track?.Duration ?? 0;
        double played = duration > 0 ? nowPlaying.Position(DateTime.UtcNow) / duration : 0;
        double fraction = Math.Clamp(model.ScrubFraction ?? played, 0, 1);
        double x = width * fraction;
        double barY = Theme.Waves.ProgressHeight; // Waves sit on top of the bar, the thumb hangs half below.

        // Unplayed track.
        double restWidth = Math.Max(0, width - x - ThumbGap);
        if (restWidth > 0)
        {
            context.DrawRoundedRectangle(Text.White(0.25), null,
                new Rect(x + ThumbGap, barY - (BarHeight - 1) / 2, restWidth, BarHeight - 1), 2, 2);
        }

        // Played bar with waves riding on it.
        if (x > 0)
        {
            context.DrawRoundedRectangle(Text.Solid(accent), null, new Rect(0, barY - BarHeight / 2, x, BarHeight), 2.5, 2.5);

            var levels = WaveView.Levels(Theme.Waves.Progress.BarCount, nowPlaying.IsPlaying, nowPlaying.Visualizer);
            context.PushTransform(new TranslateTransform(0, barY - Theme.Waves.ProgressHeight));
            context.PushClip(new RectangleGeometry(new Rect(0, 0, x, Theme.Waves.ProgressHeight)));
            new WaveRenderer(WaveStyle.Flow, Theme.Waves.Progress, accent)
                .Draw(context, levels, new Size(Math.Max(1, x), Theme.Waves.ProgressHeight));
            context.Pop();
            context.Pop();
        }

        // Glowing thumb.
        var glow = new RadialGradientBrush(WaveRenderer.WithOpacity(accent, 0.8), Colors.Transparent);
        glow.Freeze();
        context.DrawEllipse(glow, null, new Point(x, barY), ThumbSize, ThumbSize);
        var ring = new Pen(Brushes.White, 2.5);
        ring.Freeze();
        context.DrawEllipse(Text.Solid(accent), ring, new Point(x, barY), ThumbSize / 2 - 1.25, ThumbSize / 2 - 1.25);

        // Times.
        double labelY = Theme.Waves.ProgressHeight + ThumbSize / 2 + LabelSpacing;
        var elapsed = Text.Format(this, Format(fraction * duration), 10, FontWeights.Medium, Text.White(0.5));
        var total = Text.Format(this, Format(duration), 10, FontWeights.Medium, Text.White(0.5));
        context.DrawText(elapsed, new Point(0, labelY));
        context.DrawText(total, new Point(width - total.Width, labelY));
    }

    private void DrawVolume(DrawingContext context, NowPlaying nowPlaying)
    {
        double volume = nowPlaying.Volume;
        double middle = ActualHeight / 2;
        var brush = Text.White(0.7);

        var icon = Text.Format(this, volume == 0 ? Theme.Glyphs.Mute : Theme.Glyphs.VolumeHigh, 12, FontWeights.Normal,
            brush, Theme.Typography.Icons);
        context.DrawText(icon, new Point(0, middle - icon.Height / 2));

        var number = Text.Format(this, ((int)Math.Round(volume)).ToString(), 10, FontWeights.Medium, brush);
        context.DrawText(number, new Point(ActualWidth - number.Width, middle - number.Height / 2));

        double barLeft = 24, barRight = ActualWidth - 32;
        double barWidth = Math.Max(0, barRight - barLeft);
        context.DrawRoundedRectangle(Text.White(0.25), null, new Rect(barLeft, middle - 2.5, barWidth, 5), 2.5, 2.5);
        context.DrawRoundedRectangle(Text.Solid(nowPlaying.Accent), null,
            new Rect(barLeft, middle - 2.5, barWidth * volume / 100, 5), 2.5, 2.5);
    }

    // Dragging to seek

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (Model == null || Model.IsShowingVolume || Model.NowPlaying.Track?.Duration is not > 0) return;
        CaptureMouse();
        Model.ScrubFraction = FractionAt(e);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (Model == null || !IsMouseCaptured) return;
        Model.ScrubFraction = FractionAt(e);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (Model == null || !IsMouseCaptured) return;
        ReleaseMouseCapture();
        double duration = Model.NowPlaying.Track?.Duration ?? 0;
        Model.NowPlaying.Seek(FractionAt(e) * duration);
        Model.ScrubFraction = null;
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        if (Model != null) Model.ScrubFraction = null;
    }

    private double FractionAt(MouseEventArgs e) =>
        ActualWidth > 0 ? Math.Clamp(e.GetPosition(this).X / ActualWidth, 0, 1) : 0;

    private static string Format(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }
}
