using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NotchBeat;

/// <summary>Looks up synced lyrics on lrclib.net, a free and open lyrics database. No API key needed.</summary>
public static class LrcLibClient
{
    private sealed record Result([property: JsonPropertyName("syncedLyrics")] string? SyncedLyrics);

    private static readonly HttpClient Http = CreateClient();

    public static async Task<Lyrics?> LyricsFor(Track track)
    {
        // Exact match first, then a looser search when the album or duration differ.
        var exact = await Fetch<Result>("get", new Dictionary<string, string>
        {
            ["track_name"] = track.Title,
            ["artist_name"] = track.Artist,
            ["album_name"] = track.Album,
            ["duration"] = Math.Round(track.Duration).ToString(CultureInfo.InvariantCulture),
        });
        if (Lyrics.Parse(exact?.SyncedLyrics) is { } lyrics) return lyrics;

        var results = await Fetch<Result[]>("search", new Dictionary<string, string>
        {
            ["track_name"] = track.Title,
            ["artist_name"] = track.Artist,
        });
        return results?.Select(result => Lyrics.Parse(result.SyncedLyrics)).FirstOrDefault(found => found != null);
    }

    private static async Task<T?> Fetch<T>(string path, Dictionary<string, string> query) where T : class
    {
        string queryString = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        try
        {
            using var response = await Http.GetAsync($"https://lrclib.net/api/{path}?{queryString}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // LRCLIB asks clients to identify themselves.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NotchBeat/0.2 (open-source Windows notch player)");
        return client;
    }
}
