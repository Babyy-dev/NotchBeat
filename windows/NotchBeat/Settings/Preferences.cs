using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace NotchBeat;

/// <summary>Which app the island follows.</summary>
public enum MusicSource
{
    /// <summary>Anything that reports what it's playing to Windows: Spotify, Apple Music, browsers, and more.</summary>
    Automatic,
    Spotify,
    AppleMusic,
}

/// <summary>Where the island sits along the top of the screen.</summary>
public enum IslandPosition
{
    TopLeft,
    TopCenter,
    TopRight,
}

public static class PreferenceTitles
{
    public static string Title(this MusicSource source) => source switch
    {
        MusicSource.Automatic => "Automatic (any app)",
        MusicSource.Spotify => "Spotify",
        MusicSource.AppleMusic => "Apple Music",
        _ => source.ToString(),
    };

    public static string Title(this IslandPosition position) => position switch
    {
        IslandPosition.TopLeft => "Top left",
        IslandPosition.TopCenter => "Top center",
        IslandPosition.TopRight => "Top right",
        _ => position.ToString(),
    };

    /// <summary>Whether the island should follow the app with this Windows app ID.</summary>
    public static bool Allows(this MusicSource source, string appId)
    {
        string id = appId.ToLowerInvariant();
        return source switch
        {
            MusicSource.Spotify => id.Contains("spotify"),
            MusicSource.AppleMusic => id.Contains("applemusic") || id.Contains("itunes"),
            _ => true,
        };
    }
}

/// <summary>User settings, saved as JSON in %APPDATA%\NotchBeat\settings.json.</summary>
public sealed class Preferences
{
    private static readonly string FolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotchBeat");
    private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "NotchBeat";

    /// <summary>Raised after any setting changes.</summary>
    public event Action? Changed;

    private Data _data;

    private Preferences(Data data) => _data = data;

    public MusicSource Source { get => _data.Source; set => Set(ref _data.Source, value); }
    public IslandTheme Theme { get => _data.Theme; set => Set(ref _data.Theme, value); }
    public WaveStyle WaveStyle { get => _data.WaveStyle; set => Set(ref _data.WaveStyle, value); }
    public bool ReactiveWaves { get => _data.ReactiveWaves; set => Set(ref _data.ReactiveWaves, value); }
    public bool SpinArtwork { get => _data.SpinArtwork; set => Set(ref _data.SpinArtwork, value); }
    public bool ShowLyrics { get => _data.ShowLyrics; set => Set(ref _data.ShowLyrics, value); }
    public bool ShowSystemActivity { get => _data.ShowSystemActivity; set => Set(ref _data.ShowSystemActivity, value); }
    public IslandPosition Position { get => _data.Position; set => Set(ref _data.Position, value); }
    /// <summary>Distance from the screen edge, in pixels, for the left and right positions.</summary>
    public double EdgeMargin { get => _data.EdgeMargin; set => Set(ref _data.EdgeMargin, Math.Clamp(value, 0, 600)); }
    /// <summary>Device name of the screen to show on (e.g. <c>\\.\DISPLAY2</c>), or empty for the main screen.</summary>
    public string Display { get => _data.Display; set => Set(ref _data.Display, value ?? ""); }

    public bool LaunchAtLogin
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(RunValueName) != null;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (value && Environment.ProcessPath is { } path) key.SetValue(RunValueName, $"\"{path}\"");
            else key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    public static Preferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var data = JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath), JsonOptions);
                if (data != null) return new Preferences(data);
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            // Unreadable settings: start fresh.
        }
        return new Preferences(new Data());
    }

    private void Set<T>(ref T field, T value)
    {
        if (Equals(field, value)) return;
        field = value;
        Save();
        Changed?.Invoke();
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data, JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Settings still apply for this session.
        }
    }

    /// <summary>The saved fields. Public fields so they can be passed by reference.</summary>
    public sealed class Data
    {
        [JsonInclude] public MusicSource Source = MusicSource.Automatic;
        [JsonInclude] public IslandTheme Theme = IslandTheme.NowBar;
        [JsonInclude] public WaveStyle WaveStyle = WaveStyle.Flow;
        [JsonInclude] public bool ReactiveWaves = true;
        [JsonInclude] public bool SpinArtwork = true;
        [JsonInclude] public bool ShowLyrics = true;
        [JsonInclude] public bool ShowSystemActivity = true;
        [JsonInclude] public IslandPosition Position = IslandPosition.TopCenter;
        [JsonInclude] public double EdgeMargin = NotchBeat.Theme.Island.DefaultEdgeMargin;
        [JsonInclude] public string Display = "";
    }
}
