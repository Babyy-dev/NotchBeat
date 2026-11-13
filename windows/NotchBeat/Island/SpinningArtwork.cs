using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NotchBeat;

/// <summary>
/// Round album art that turns like a vinyl record while music plays, with a music-note placeholder while it loads.
/// The angle follows the song position, so it stops on pause and resumes smoothly.
/// </summary>
public sealed class SpinningArtwork : FrameworkElement
{
    private BitmapSource? _brushSource;
    private ImageBrush? _brush;

    public NowPlaying? NowPlaying { get; set; }
    public bool IsSpinning { get; set; } = true;

    public double Size
    {
        get => Width;
        set { Width = value; Height = value; }
    }

    public SpinningArtwork()
    {
        IsHitTestVisible = false;
    }

    public void Animate() => InvalidateVisual();

    protected override void OnRender(DrawingContext context)
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0 || NowPlaying == null) return;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double radius = size / 2;

        double degrees = IsSpinning ? NowPlaying.Position(DateTime.UtcNow) * Theme.Artwork.SpinDegreesPerSecond : 0;
        context.PushTransform(new RotateTransform(degrees % 360, center.X, center.Y));

        if (NowPlaying.Artwork is { } artwork)
        {
            if (!ReferenceEquals(artwork, _brushSource))
            {
                _brushSource = artwork;
                _brush = new ImageBrush(artwork) { Stretch = Stretch.UniformToFill };
                _brush.Freeze();
            }
            context.DrawEllipse(_brush, null, center, radius, radius);
        }
        else
        {
            context.DrawEllipse(Text.White(0.1), null, center, radius, radius);
            var note = Text.Format(this, Theme.Glyphs.MusicNote, size * 0.45, FontWeights.Normal, Text.White(0.6),
                Theme.Typography.Icons);
            context.DrawText(note, new Point(center.X - note.Width / 2, center.Y - note.Height / 2));
        }

        if (IsSpinning && size >= Theme.Artwork.VinylHoleMinimumSize)
        {
            double hole = size * 0.09;
            var ring = new Pen(Text.White(0.25), 1);
            ring.Freeze();
            context.DrawEllipse(Text.Solid(Colors.Black, 0.7), ring, center, hole, hole);
        }

        context.Pop();
    }
}
