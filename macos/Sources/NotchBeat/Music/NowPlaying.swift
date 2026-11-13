import AppKit
import SwiftUI

/// The single source of truth for what is playing. Views read from it; it reads from the music apps.
@MainActor
@Observable
final class NowPlaying {
    private(set) var track: Track?
    private(set) var isPlaying = false
    /// 0...100
    private(set) var volume: Double = 50
    private(set) var artwork: NSImage?
    private(set) var accent = Theme.Colors.fallbackAccent
    private(set) var lyrics: Lyrics?

    /// Called when a new song starts.
    @ObservationIgnored var onTrackChange: (() -> Void)?
    @ObservationIgnored let visualizer = AudioVisualizer()

    @ObservationIgnored private let preferences: Preferences
    @ObservationIgnored private let players: [any MusicPlayer] = [SpotifyPlayer(), AppleMusicPlayer()]
    @ObservationIgnored private var activePlayer: (any MusicPlayer)?
    @ObservationIgnored private var lyricsTrackID: String?

    /// Last known position and when it was read; the current position is extrapolated from it.
    private var anchor = (position: TimeInterval(0), date: Date.now)

    init(preferences: Preferences) {
        self.preferences = preferences
    }

    func start() {
        for player in players {
            player.startObserving { [weak self] in self?.refresh() }
        }
        refresh()
    }

    func preferencesChanged() {
        refresh()
        updateLyrics()
    }

    func position(at date: Date) -> TimeInterval {
        guard let track else { return 0 }
        let elapsed = isPlaying ? date.timeIntervalSince(anchor.date) : 0
        return min(anchor.position + elapsed, track.duration)
    }

    func perform(_ command: PlayerCommand) {
        activePlayer?.send(command)
        if command == .playPause {
            // Update instantly; the app's change notification confirms shortly after.
            anchor = (position(at: .now), .now)
            isPlaying.toggle()
        }
    }

    func seek(to position: TimeInterval) {
        activePlayer?.seek(to: position)
        anchor = (position, .now)
    }

    func setVolume(_ newValue: Double) {
        let clamped = min(max(newValue, 0), 100)
        let didChangeStep = Int(clamped.rounded()) != Int(volume.rounded())
        volume = clamped
        if didChangeStep {
            activePlayer?.setVolume(Int(clamped.rounded()))
        }
    }

    // MARK: - Reading state

    private func refresh() {
        let states = players
            .filter { preferences.source.allows($0) }
            .compactMap { player in player.fetchState().map { (player: player, state: $0) } }

        // Prefer whatever is playing, then the app we were already showing.
        let chosen = states.first { $0.state.isPlaying }
            ?? states.first { $0.player === activePlayer }
            ?? states.first

        activePlayer = chosen?.player
        apply(chosen?.state)
        visualizer.update(bundleID: isPlaying ? activePlayer?.bundleID : nil, enabled: preferences.reactiveWaves)
    }

    private func apply(_ state: PlayerState?) {
        guard let state, let activePlayer else {
            track = nil
            isPlaying = false
            artwork = nil
            accent = Theme.Colors.fallbackAccent
            lyrics = nil
            lyricsTrackID = nil
            return
        }

        let isNewTrack = state.track.id != track?.id
        track = state.track
        isPlaying = state.isPlaying
        volume = Double(state.volume)
        anchor = (state.position, .now)

        if isNewTrack {
            artwork = nil
            lyrics = nil
            loadArtwork(for: state.track, from: activePlayer)
            updateLyrics()
            onTrackChange?()
        }
    }

    private func loadArtwork(for track: Track, from player: any MusicPlayer) {
        Task {
            let image = await player.artwork(for: track)
            guard self.track?.id == track.id else { return } // The song changed meanwhile.
            artwork = image
            accent = image.flatMap(ArtworkPalette.accentColor(of:)) ?? Theme.Colors.fallbackAccent
        }
    }

    private func updateLyrics() {
        guard preferences.showLyrics, let track else {
            lyrics = nil
            lyricsTrackID = nil
            return
        }
        guard lyricsTrackID != track.id else { return }
        lyricsTrackID = track.id

        Task {
            let result = await LRCLibClient.lyrics(for: track)
            guard lyricsTrackID == track.id else { return }
            lyrics = result
        }
    }
}
