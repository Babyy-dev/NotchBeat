using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace NotchBeat;

/// <summary>Time-synced lyrics parsed from the LRC format (<c>[mm:ss.xx] line</c>).</summary>
public sealed partial class Lyrics
{
    public sealed record Line(double Time, string Text);

    public IReadOnlyList<Line> Lines { get; }

    private Lyrics(IReadOnlyList<Line> lines) => Lines = lines;

    [GeneratedRegex(@"\[(\d+):(\d+(?:\.\d+)?)\]")]
    private static partial Regex Timestamp();

    public static Lyrics? Parse(string? lrc)
    {
        if (string.IsNullOrWhiteSpace(lrc)) return null;
        var lines = new List<Line>();

        foreach (var rawLine in lrc.Split('\n', '\r'))
        {
            // A line can carry several timestamps when it repeats, e.g. "[00:12.00][01:30.00] Chorus".
            var stamps = Timestamp().Matches(rawLine);
            if (stamps.Count == 0) continue;
            var last = stamps[^1];
            string text = rawLine[(last.Index + last.Length)..].Trim();

            foreach (Match stamp in stamps)
            {
                if (double.TryParse(stamp.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes) &&
                    double.TryParse(stamp.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                {
                    lines.Add(new Line(minutes * 60 + seconds, text));
                }
            }
        }

        return lines.Count == 0 ? null : new Lyrics(lines.OrderBy(line => line.Time).ToList());
    }

    /// <summary>The line being sung at <paramref name="position"/>, or <c>null</c> before the first line.</summary>
    public string? LineAt(double position) => Progress(position) is { } progress ? Lines[progress.Index].Text : null;

    /// <summary>
    /// Index of the line being sung and how far through it we are (0...1),
    /// estimated from the time until the next line.
    /// </summary>
    public (int Index, double Fraction)? Progress(double position)
    {
        int index = -1;
        for (int i = 0; i < Lines.Count; i++)
        {
            if (Lines[i].Time <= position) index = i;
            else break;
        }
        if (index < 0) return null;

        double start = Lines[index].Time;
        double end = index + 1 < Lines.Count ? Lines[index + 1].Time : start + 4;
        double fraction = (position - start) / Math.Max(end - start, 0.1);
        return (index, Math.Clamp(fraction, 0, 1));
    }
}
