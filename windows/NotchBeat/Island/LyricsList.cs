using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NotchBeat;

/// <summary>
/// Karaoke lyrics: the current line fills with color as it's sung and stays centered.
/// Click a line to jump to it.
/// </summary>
public sealed class LyricsList : FrameworkElement
{
    private const double LineSpacing = 12;
    /// <summary>Lets the first and last lines reach the center.</summary>
    private const double EdgePadding = 70;
    private const double NormalSize = 14;
    private const double CurrentSize = 17;

    private double _scrollOffset;
    private bool _snapNextFrame = true;
    private DateTime _lastFrame = DateTime.UtcNow;
    /// <summary>Top of each line from the last frame, used for clicks.</summary>
    private readonly List<(double Top, double Bottom)> _lineBounds = new();

    public IslandModel? Model { get; set; }

    public LyricsList()
    {
        Cursor = Cursors.Hand;
        ClipToBounds = true;
        var fade = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
        fade.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
        fade.GradientStops.Add(new GradientStop(Colors.Black, 0.2));
        fade.GradientStops.Add(new GradientStop(Colors.Black, 0.8));
        fade.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
        fade.Freeze();
        OpacityMask = fade;
    }

    /// <summary>Jump straight to the current line next time instead of scrolling there.</summary>
    public void SnapToCurrentLine() => _snapNextFrame = true;

    public void Animate() => InvalidateVisual();

    protected override void OnRender(DrawingContext context)
    {
        context.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        var nowPlaying = Model?.NowPlaying;
        if (nowPlaying == null) return;

        var now = DateTime.UtcNow;
        double frameSeconds = Math.Clamp((now - _lastFrame).TotalSeconds, 0, 0.1);
        _lastFrame = now;

        if (nowPlaying.Lyrics is not { } lyrics)
        {
            var empty = Text.Format(this, "No synced lyrics for this song", 13, FontWeights.Normal, Text.White(0.5));
            context.DrawText(empty, new Point((ActualWidth - empty.Width) / 2, (ActualHeight - empty.Height) / 2));
            return;
        }

        var progress = lyrics.Progress(nowPlaying.Position(now));
        var accent = nowPlaying.Accent;
        double width = Math.Max(1, ActualWidth);

        // Lay out every line.
        var lines = new List<(FormattedText Base, FormattedText? Fill, double Top, double Fraction)>();
        _lineBounds.Clear();
        double y = EdgePadding;
        double currentCenter = EdgePadding;
        for (int i = 0; i < lyrics.Lines.Count; i++)
        {
            string text = lyrics.Lines[i].Text.Length == 0 ? "♪" : lyrics.Lines[i].Text;
            bool isCurrent = progress?.Index == i;
            bool isSung = progress is { } p && i < p.Index;
            double size = isCurrent ? CurrentSize : NormalSize;
            var weight = isCurrent ? FontWeights.Bold : FontWeights.SemiBold;

            var baseText = Text.Format(this, text, size, weight, Text.White(isSung ? 0.5 : 0.3));
            baseText.MaxTextWidth = width;
            FormattedText? fill = null;
            if (isCurrent)
            {
                fill = Text.Format(this, text, size, weight, Text.Solid(accent));
                fill.MaxTextWidth = width;
                currentCenter = y + baseText.Height / 2;
            }

            lines.Add((baseText, fill, y, progress?.Fraction ?? 0));
            _lineBounds.Add((y - LineSpacing / 2, y + baseText.Height + LineSpacing / 2));
            y += baseText.Height + LineSpacing;
        }

        // Ease the current line toward the middle.
        double target = Math.Max(0, currentCenter - ActualHeight / 2);
        if (_snapNextFrame)
        {
            _scrollOffset = target;
            _snapNextFrame = false;
        }
        else
        {
            _scrollOffset += (target - _scrollOffset) * Math.Min(1, frameSeconds * 7);
        }

        context.PushTransform(new TranslateTransform(0, -_scrollOffset));
        foreach (var (baseText, fill, top, fraction) in lines)
        {
            if (top - _scrollOffset > ActualHeight || top + baseText.Height - _scrollOffset < 0) continue;
            context.DrawText(baseText, new Point(0, top));
            if (fill != null)
            {
                // Same fill width across wrapped rows, like the macOS version.
                context.PushClip(new RectangleGeometry(new Rect(0, top, baseText.WidthIncludingTrailingWhitespace * fraction, baseText.Height)));
                context.DrawText(fill, new Point(0, top));
                context.Pop();
            }
        }
        context.Pop();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        var lyrics = Model?.NowPlaying.Lyrics;
        if (lyrics == null) return;
        double y = e.GetPosition(this).Y + _scrollOffset;
        for (int i = 0; i < _lineBounds.Count && i < lyrics.Lines.Count; i++)
        {
            if (y >= _lineBounds[i].Top && y < _lineBounds[i].Bottom)
            {
                Model!.NowPlaying.Seek(lyrics.Lines[i].Time);
                e.Handled = true;
                return;
            }
        }
    }
}
