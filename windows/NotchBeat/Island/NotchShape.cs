using System;
using System.Windows;
using System.Windows.Media;

namespace NotchBeat;

/// <param name="Top">Outward flare where the island meets the top of the screen.</param>
/// <param name="Bottom">Rounded bottom corners.</param>
public readonly record struct NotchCorners(double Top, double Bottom);

/// <summary>The island outline: flared top corners that blend into the screen edge, rounded bottom corners.</summary>
public static class NotchShape
{
    public static Geometry Create(double width, double height, NotchCorners corners)
    {
        // Keep the curves inside the shape while it is very small mid-animation.
        double top = Math.Clamp(corners.Top, 0, width / 4);
        double bottom = Math.Clamp(corners.Bottom, 0, Math.Min(height - top, (width - 2 * top) / 2));
        bottom = Math.Max(bottom, 0);
        double minX = 0, minY = 0, maxX = width, maxY = height;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(minX, minY), isFilled: true, isClosed: true);
            ctx.QuadraticBezierTo(new Point(minX + top, minY), new Point(minX + top, minY + top), true, true);
            ctx.LineTo(new Point(minX + top, maxY - bottom), true, true);
            ctx.QuadraticBezierTo(new Point(minX + top, maxY), new Point(minX + top + bottom, maxY), true, true);
            ctx.LineTo(new Point(maxX - top - bottom, maxY), true, true);
            ctx.QuadraticBezierTo(new Point(maxX - top, maxY), new Point(maxX - top, maxY - bottom), true, true);
            ctx.LineTo(new Point(maxX - top, minY + top), true, true);
            ctx.QuadraticBezierTo(new Point(maxX - top, minY), new Point(maxX, minY), true, true);
        }
        geometry.Freeze();
        return geometry;
    }
}
