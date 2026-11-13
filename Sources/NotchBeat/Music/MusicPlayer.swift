import AppKit

enum PlayerCommand {
    case playPause, next, previous

    fileprivate var script: String {
        switch self {
        case .playPause: "playpause"
        case .next: "next track"
        case .previous: "previous track"
        }
    }
}

/// A music app the island can show and control.
/// To add a new app, conform to this and add it to `NowPlaying.players`.
@MainActor
protocol MusicPlayer: AnyObject, Sendable {
    var app: ScriptableApp { get }
    /// Returns `nil` when the app is closed or has nothing loaded.
    func fetchState() -> PlayerState?
    func artwork(for track: Track) async -> NSImage?
}

/// Commands shared by every AppleScript-controlled player.
extension MusicPlayer {
    var bundleID: String { app.bundleID }

    func startObserving(onChange: @escaping @MainActor @Sendable () -> Void) {
        app.observeChanges(onChange)
    }

    func send(_ command: PlayerCommand) {
        app.tell(command.script)
    }

    func seek(to position: TimeInterval) {
        app.tell("set player position to \(String(format: "%.2f", position))")
    }

    func setVolume(_ volume: Int) {
        app.tell("set sound volume to \(volume)")
    }
}
