import AppKit

@MainActor
final class SpotifyPlayer: MusicPlayer {
    nonisolated static let bundleID = "com.spotify.client"

    let app = ScriptableApp(
        name: "Spotify",
        bundleID: bundleID,
        changeNotification: "com.spotify.client.PlaybackStateChanged"
    )

    func fetchState() -> PlayerState? {
        guard
            let list = app.tell("""
                set t to current track
                return {player state as string, id of t, name of t, artist of t, album of t, ¬
                    duration of t, player position, sound volume, artwork url of t}
                """),
            list.numberOfItems == 9
        else { return nil }

        let track = Track(
            id: list.atIndex(2)?.stringValue ?? "",
            title: list.atIndex(3)?.stringValue ?? "",
            artist: list.atIndex(4)?.stringValue ?? "",
            album: list.atIndex(5)?.stringValue ?? "",
            artworkURL: list.atIndex(9)?.stringValue.flatMap(URL.init(string:)),
            duration: TimeInterval(list.atIndex(6)?.int32Value ?? 0) / 1000 // Spotify reports milliseconds.
        )
        return PlayerState(
            track: track,
            isPlaying: list.atIndex(1)?.stringValue == "playing",
            position: list.atIndex(7)?.doubleValue ?? 0,
            volume: Int(list.atIndex(8)?.int32Value ?? 50)
        )
    }

    func artwork(for track: Track) async -> NSImage? {
        guard let url = track.artworkURL, let (data, _) = try? await URLSession.shared.data(from: url) else {
            return nil
        }
        return NSImage(data: data)
    }
}
