import AppKit

@MainActor
final class AppleMusicPlayer: MusicPlayer {
    nonisolated static let bundleID = "com.apple.Music"

    let app = ScriptableApp(
        name: "Music",
        bundleID: bundleID,
        changeNotification: "com.apple.Music.playerInfo"
    )

    func fetchState() -> PlayerState? {
        guard
            let list = app.tell("""
                set t to current track
                return {player state as string, persistent ID of t, name of t, artist of t, album of t, ¬
                    duration of t, player position, sound volume}
                """),
            list.numberOfItems == 8
        else { return nil }

        let track = Track(
            id: list.atIndex(2)?.stringValue ?? "",
            title: list.atIndex(3)?.stringValue ?? "",
            artist: list.atIndex(4)?.stringValue ?? "",
            album: list.atIndex(5)?.stringValue ?? "",
            artworkURL: nil,
            duration: list.atIndex(6)?.doubleValue ?? 0 // Apple Music reports seconds.
        )
        return PlayerState(
            track: track,
            isPlaying: list.atIndex(1)?.stringValue == "playing",
            position: list.atIndex(7)?.doubleValue ?? 0,
            volume: Int(list.atIndex(8)?.int32Value ?? 50)
        )
    }

    func artwork(for track: Track) async -> NSImage? {
        guard let data = app.tell("return raw data of artwork 1 of current track")?.data else { return nil }
        return NSImage(data: data)
    }
}
