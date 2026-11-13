using System;
using System.Windows;
using System.Windows.Threading;

namespace NotchBeat;

public enum IslandMode
{
    /// <summary>Nothing to show: the island is hidden.</summary>
    Idle,
    /// <summary>Music playing: artwork on the left, title in the middle, waves on the right.</summary>
    Compact,
    /// <summary>Short pop with the new song's title after a track change.</summary>
    Peek,
    /// <summary>Short pop for a system event: volume, audio device, charging.</summary>
    Activity,
    /// <summary>Hovered: full player.</summary>
    Expanded,
    /// <summary>Hovered, with the karaoke lyrics open.</summary>
    Lyrics,
}

/// <summary>Decides which shape the island takes and how big it is.</summary>
public sealed class IslandModel
{
    /// <summary>The largest the island can get; the window is sized to fit it.</summary>
    public static Size LargestSize => new(
        Math.Max(Math.Max(Theme.Island.ExpandedSize.Width, Theme.Island.LyricsSize.Width), Theme.Island.PeekWidth),
        Math.Max(Theme.Island.ExpandedSize.Height + Theme.Island.LyricsRowHeight, Theme.Island.LyricsSize.Height));

    public NowPlaying NowPlaying { get; }
    public Preferences Preferences { get; }

    /// <summary>Raised whenever <see cref="Mode"/> or anything shown may have changed.</summary>
    public event Action? Changed;

    private bool _isHovering;
    private bool _isShowingFullLyrics;
    private double? _scrubFraction;
    private readonly DispatcherTimer _peekTimer;
    private readonly DispatcherTimer _volumeTimer;
    private readonly DispatcherTimer _activityTimer;

    public IslandModel(NowPlaying nowPlaying, Preferences preferences)
    {
        NowPlaying = nowPlaying;
        Preferences = preferences;
        _peekTimer = OneShot(() => IsPeeking = false);
        _volumeTimer = OneShot(() => IsShowingVolume = false);
        _activityTimer = OneShot(() => Activity = null);

        nowPlaying.TrackChanged += Peek;
        nowPlaying.Changed += () => Changed?.Invoke();
        preferences.Changed += () => Changed?.Invoke();
    }

    public bool IsHovering
    {
        get => _isHovering;
        set
        {
            if (_isHovering == value) return;
            _isHovering = value;
            if (!value) _isShowingFullLyrics = false;
            Changed?.Invoke();
        }
    }

    public bool IsShowingFullLyrics
    {
        get => _isShowingFullLyrics;
        set { _isShowingFullLyrics = value; Changed?.Invoke(); }
    }

    /// <summary>Progress-bar position (0...1) while the user drags it, otherwise <c>null</c>.</summary>
    public double? ScrubFraction
    {
        get => _scrubFraction;
        set { _scrubFraction = value; Changed?.Invoke(); }
    }

    public bool IsPeeking { get; private set; }
    public bool IsShowingVolume { get; private set; }
    public SystemActivity? Activity { get; private set; }

    public IslandMode Mode
    {
        get
        {
            bool hasTrack = NowPlaying.Track != null;
            if (IsHovering && hasTrack) return IsShowingFullLyrics ? IslandMode.Lyrics : IslandMode.Expanded;
            if (Activity != null) return IslandMode.Activity;
            if (!hasTrack) return IslandMode.Idle;
            if (IsPeeking) return IslandMode.Peek;
            return NowPlaying.IsPlaying ? IslandMode.Compact : IslandMode.Idle;
        }
    }

    public static Size CompactSize => new(Theme.Island.BarWidth + 2 * Theme.Island.CompactWingWidth, Theme.Island.BarHeight);

    public Size SizeFor(IslandMode mode) => mode switch
    {
        IslandMode.Idle => new Size(Theme.Island.BarWidth, Theme.Island.BarHeight),
        IslandMode.Compact => CompactSize,
        IslandMode.Peek => new Size(Theme.Island.PeekWidth, Theme.Island.PeekHeight),
        IslandMode.Activity => new Size(Theme.Island.PeekWidth, Theme.Island.ActivityHeight),
        IslandMode.Expanded => new Size(
            Theme.Island.ExpandedSize.Width,
            Theme.Island.ExpandedSize.Height + (Preferences.ShowLyrics ? Theme.Island.LyricsRowHeight : 0)),
        _ => Theme.Island.LyricsSize,
    };

    public void AdjustVolume(double delta)
    {
        NowPlaying.SetVolume(NowPlaying.Volume + delta);
        IsShowingVolume = true;
        Restart(_volumeTimer, Theme.Gestures.VolumeIndicatorDuration);
        Changed?.Invoke();
    }

    public void Show(SystemActivity activity)
    {
        if (!Preferences.ShowSystemActivity) return;
        // The open player already shows its own volume bar.
        if (activity.Kind == SystemActivity.ActivityKind.Volume && Mode is IslandMode.Expanded or IslandMode.Lyrics) return;

        Activity = activity;
        Restart(_activityTimer, activity.Kind == SystemActivity.ActivityKind.Volume
            ? Theme.Island.VolumeActivityDuration
            : Theme.Island.ActivityDuration);
        Changed?.Invoke();
    }

    private void Peek()
    {
        IsPeeking = true;
        Restart(_peekTimer, Theme.Island.PeekDuration);
        Changed?.Invoke();
    }

    private DispatcherTimer OneShot(Action action)
    {
        var timer = new DispatcherTimer();
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            action();
            Changed?.Invoke();
        };
        return timer;
    }

    private static void Restart(DispatcherTimer timer, double seconds)
    {
        timer.Stop();
        timer.Interval = TimeSpan.FromSeconds(seconds);
        timer.Start();
    }
}
