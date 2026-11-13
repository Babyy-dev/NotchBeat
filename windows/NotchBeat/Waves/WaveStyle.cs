namespace NotchBeat;

/// <summary>How the wave levels are drawn. Picked in Settings.</summary>
public enum WaveStyle
{
    /// <summary>Samsung-style layered translucent waves rising from the bottom.</summary>
    Flow,
    /// <summary>Rounded bars mirrored around the center line.</summary>
    Bars,
    /// <summary>A smooth mirrored line.</summary>
    Line,
    /// <summary>Columns of dots, like a dot-matrix equalizer.</summary>
    Dots,
    /// <summary>Bars radiating from a ring.</summary>
    Circle,
}

/// <summary>How many bars to draw and how big they are.</summary>
public readonly record struct WaveLayout(int BarCount, double BarWidth, double Spacing);
