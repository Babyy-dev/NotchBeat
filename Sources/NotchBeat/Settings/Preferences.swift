import Foundation
import ServiceManagement

/// Which music app the island follows.
enum MusicSource: String, CaseIterable, Identifiable {
    case automatic, spotify, appleMusic

    var id: Self { self }

    var title: String {
        switch self {
        case .automatic: "Automatic"
        case .spotify: "Spotify"
        case .appleMusic: "Apple Music"
        }
    }

    @MainActor
    func allows(_ player: any MusicPlayer) -> Bool {
        switch self {
        case .automatic: true
        case .spotify: player.bundleID == SpotifyPlayer.bundleID
        case .appleMusic: player.bundleID == AppleMusicPlayer.bundleID
        }
    }
}

/// User settings, saved in UserDefaults.
@MainActor
@Observable
final class Preferences {
    var source: MusicSource { didSet { save(source.rawValue, Key.source) } }
    var theme: IslandTheme { didSet { save(theme.rawValue, Key.theme) } }
    var waveStyle: WaveStyle { didSet { save(waveStyle.rawValue, Key.waveStyle) } }
    var reactiveWaves: Bool { didSet { save(reactiveWaves, Key.reactiveWaves) } }
    var spinArtwork: Bool { didSet { save(spinArtwork, Key.spinArtwork) } }
    var showLyrics: Bool { didSet { save(showLyrics, Key.showLyrics) } }
    var showSystemActivity: Bool { didSet { save(showSystemActivity, Key.showSystemActivity) } }
    private(set) var launchAtLogin = SMAppService.mainApp.status == .enabled

    /// Called after any setting changes.
    @ObservationIgnored var onChange: (() -> Void)?

    @ObservationIgnored private let defaults = UserDefaults.standard

    private enum Key {
        static let source = "source"
        static let theme = "theme"
        static let waveStyle = "waveStyle"
        static let reactiveWaves = "reactiveWaves"
        static let spinArtwork = "spinArtwork"
        static let showLyrics = "showLyrics"
        static let showSystemActivity = "showSystemActivity"
    }

    init() {
        source = MusicSource(rawValue: defaults.string(forKey: Key.source) ?? "") ?? .automatic
        theme = IslandTheme(rawValue: defaults.string(forKey: Key.theme) ?? "") ?? .nowBar
        waveStyle = WaveStyle(rawValue: defaults.string(forKey: Key.waveStyle) ?? "") ?? .flow
        reactiveWaves = defaults.object(forKey: Key.reactiveWaves) as? Bool ?? true
        spinArtwork = defaults.object(forKey: Key.spinArtwork) as? Bool ?? true
        showLyrics = defaults.object(forKey: Key.showLyrics) as? Bool ?? true
        showSystemActivity = defaults.object(forKey: Key.showSystemActivity) as? Bool ?? true
    }

    func setLaunchAtLogin(_ enabled: Bool) {
        // Fails when run outside an .app bundle (e.g. `swift run`); the toggle then stays off.
        try? enabled ? SMAppService.mainApp.register() : SMAppService.mainApp.unregister()
        launchAtLogin = SMAppService.mainApp.status == .enabled
    }

    private func save(_ value: Any, _ key: String) {
        defaults.set(value, forKey: key)
        onChange?()
    }
}
