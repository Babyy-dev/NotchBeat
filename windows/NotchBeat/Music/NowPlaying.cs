using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using MediaManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;
using MediaSession = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;

namespace NotchBeat;

/// <summary>
/// The single source of truth for what is playing. Views read from it; it reads from Windows' media sessions,
/// the same ones behind the media controls in the volume flyout. Spotify, Apple Music, browsers and most
/// other players report there, so no app needs special code.
/// </summary>
public sealed class NowPlaying
{
    public Track? Track { get; private set; }
    public bool IsPlaying { get; private set; }
    /// <summary>System volume, 0...100.</summary>
    public double Volume { get; private set; } = 50;
    public BitmapSource? Artwork { get; private set; }
    public Color Accent { get; private set; } = Theme.Colors.FallbackAccent;
    public Lyrics? Lyrics { get; private set; }

    /// <summary>Raised whenever anything above changes.</summary>
    public event Action? Changed;
    /// <summary>Raised when a new song starts.</summary>
    public event Action? TrackChanged;

    public AudioVisualizer Visualizer { get; } = new();

    private readonly Preferences _preferences;
    private readonly SystemAudio _systemAudio;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _pollTimer;
    private MediaManager? _manager;
    private MediaSession? _activeSession;
    private string? _lyricsTrackId;
    private string? _artworkTrackId;
    private bool _isRefreshing;
    private bool _needsAnotherRefresh;

    /// <summary>Last known position and when it was read; the current position is extrapolated from it.</summary>
    private (double Position, DateTime Date) _anchor = (0, DateTime.UtcNow);

    public NowPlaying(Preferences preferences, SystemAudio systemAudio, Dispatcher dispatcher)
    {
        _preferences = preferences;
        _systemAudio = systemAudio;
        _dispatcher = dispatcher;
        Volume = systemAudio.Volume;
        // Catches apps that start playing while another app's session is shown.
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _pollTimer.Tick += (_, _) => RequestRefresh();
    }

    public async void Start()
    {
        try
        {
            _manager = await MediaManager.RequestAsync();
            _manager.SessionsChanged += (_, _) => _dispatcher.BeginInvoke(RequestRefresh);
            _manager.CurrentSessionChanged += (_, _) => _dispatcher.BeginInvoke(RequestRefresh);
        }
        catch (Exception)
        {
            _manager = null; // Media sessions unavailable (very old Windows 10).
        }
        _pollTimer.Start();
        RequestRefresh();
    }

    public void PreferencesChanged()
    {
        RequestRefresh();
        UpdateLyrics();
    }

    /// <summary>The song position in seconds at <paramref name="date"/> (UTC).</summary>
    public double Position(DateTime date)
    {
        if (Track == null) return 0;
        double elapsed = IsPlaying ? (date - _anchor.Date).TotalSeconds : 0;
        double position = _anchor.Position + elapsed;
        return Track.Duration > 0 ? Math.Clamp(position, 0, Track.Duration) : Math.Max(position, 0);
    }

    public async void Perform(PlayerCommand command)
    {
        var session = _activeSession;
        if (session == null) return;

        if (command == PlayerCommand.PlayPause)
        {
            // Update instantly; the session's change event confirms shortly after.
            _anchor = (Position(DateTime.UtcNow), DateTime.UtcNow);
            IsPlaying = !IsPlaying;
            Changed?.Invoke();
        }

        try
        {
            _ = command switch
            {
                PlayerCommand.PlayPause => await session.TryTogglePlayPauseAsync(),
                PlayerCommand.Next => await session.TrySkipNextAsync(),
                _ => await session.TrySkipPreviousAsync(),
            };
        }
        catch (Exception)
        {
            // The app closed in the meantime.
        }
    }

    public async void Seek(double position)
    {
        _anchor = (position, DateTime.UtcNow);
        Changed?.Invoke();
        try
        {
            if (_activeSession != null) await _activeSession.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(position).Ticks);
        }
        catch (Exception)
        {
            // Some apps don't support seeking.
        }
    }

    public void SetVolume(double newValue)
    {
        double clamped = Math.Clamp(newValue, 0, 100);
        bool didChangeStep = (int)Math.Round(clamped) != (int)Math.Round(Volume);
        Volume = clamped;
        if (didChangeStep) _systemAudio.Volume = Math.Round(clamped);
        Changed?.Invoke();
    }

    // Reading state

    private async void RequestRefresh()
    {
        // Refreshes are async; run one at a time, and once more if something changed meanwhile.
        if (_isRefreshing)
        {
            _needsAnotherRefresh = true;
            return;
        }
        _isRefreshing = true;
        try
        {
            do
            {
                _needsAnotherRefresh = false;
                await Refresh();
            } while (_needsAnotherRefresh);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async Task Refresh()
    {
        Volume = _systemAudio.Volume; // It can also change from the keyboard or the taskbar.

        var sessions = _manager?.GetSessions()
            .Where(session => _preferences.Source.Allows(session.SourceAppUserModelId))
            .ToList() ?? new();

        // Prefer whatever is playing, then the app we were already showing, then the one Windows picks.
        string? activeId = _activeSession?.SourceAppUserModelId;
        string? currentId = TryGet(() => _manager?.GetCurrentSession()?.SourceAppUserModelId);
        var chosen = sessions.FirstOrDefault(IsSessionPlaying)
            ?? sessions.FirstOrDefault(session => session.SourceAppUserModelId == activeId)
            ?? sessions.FirstOrDefault(session => session.SourceAppUserModelId == currentId)
            ?? sessions.FirstOrDefault();

        SetActiveSession(chosen);

        GlobalSystemMediaTransportControlsSessionMediaProperties? properties = null;
        if (chosen != null)
        {
            try { properties = await chosen.TryGetMediaPropertiesAsync(); }
            catch (Exception) { properties = null; }
        }

        if (chosen == null || properties == null || string.IsNullOrWhiteSpace(properties.Title))
        {
            Apply(null);
        }
        else
        {
            Apply(new State(chosen, properties));
        }

        Visualizer.Update(IsPlaying && _preferences.ReactiveWaves);
        Changed?.Invoke();
    }

    private sealed class State
    {
        public Track Track { get; }
        public bool IsPlaying { get; }
        public double Position { get; }
        public DateTime PositionDate { get; }
        public GlobalSystemMediaTransportControlsSessionMediaProperties Properties { get; }

        public State(MediaSession session, GlobalSystemMediaTransportControlsSessionMediaProperties properties)
        {
            Properties = properties;
            var timeline = TryGet(session.GetTimelineProperties);
            double duration = timeline == null ? 0 : Math.Max(0, (timeline.EndTime - timeline.StartTime).TotalSeconds);

            string artist = string.IsNullOrWhiteSpace(properties.Artist) ? properties.AlbumArtist : properties.Artist;
            string appId = session.SourceAppUserModelId;
            Track = new Track(
                Id: $"{appId}|{properties.Title}|{artist}|{properties.AlbumTitle}",
                AppId: appId,
                Title: properties.Title,
                Artist: artist ?? "",
                Album: properties.AlbumTitle ?? "",
                Duration: duration);
            IsPlaying = IsSessionPlaying(session);

            Position = timeline == null ? 0 : (timeline.Position - timeline.StartTime).TotalSeconds;
            // Apps report the position along with when they last updated it; older updates are extrapolated.
            var updated = timeline?.LastUpdatedTime.UtcDateTime ?? DateTime.UtcNow;
            PositionDate = updated.Year < 2000 || updated > DateTime.UtcNow ? DateTime.UtcNow : updated;
        }
    }

    private void Apply(State? state)
    {
        if (state == null)
        {
            Track = null;
            IsPlaying = false;
            Artwork = null;
            Accent = Theme.Colors.FallbackAccent;
            Lyrics = null;
            _lyricsTrackId = null;
            _artworkTrackId = null;
            return;
        }

        bool isNewTrack = state.Track.Id != Track?.Id;
        Track = state.Track;
        IsPlaying = state.IsPlaying;
        _anchor = (state.Position, state.PositionDate);

        if (isNewTrack)
        {
            Artwork = null;
            Accent = Theme.Colors.FallbackAccent;
            Lyrics = null;
            _artworkTrackId = null;
            UpdateLyrics();
            TrackChanged?.Invoke();
        }

        // Some apps send the cover a moment after the title, so keep trying until it arrives.
        if (Artwork == null && _artworkTrackId != state.Track.Id && state.Properties.Thumbnail != null)
        {
            _artworkTrackId = state.Track.Id;
            LoadArtwork(state.Track, state.Properties);
        }
    }

    private async void LoadArtwork(Track track, GlobalSystemMediaTransportControlsSessionMediaProperties properties)
    {
        BitmapSource? image = null;
        try
        {
            using var stream = await properties.Thumbnail.OpenReadAsync();
            using var input = stream.AsStreamForRead();
            using var memory = new MemoryStream();
            await input.CopyToAsync(memory);
            memory.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memory;
            bitmap.EndInit();
            bitmap.Freeze();
            image = bitmap;
        }
        catch (Exception)
        {
            _artworkTrackId = null; // Try again on the next refresh.
            return;
        }

        if (Track?.Id != track.Id) return; // The song changed meanwhile.
        Artwork = image;
        Accent = ArtworkPalette.AccentColor(image) ?? Theme.Colors.FallbackAccent;
        Changed?.Invoke();
    }

    private async void UpdateLyrics()
    {
        var track = Track;
        if (!_preferences.ShowLyrics || track == null)
        {
            Lyrics = null;
            _lyricsTrackId = null;
            Changed?.Invoke();
            return;
        }
        if (_lyricsTrackId == track.Id) return;
        _lyricsTrackId = track.Id;

        var result = await LrcLibClient.LyricsFor(track);
        if (_lyricsTrackId != track.Id) return;
        Lyrics = result;
        Changed?.Invoke();
    }

    // Session events

    private void SetActiveSession(MediaSession? session)
    {
        if (_activeSession?.SourceAppUserModelId == session?.SourceAppUserModelId && _activeSession != null)
        {
            return;
        }

        if (_activeSession != null)
        {
            _activeSession.PlaybackInfoChanged -= OnSessionChanged;
            _activeSession.MediaPropertiesChanged -= OnSessionChanged;
            _activeSession.TimelinePropertiesChanged -= OnSessionChanged;
        }
        _activeSession = session;
        if (session != null)
        {
            session.PlaybackInfoChanged += OnSessionChanged;
            session.MediaPropertiesChanged += OnSessionChanged;
            session.TimelinePropertiesChanged += OnSessionChanged;
        }
    }

    private void OnSessionChanged(MediaSession sender, object args) => _dispatcher.BeginInvoke(RequestRefresh);

    private static bool IsSessionPlaying(MediaSession session) =>
        TryGet(session.GetPlaybackInfo)?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

    /// <summary>Session calls throw when the app has just closed; treat that as "nothing".</summary>
    private static T? TryGet<T>(Func<T?> read) where T : class
    {
        try { return read(); }
        catch (Exception) { return null; }
    }
}
