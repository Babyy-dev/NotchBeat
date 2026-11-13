using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Screen = System.Windows.Forms.Screen;

namespace NotchBeat;

/// <summary>
/// The island: a borderless, see-through, always-on-top window at the top of the screen.
/// The window is sized for the largest island but lets clicks through until the pointer is on the island.
/// Every frame it springs the island toward the size of the current mode and fades the matching content in.
/// </summary>
public partial class IslandWindow : Window
{
    /// <summary>Room around the island for its shadow.</summary>
    private const double ShadowMargin = 24;

    private readonly IslandModel _model;
    private readonly ScrollGestures _gestures;
    private readonly DispatcherTimer _hoverTimer;
    private readonly (FrameworkElement Panel, IslandMode Mode)[] _panels;
    private readonly Spring _width;
    private readonly Spring _height;
    private readonly Spring _cornerTop;
    private readonly Spring _cornerBottom;

    private IntPtr _hwnd;
    private bool _isTicking;
    private TimeSpan _lastFrameTime;
    private DateTime? _hoverStart;
    private bool _isClickThrough;
    private IslandMode _lastMode = IslandMode.Idle;
    private string _lyricLine = "";
    private Color _brushColor;
    private DateTime _lastFullScreenCheck = DateTime.MinValue;

    public IslandWindow(IslandModel model)
    {
        InitializeComponent();
        _model = model;
        _gestures = new ScrollGestures(model);

        var idle = model.SizeFor(IslandMode.Idle);
        _width = new Spring(idle.Width);
        _height = new Spring(idle.Height);
        _cornerTop = new Spring(Theme.Island.CompactCorners.Top);
        _cornerBottom = new Spring(Theme.Island.CompactCorners.Bottom);

        _panels = new (FrameworkElement, IslandMode)[]
        {
            (CompactPanel, IslandMode.Compact),
            (PeekPanel, IslandMode.Peek),
            (ActivityPanel, IslandMode.Activity),
            (ExpandedPanel, IslandMode.Expanded),
            (LyricsPanel, IslandMode.Lyrics),
        };

        ConfigureContent();

        _hoverTimer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(30) };
        _hoverTimer.Tick += (_, _) => UpdateHover();

        model.Changed += StartTicking;
        model.Preferences.Changed += () => Dispatcher.BeginInvoke(ApplyLayout);
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (_, _) => Dispatcher.BeginInvoke(ApplyLayout);
        SourceInitialized += OnSourceInitialized;
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(ApplyLayout);
    }

    // Setup

    /// <summary>Sizes every panel from <see cref="Theme"/> and wires up the buttons.</summary>
    private void ConfigureContent()
    {
        double top = Theme.Island.ContentTopPadding;

        CompactPanel.Width = IslandModel.CompactSize.Width;
        CompactPanel.Height = IslandModel.CompactSize.Height;
        CompactArtwork.Size = 20;
        CompactTitle.Width = Theme.Island.BarWidth - 12;
        CompactTitle.FontSize = 12;
        CompactWaves.Width = Theme.Waves.CompactSize.Width;
        CompactWaves.Height = Theme.Waves.CompactSize.Height;
        CompactWaves.Layout = Theme.Waves.Compact;

        PeekPanel.Width = Theme.Island.PeekWidth;
        PeekPanel.Height = Theme.Island.PeekHeight;
        PeekContent.Margin = new Thickness(24, top, 24, 0);
        PeekArtwork.Size = 32;

        ActivityPanel.Width = Theme.Island.PeekWidth;
        ActivityPanel.Height = Theme.Island.ActivityHeight;
        ActivityContent.Margin = new Thickness(24, top, 24, 0);

        ExpandedPanel.Width = Theme.Island.ExpandedSize.Width;
        ExpandedContent.Margin = new Thickness(22, top, 22, 0);
        ExpandedArtwork.Size = 50;
        BottomWaves.Height = Theme.Waves.BottomEdgeHeight;
        BottomWaves.Opacity = Theme.Waves.BottomEdgeOpacity;
        BottomWaves.Layout = Theme.Waves.BottomEdge;
        LyricLineRow.Height = Theme.Island.LyricsRowHeight - 8;
        LyricsIcon.Text = Theme.Glyphs.Lyrics;
        PreviousButton.Content = Theme.Glyphs.Previous;
        NextButton.Content = Theme.Glyphs.Next;
        PlayPauseButton.Content = Theme.Glyphs.Play;
        Progress.Model = _model;

        LyricsPanel.Width = Theme.Island.LyricsSize.Width;
        LyricsPanel.Height = Theme.Island.LyricsSize.Height;
        LyricsContent.Margin = new Thickness(24, top, 24, 14);
        LyricsArtwork.Size = 28;
        LyricsCloseIcon.Text = Theme.Glyphs.ChevronUp;
        LyricsLines.Model = _model;

        ArtworkBlur.Radius = Theme.Colors.ArtworkBlurRadius;
        ArtworkDimming.Opacity = Theme.Colors.ArtworkDimming;
        // A see-through tint, slightly lighter at the bottom, so the glass reads as glass.
        var tint = Theme.Colors.GlassTint;
        var sheen = Color.FromArgb(tint.A, (byte)(tint.R + 0x18), (byte)(tint.G + 0x18), (byte)(tint.B + 0x1C));
        GlassBackground.Fill = new LinearGradientBrush(tint, sheen, 90);

        var nowPlaying = _model.NowPlaying;
        PreviousButton.Click += (_, _) => nowPlaying.Perform(PlayerCommand.Previous);
        PlayPauseButton.Click += (_, _) => nowPlaying.Perform(PlayerCommand.PlayPause);
        NextButton.Click += (_, _) => nowPlaying.Perform(PlayerCommand.Next);
        LyricLineRow.MouseLeftButtonUp += (_, _) => _model.IsShowingFullLyrics = true;
        LyricsHeader.MouseLeftButtonUp += (_, _) => _model.IsShowingFullLyrics = false;
        IslandHost.MouseWheel += (_, e) => _gestures.Vertical(e.Delta);

        foreach (var artwork in new[] { CompactArtwork, PeekArtwork, ExpandedArtwork, LyricsArtwork })
        {
            artwork.NowPlaying = nowPlaying;
        }
        foreach (var waves in new[] { CompactWaves, BottomWaves })
        {
            waves.Visualizer = nowPlaying.Visualizer;
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        // No taskbar button, no Alt+Tab entry, never steals focus, and clicks pass through until hovered.
        NativeMethods.SetExtendedStyle(_hwnd, NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE, true);
        SetClickThrough(true);
        HwndSource.FromHwnd(_hwnd)?.AddHook(WindowProc);

        ApplyLayout();
        _hoverTimer.Start();
        StartTicking();
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WM_MOUSEHWHEEL)
        {
            // WPF has no event for sideways scrolling.
            int delta = unchecked((short)((long)wParam >> 16));
            _gestures.Horizontal(delta);
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>Puts the window at the top of the chosen screen, at the chosen position.</summary>
    private void ApplyLayout()
    {
        if (_hwnd == IntPtr.Zero) return;
        var preferences = _model.Preferences;
        var screen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == preferences.Display) ?? Screen.PrimaryScreen;
        if (screen == null) return;
        var bounds = screen.Bounds; // Physical pixels.
        double scale = NativeMethods.ScaleAt(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

        var largest = IslandModel.LargestSize;
        int width = (int)Math.Ceiling((largest.Width + 2 * ShadowMargin) * scale);
        int height = (int)Math.Ceiling((largest.Height + ShadowMargin) * scale);
        int shadow = (int)Math.Round(ShadowMargin * scale);
        int margin = (int)Math.Round(preferences.EdgeMargin * scale);

        // The island's own edge sits EdgeMargin from the screen edge; the shadow margin may hang off-screen.
        int x = preferences.Position switch
        {
            IslandPosition.TopLeft => bounds.Left + margin - shadow,
            IslandPosition.TopRight => bounds.Right - width - margin + shadow,
            _ => bounds.Left + (bounds.Width - width) / 2,
        };
        IslandHost.HorizontalAlignment = preferences.Position switch
        {
            IslandPosition.TopLeft => HorizontalAlignment.Left,
            IslandPosition.TopRight => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Center,
        };
        IslandHost.Margin = new Thickness(ShadowMargin, 0, ShadowMargin, 0);

        NativeMethods.SetWindowPos(_hwnd, NativeMethods.HWND_TOPMOST, x, bounds.Top, width, height, NativeMethods.SWP_NOACTIVATE);
        StartTicking();
    }

    // Hover and click-through

    private void UpdateHover()
    {
        HideDuringFullScreenApps();

        bool isInside = false;
        if (_model.NowPlaying.Track != null && Root.Visibility == Visibility.Visible && IslandHost.ActualWidth > 0)
        {
            NativeMethods.GetCursorPos(out var cursor);
            var topLeft = IslandHost.PointToScreen(new Point(0, 0));
            var bottomRight = IslandHost.PointToScreen(new Point(IslandHost.ActualWidth, IslandHost.ActualHeight));
            double slack = 4 * (bottomRight.X - topLeft.X) / IslandHost.ActualWidth;
            isInside = cursor.X >= topLeft.X - slack && cursor.X <= bottomRight.X + slack &&
                       cursor.Y >= topLeft.Y - slack && cursor.Y <= bottomRight.Y + slack;
        }
        // Keep the player open while dragging the progress bar outside it.
        isInside |= System.Windows.Input.Mouse.Captured != null && _model.IsHovering;

        if (isInside && !_model.IsHovering)
        {
            // A short delay so moving the pointer past the top of the screen doesn't pop the player open.
            _hoverStart ??= DateTime.UtcNow;
            if ((DateTime.UtcNow - _hoverStart.Value).TotalSeconds >= Theme.Island.HoverDelay)
            {
                _model.IsHovering = true;
            }
        }
        else if (!isInside)
        {
            _hoverStart = null;
            _model.IsHovering = false;
        }

        SetClickThrough(!_model.IsHovering);
    }

    private void SetClickThrough(bool enabled)
    {
        if (_hwnd == IntPtr.Zero || _isClickThrough == enabled) return;
        _isClickThrough = enabled;
        NativeMethods.SetExtendedStyle(_hwnd, NativeMethods.WS_EX_TRANSPARENT, enabled);
    }

    private void HideDuringFullScreenApps()
    {
        if ((DateTime.UtcNow - _lastFullScreenCheck).TotalSeconds < 1) return;
        _lastFullScreenCheck = DateTime.UtcNow;
        var visibility = NativeMethods.IsFullScreenAppActive() ? Visibility.Hidden : Visibility.Visible;
        if (Root.Visibility != visibility) Root.Visibility = visibility;
    }

    // Frame loop

    private void StartTicking()
    {
        if (_isTicking) return;
        _isTicking = true;
        _lastFrameTime = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var time = e is RenderingEventArgs rendering ? rendering.RenderingTime : TimeSpan.Zero;
        if (time == _lastFrameTime) return; // Rendering can fire more than once per frame.
        double seconds = _lastFrameTime == TimeSpan.Zero ? 1 / 60.0 : (time - _lastFrameTime).TotalSeconds;
        _lastFrameTime = time;

        bool isSettled = Tick(Math.Clamp(seconds, 0, 0.1));
        // Stop redrawing when hidden and still, so an idle island uses no CPU.
        if (isSettled && _model.Mode == IslandMode.Idle)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isTicking = false;
        }
    }

    /// <summary>Advances every animation by <paramref name="seconds"/>. Returns true when nothing is moving.</summary>
    private bool Tick(double seconds)
    {
        var mode = _model.Mode;
        var size = _model.SizeFor(mode);
        bool isOpen = mode is not (IslandMode.Idle or IslandMode.Compact);
        var corners = isOpen ? Theme.Island.ExpandedCorners : Theme.Island.CompactCorners;
        var (response, damping) = mode is IslandMode.Peek or IslandMode.Activity ? Theme.Motion.Bounce : Theme.Motion.Morph;

        if (mode != _lastMode)
        {
            if (mode == IslandMode.Lyrics) LyricsLines.SnapToCurrentLine();
            _lastMode = mode;
        }

        // Shape
        _width.Target = size.Width;
        _height.Target = size.Height;
        _cornerTop.Target = corners.Top;
        _cornerBottom.Target = corners.Bottom;
        foreach (var spring in new[] { _width, _height, _cornerTop, _cornerBottom }) spring.Step(seconds, response, damping);

        double width = Math.Max(1, _width.Value), height = Math.Max(1, _height.Value);
        IslandHost.Width = width;
        IslandHost.Height = height;
        var shape = NotchShape.Create(width, height, new NotchCorners(_cornerTop.Value, _cornerBottom.Value));
        Island.Clip = shape;
        Outline.Data = shape;

        bool isSettled = _width.IsSettled && _height.IsSettled && _cornerTop.IsSettled && _cornerBottom.IsSettled;

        // With nothing to show the island disappears; the hover area stays where it was.
        isSettled &= Fade(IslandHost, mode == IslandMode.Idle ? 0 : 1, seconds);
        Shadow.Opacity = Approach(Shadow.Opacity, isOpen ? 0.45 : 0, seconds);

        isSettled &= UpdateBackground(isOpen, seconds);

        foreach (var (panel, panelMode) in _panels)
        {
            isSettled &= Fade(panel, panelMode == mode ? 1 : 0, seconds);
            panel.Visibility = panel.Opacity > 0 ? Visibility.Visible : Visibility.Collapsed;
            panel.IsHitTestVisible = panelMode == mode;
        }
        ExpandedScale.ScaleX = ExpandedScale.ScaleY = 0.9 + 0.1 * ExpandedPanel.Opacity;
        ExpandedPanel.Height = _model.SizeFor(IslandMode.Expanded).Height;

        UpdateContent();
        return isSettled;
    }

    private bool UpdateBackground(bool isOpen, double seconds)
    {
        var theme = _model.Preferences.Theme;
        var nowPlaying = _model.NowPlaying;
        var accent = nowPlaying.Accent;
        bool isSettled = true;

        isSettled &= Fade(SolidBackground, isOpen && theme == IslandTheme.Glass ? 0 : 1, seconds);
        isSettled &= Fade(GlassBackground, isOpen && theme == IslandTheme.Glass ? 1 : 0, seconds);
        isSettled &= Fade(ArtworkBackground, isOpen && theme == IslandTheme.NowBar && nowPlaying.Artwork != null ? 1 : 0, seconds);
        isSettled &= Fade(AccentTint, isOpen && theme == IslandTheme.NowBar && nowPlaying.Artwork == null
            ? Theme.Colors.AlbumTintOpacity : 0, seconds);
        isSettled &= Fade(TopFade, isOpen ? 1 : 0, seconds);
        if (!ReferenceEquals(BlurredArtwork.Source, nowPlaying.Artwork)) BlurredArtwork.Source = nowPlaying.Artwork;

        // Neon: a glowing edge in the album's color. Glass: a faint light edge.
        bool hasOutline = isOpen && theme is IslandTheme.Neon or IslandTheme.Glass;
        isSettled &= Fade(Outline, hasOutline ? 1 : 0, seconds);
        if (accent != _brushColor || AccentTint.Fill == null)
        {
            _brushColor = accent;
            AccentTint.Fill = Text.Solid(accent);
        }
        if (theme == IslandTheme.Glass)
        {
            Outline.Stroke = Text.White(0.15);
            Outline.StrokeThickness = 1;
            OutlineGlow.Opacity = 0;
        }
        else
        {
            Outline.Stroke = Text.Solid(accent);
            Outline.StrokeThickness = 1.5;
            OutlineGlow.Color = accent;
            OutlineGlow.Opacity = 1;
        }
        return isSettled;
    }

    /// <summary>Fills in the visible panels and moves their animations along.</summary>
    private void UpdateContent()
    {
        var nowPlaying = _model.NowPlaying;
        var preferences = _model.Preferences;
        var track = nowPlaying.Track;
        string title = track?.Title ?? "";
        string artist = track?.Artist ?? "";
        var accent = nowPlaying.Accent;

        foreach (var artwork in new[] { CompactArtwork, PeekArtwork, ExpandedArtwork, LyricsArtwork })
        {
            artwork.IsSpinning = preferences.SpinArtwork;
            if (IsShown(artwork)) artwork.Animate();
        }

        if (IsShown(CompactPanel))
        {
            CompactTitle.Text = title;
            CompactTitle.Animate();
            UpdateWaves(CompactWaves);
        }

        if (IsShown(PeekPanel))
        {
            PeekTitle.Text = title;
            PeekArtist.Text = artist;
        }

        if (IsShown(ActivityPanel) && _model.Activity is { } activity)
        {
            ActivityIcon.Text = activity.Glyph;
            ActivityTitle.Text = activity.Title;
            ActivityLevel.Visibility = activity.Level.HasValue ? Visibility.Visible : Visibility.Collapsed;
            double fill = 90 * Math.Clamp(activity.Level ?? 0, 0, 1);
            ActivityLevelFill.Width = Approach(double.IsNaN(ActivityLevelFill.Width) ? fill : ActivityLevelFill.Width, fill, 1 / 60.0, rate: 8);
        }

        if (IsShown(ExpandedPanel))
        {
            ExpandedTitle.Text = artist.Length == 0 ? title : $"{title} • {artist}";
            ExpandedTitle.Animate();
            PlayPauseButton.Content = nowPlaying.IsPlaying ? Theme.Glyphs.Pause : Theme.Glyphs.Play;

            LyricLineRow.Visibility = preferences.ShowLyrics ? Visibility.Visible : Visibility.Collapsed;
            LyricsIcon.Visibility = nowPlaying.Lyrics != null ? Visibility.Visible : Visibility.Collapsed;
            string line = nowPlaying.Lyrics?.LineAt(nowPlaying.Position(DateTime.UtcNow)) ?? "";
            ShowLyricLine(line.Length == 0 ? "♪" : line, accent);

            Progress.Animate();
            BottomWaves.Visibility = preferences.Theme.ShowsBottomWaves() ? Visibility.Visible : Visibility.Collapsed;
            UpdateWaves(BottomWaves);
        }

        if (IsShown(LyricsPanel))
        {
            LyricsTitle.Text = title;
            LyricsTitle.Animate();
            LyricsLines.Animate();
        }
    }

    private void UpdateWaves(WaveView waves)
    {
        waves.WaveStyle = _model.Preferences.WaveStyle;
        waves.Color = _model.NowPlaying.Accent;
        waves.IsPlaying = _model.NowPlaying.IsPlaying;
        waves.Animate();
    }

    /// <summary>The new line slides up from below, like the macOS version.</summary>
    private void ShowLyricLine(string line, Color accent)
    {
        LyricLine.Foreground = Text.Solid(accent);
        if (line == _lyricLine) return;
        _lyricLine = line;
        LyricLine.Text = line;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        LyricLineShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromSeconds(0.3)) { EasingFunction = ease });
        LyricLine.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)) { EasingFunction = ease });
    }

    private bool IsShown(FrameworkElement element)
    {
        for (DependencyObject? current = element; current != null && current != Island; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: not Visibility.Visible }) return false;
        }
        return true;
    }

    /// <summary>Moves the element's opacity toward <paramref name="target"/>. Returns true once it's there.</summary>
    private static bool Fade(UIElement element, double target, double seconds)
    {
        double from = Math.Abs(target) < 1e-9 ? 1 : target;
        double step = seconds / Theme.Motion.Fade * from;
        double opacity = element.Opacity;
        opacity = opacity < target ? Math.Min(target, opacity + step) : Math.Max(target, opacity - step);
        if (opacity != element.Opacity) element.Opacity = opacity;
        return Math.Abs(opacity - target) < 1e-6;
    }

    private static double Approach(double value, double target, double seconds, double rate = 6) =>
        value + (target - value) * Math.Min(1, seconds * rate);
}
