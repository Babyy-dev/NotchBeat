namespace NotchBeat;

/// <param name="Id">Identifies the song within its app; changes when the song changes.</param>
/// <param name="AppId">The Windows app ID of the app playing it, e.g. <c>Spotify.exe</c>.</param>
/// <param name="Duration">Length in seconds, or 0 when the app doesn't report it.</param>
public sealed record Track(string Id, string AppId, string Title, string Artist, string Album, double Duration);

public enum PlayerCommand
{
    PlayPause,
    Next,
    Previous,
}
