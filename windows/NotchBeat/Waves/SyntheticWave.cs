using System;

namespace NotchBeat;

/// <summary>Animated wave levels used when real audio isn't available.</summary>
public static class SyntheticWave
{
    /// <summary>Levels (0...1) for <paramref name="count"/> bars at <paramref name="time"/> seconds.</summary>
    public static double[] Levels(int count, double time)
    {
        var levels = new double[count];
        for (int index = 0; index < count; index++)
        {
            double x = (double)index / Math.Max(count - 1, 1); // 0...1 across the row
            // Three sine waves at different speeds blend into an organic, non-repeating motion.
            double wave = Math.Sin(time * 5.1 + x * 7) * 0.5
                + Math.Sin(time * 3.3 - x * 11 + 1.3) * 0.3
                + Math.Sin(time * 8.7 + x * 3 + 2.1) * 0.2;
            double normalized = (wave + 1) / 2;
            // Taller in the middle, shorter at the edges.
            double envelope = 0.35 + 0.65 * Math.Sin(x * Math.PI);
            levels[index] = normalized * envelope;
        }
        return levels;
    }
}
