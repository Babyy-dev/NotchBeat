using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace NotchBeat;

/// <summary>Draws wave levels (0...1) in one of the <see cref="WaveStyle"/>s.</summary>
public sealed class WaveRenderer
{
    private readonly WaveStyle _style;
    private readonly WaveLayout _layout;
    private readonly Color _color;

    public WaveRenderer(WaveStyle style, WaveLayout layout, Color color)
    {
        _style = style;
        _layout = layout;
        _color = color;
    }

    public void Draw(DrawingContext context, IReadOnlyList<double> levels, Size size)
    {
        if (levels.Count == 0 || size.Width <= 0 || size.Height <= 0) return;
        switch (_style)
        {
            case WaveStyle.Flow: DrawFlow(context, levels, size); break;
            case WaveStyle.Bars: DrawBars(context, levels, size); break;
            case WaveStyle.Line: DrawLine(context, levels, size); break;
            case WaveStyle.Dots: DrawDots(context, levels, size); break;
            case WaveStyle.Circle: DrawCircle(context, levels, size); break;
        }
    }

    /// <summary>Three overlapping translucent hills, each fed a shifted copy of the levels so they move independently.</summary>
    private void DrawFlow(DrawingContext context, IReadOnlyList<double> levels, Size size)
    {
        int count = levels.Count;
        if (count < 2) return;
        var layers = new (int Shift, double Scale, double Opacity)[]
        {
            (count / 3, 0.75, 0.3),
            (count * 2 / 3, 0.9, 0.45),
            (0, 1, 0.85),
        };

        for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            var layer = layers[layerIndex];
            var points = new Point[count];
            for (int i = 0; i < count; i++)
            {
                double x = (double)i / (count - 1);
                double taper = Math.Sin(x * Math.PI); // Ends meet the baseline, like a hill.
                double level = levels[(i + layer.Shift) % count] * layer.Scale * taper;
                points[i] = new Point(x * size.Width, size.Height * (1 - level));
            }

            var hill = new StreamGeometry();
            using (var ctx = hill.Open())
            {
                AddSmoothCurve(ctx, points, isFilled: true);
                ctx.LineTo(new Point(size.Width, size.Height), false, false);
                ctx.LineTo(new Point(0, size.Height), false, false);
            }
            hill.Freeze();
            context.DrawGeometry(VerticalGradient(layer.Opacity, layer.Opacity * 0.5, size.Height), null, hill);

            // A soft highlight on the front wave's crest.
            if (layerIndex == layers.Length - 1)
            {
                var crest = new StreamGeometry();
                using (var ctx = crest.Open()) AddSmoothCurve(ctx, points, isFilled: false);
                crest.Freeze();
                context.DrawGeometry(null, Pen(Color.FromArgb(89, 255, 255, 255), 1), crest);
            }
        }
    }

    private void DrawBars(DrawingContext context, IReadOnlyList<double> levels, Size size)
    {
        var shading = VerticalGradient(1, 0.55, size.Height);
        var xs = Columns(levels.Count, size.Width);
        double radius = _layout.BarWidth / 2;
        for (int i = 0; i < levels.Count; i++)
        {
            double height = Math.Max(_layout.BarWidth, size.Height * levels[i]);
            var rect = new Rect(xs[i] - radius, (size.Height - height) / 2, _layout.BarWidth, height);
            context.DrawRoundedRectangle(shading, null, rect, radius, radius);
        }
    }

    private void DrawLine(DrawingContext context, IReadOnlyList<double> levels, Size size)
    {
        var xs = Columns(levels.Count, size.Width);
        double middle = size.Height / 2;

        // Upper line at full strength, mirrored lower line fainter.
        foreach (var (direction, opacity) in new[] { (-1.0, 1.0), (1.0, 0.5) })
        {
            var points = new Point[levels.Count];
            for (int i = 0; i < levels.Count; i++)
            {
                points[i] = new Point(xs[i], middle + direction * levels[i] * size.Height / 2);
            }
            var line = new StreamGeometry();
            using (var ctx = line.Open()) AddSmoothCurve(ctx, points, isFilled: false);
            line.Freeze();
            var pen = Pen(WithOpacity(_color, opacity), 2);
            context.DrawGeometry(null, pen, line);
        }
    }

    private void DrawDots(DrawingContext context, IReadOnlyList<double> levels, Size size)
    {
        double dot = _layout.BarWidth;
        double step = dot + _layout.Spacing;
        int maxDots = Math.Max(1, (int)((size.Height + _layout.Spacing) / step));
        var brush = Frozen(new SolidColorBrush(_color));
        var xs = Columns(levels.Count, size.Width);

        for (int i = 0; i < levels.Count; i++)
        {
            int count = Math.Max(1, (int)Math.Round(maxDots * levels[i]));
            double y = (size.Height - (count * step - _layout.Spacing)) / 2;
            for (int d = 0; d < count; d++)
            {
                context.DrawEllipse(brush, null, new Point(xs[i], y + dot / 2), dot / 2, dot / 2);
                y += step;
            }
        }
    }

    private void DrawCircle(DrawingContext context, IReadOnlyList<double> levels, Size size)
    {
        var center = new Point(size.Width / 2, size.Height / 2);
        double outer = Math.Min(size.Width, size.Height) / 2;
        double inner = outer * 0.45;
        var pen = Pen(_color, Math.Max(1, _layout.BarWidth * 0.5));

        for (int i = 0; i < levels.Count; i++)
        {
            double angle = (double)i / levels.Count * 2 * Math.PI - Math.PI / 2;
            double dx = Math.Cos(angle), dy = Math.Sin(angle);
            double length = inner + (outer - inner) * levels[i];
            context.DrawLine(pen,
                new Point(center.X + dx * inner, center.Y + dy * inner),
                new Point(center.X + dx * length, center.Y + dy * length));
        }
    }

    /// <summary>A smooth curve through <paramref name="points"/>, using their midpoints as curve ends.</summary>
    private static void AddSmoothCurve(StreamGeometryContext ctx, Point[] points, bool isFilled)
    {
        if (points.Length == 0) return;
        ctx.BeginFigure(points[0], isFilled, isClosed: isFilled);
        for (int i = 1; i < points.Length; i++)
        {
            var previous = points[i - 1];
            var midpoint = new Point((previous.X + points[i].X) / 2, (previous.Y + points[i].Y) / 2);
            ctx.QuadraticBezierTo(previous, midpoint, true, true);
        }
        ctx.LineTo(points[^1], true, true);
    }

    /// <summary>Center x of each bar, with the whole row centered in <paramref name="width"/>.</summary>
    private double[] Columns(int count, double width)
    {
        double rowWidth = count * _layout.BarWidth + (count - 1) * _layout.Spacing;
        double start = (width - rowWidth) / 2 + _layout.BarWidth / 2;
        var xs = new double[count];
        for (int i = 0; i < count; i++) xs[i] = start + i * (_layout.BarWidth + _layout.Spacing);
        return xs;
    }

    private Brush VerticalGradient(double topOpacity, double bottomOpacity, double height)
    {
        var brush = new LinearGradientBrush(WithOpacity(_color, topOpacity), WithOpacity(_color, bottomOpacity),
            new Point(0, 0), new Point(0, Math.Max(height, 1)))
        {
            MappingMode = BrushMappingMode.Absolute,
        };
        return Frozen(brush);
    }

    private static Pen Pen(Color color, double thickness)
    {
        var pen = new Pen(new SolidColorBrush(color), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        pen.Freeze();
        return pen;
    }

    internal static Color WithOpacity(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Clamp(color.A * opacity, 0, 255), color.R, color.G, color.B);

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
