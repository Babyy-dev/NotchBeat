using System.Windows;
using System.Windows.Media;

namespace NotchBeat;

/// <summary>Every visual knob lives here. Change these values to make the island your own.</summary>
public static class Theme
{
    public static class Island
    {
        /// <summary>The always-visible bar at the top of the screen (Windows has no notch to hide behind).</summary>
        public const double BarWidth = 190;
        public const double BarHeight = 32;
        /// <summary>Extra width shown on each side of the bar while music plays.</summary>
        public const double CompactWingWidth = 34;

        /// <summary>Space above the content of the open island.</summary>
        public const double ContentTopPadding = 14;

        public static readonly Size ExpandedSize = new(360, 136);
        /// <summary>Extra height added to the expanded island when lyrics are on.</summary>
        public const double LyricsRowHeight = 26;

        /// <summary>The pop shown when the song changes.</summary>
        public const double PeekWidth = 300;
        public const double PeekHeight = 62;
        public const double PeekDuration = 3;

        /// <summary>Full karaoke lyrics view.</summary>
        public static readonly Size LyricsSize = new(360, 280);

        /// <summary>System pop-ups (volume, audio device, charging).</summary>
        public const double ActivityHeight = 54;
        public const double ActivityDuration = 2.5;
        public const double VolumeActivityDuration = 1.5;

        public static readonly NotchCorners CompactCorners = new(6, 10);
        public static readonly NotchCorners ExpandedCorners = new(14, 36);

        /// <summary>How long the pointer must rest on the island before it opens, in seconds.</summary>
        public const double HoverDelay = 0.2;
        /// <summary>Default distance from the screen edge for the left and right positions.</summary>
        /// <remarks>Leaves room for the minimize, maximize and close buttons of full-size windows.</remarks>
        public const double DefaultEdgeMargin = 160;
    }

    public static class Waves
    {
        public static readonly WaveLayout Compact = new(BarCount: 5, BarWidth: 2.5, Spacing: 2);
        public static readonly Size CompactSize = new(24, 14);
        /// <summary>Waves along the bottom edge of the expanded island.</summary>
        public static readonly WaveLayout BottomEdge = new(BarCount: 40, BarWidth: 4, Spacing: 3);
        public const double BottomEdgeHeight = 22;
        public const double BottomEdgeOpacity = 0.6;
        /// <summary>Hills riding on the played part of the progress bar.</summary>
        public static readonly WaveLayout Progress = new(BarCount: 20, BarWidth: 3, Spacing: 2);
        public const double ProgressHeight = 16;
        /// <summary>Bar height (0...1) when music is paused.</summary>
        public const double RestLevel = 0.18;
        /// <summary>Speed of the animated (non-reactive) waves. Higher is faster.</summary>
        public const double Speed = 1.0;
        public const double GlowRadius = 4;
    }

    public static class Colors
    {
        public static readonly Color Background = System.Windows.Media.Colors.Black;
        /// <summary>Used until album art loads, or when the art has no vivid color.</summary>
        public static readonly Color FallbackAccent = Color.FromRgb(92, 140, 255);
        /// <summary>Album-color tint used by the Now Bar theme until artwork loads.</summary>
        public const double AlbumTintOpacity = 0.28;
        /// <summary>Now Bar theme: how blurry and how darkened the album art background is.</summary>
        public const double ArtworkBlurRadius = 28;
        public const double ArtworkDimming = 0.45;
        /// <summary>Glass theme: a dark see-through tint.</summary>
        public static readonly Color GlassTint = Color.FromArgb(0xB4, 0x1C, 0x1C, 0x22);
    }

    public static class Artwork
    {
        /// <summary>Spinning-cover speed. 20 means one full turn every 18 seconds.</summary>
        public const double SpinDegreesPerSecond = 20;
        /// <summary>Covers smaller than this skip the vinyl center hole.</summary>
        public const double VinylHoleMinimumSize = 32;
    }

    public static class Typography
    {
        public static readonly FontFamily Text = new("Segoe UI Variable Text, Segoe UI");
        /// <summary>Windows' built-in icon font (Fluent on Windows 11, MDL2 on Windows 10).</summary>
        public static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");
        public const double TitleSize = 13;
        /// <summary>Scrolling speed (pixels per second) for titles that don't fit.</summary>
        public const double MarqueeSpeed = 30;
        /// <summary>Pause before each scroll, in seconds.</summary>
        public const double MarqueePause = 2;
    }

    public static class Motion
    {
        /// <summary>Spring used when the island changes shape: (response in seconds, damping ratio).</summary>
        public static readonly (double Response, double Damping) Morph = (0.42, 0.78);
        /// <summary>Bouncier spring for pop-ups.</summary>
        public static readonly (double Response, double Damping) Bounce = (0.45, 0.6);
        /// <summary>How long content takes to fade in or out, in seconds.</summary>
        public const double Fade = 0.18;
    }

    public static class Gestures
    {
        /// <summary>Horizontal scroll (in wheel units, 120 = one notch) that skips a track.</summary>
        public const double SwipeThreshold = 120;
        /// <summary>Volume change per notch of a mouse wheel. Touchpads scroll in smaller steps.</summary>
        public const double VolumePerWheelStep = 2;
        public const double VolumeIndicatorDuration = 1.2;
    }

    /// <summary>Segoe icon font characters.</summary>
    public static class Glyphs
    {
        public const string Play = "";
        public const string Pause = "";
        public const string Previous = "";
        public const string Next = "";
        public const string MusicNote = "";
        public const string ChevronUp = "";
        public const string Lyrics = "";
        public const string Mute = "";
        public const string VolumeLow = "";
        public const string VolumeMedium = "";
        public const string VolumeHigh = "";
        public const string Headphones = "";
        public const string Speakers = "";
        public const string Bluetooth = "";
        public const string BatteryCharging = "";
        public const string BatteryFull = "";
        /// <summary>Battery0 … Battery9.</summary>
        public static string Battery(int percent) => percent >= 95
            ? BatteryFull
            : ((char)(0xE850 + System.Math.Clamp(percent / 10, 0, 9))).ToString();
    }
}
