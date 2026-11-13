using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace NotchBeat;

/// <summary>Live wave visualizer. Uses real audio levels when available, otherwise a smooth animation.</summary>
public sealed class WaveView : FrameworkElement
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private readonly DropShadowEffect _glow = new() { ShadowDepth = 0, BlurRadius = Theme.Waves.GlowRadius * 2, Opacity = 0.6 };

    public WaveStyle WaveStyle { get; set; } = WaveStyle.Flow;
    public WaveLayout Layout { get; set; } = Theme.Waves.Compact;
    public bool IsPlaying { get; set; }
    public AudioVisualizer? Visualizer { get; set; }

    public Color Color
    {
        get => _glow.Color;
        set => _glow.Color = value;
    }

    public WaveView()
    {
        Effect = _glow;
        IsHitTestVisible = false;
    }

    /// <summary>Called every frame by the island.</summary>
    public void Animate() => InvalidateVisual();

    protected override void OnRender(DrawingContext context)
    {
        var levels = Levels(Layout.BarCount, IsPlaying, Visualizer);
        new WaveRenderer(WaveStyle, Layout, Color).Draw(context, levels, RenderSize);
    }

    /// <summary>The levels to draw right now, lifted so even silence shows short bars.</summary>
    public static double[] Levels(int count, bool isPlaying, AudioVisualizer? visualizer)
    {
        double rest = Theme.Waves.RestLevel;
        if (!isPlaying) return Enumerable.Repeat(rest, count).ToArray();

        var raw = visualizer?.Levels(count)
            ?? SyntheticWave.Levels(count, Clock.Elapsed.TotalSeconds * Theme.Waves.Speed);
        return raw.Select(level => rest + (1 - rest) * Math.Clamp(level, 0, 1)).ToArray();
    }
}
