import AppKit

/// Creates the long-lived objects and wires them together.
@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    private let preferences = Preferences()
    private lazy var nowPlaying = NowPlaying(preferences: preferences)
    private var islandController: IslandWindowController?
    private var statusItemController: StatusItemController?
    private var audioOutputMonitor: AudioOutputMonitor?
    private var powerMonitor: PowerMonitor?

    func applicationDidFinishLaunching(_ notification: Notification) {
        let island = IslandModel(nowPlaying: nowPlaying, preferences: preferences)
        islandController = IslandWindowController(model: island)
        statusItemController = StatusItemController(preferences: preferences)
        audioOutputMonitor = AudioOutputMonitor { [weak island] in island?.show($0) }
        powerMonitor = PowerMonitor { [weak island] in island?.show($0) }

        preferences.onChange = { [nowPlaying] in nowPlaying.preferencesChanged() }
        nowPlaying.start()
    }
}
