import SwiftUI

/// Decides which shape the island takes and how big it is.
@MainActor
@Observable
final class IslandModel {
    enum Mode {
        /// Hidden behind the notch.
        case idle
        /// Music playing: artwork on the left, waves on the right.
        case compact
        /// Short pop with the new song's title after a track change.
        case peek
        /// Short pop for a system event: volume, audio device, charging.
        case activity
        /// Hovered: full player.
        case expanded
        /// Hovered, with the karaoke lyrics open.
        case lyrics
    }

    /// The largest the island can get; the window is sized to fit it.
    static var largestSize: CGSize {
        CGSize(
            width: max(Theme.Island.expandedSize.width, Theme.Island.lyricsSize.width, Theme.Island.peekWidth),
            height: max(Theme.Island.expandedSize.height + Theme.Island.lyricsRowHeight, Theme.Island.lyricsSize.height)
        )
    }

    let nowPlaying: NowPlaying
    let preferences: Preferences
    var notchSize = CGSize(width: Theme.Island.fallbackNotchWidth, height: 32)
    var hasNotch = false
    var isHovering = false {
        didSet {
            if !isHovering { isShowingFullLyrics = false }
        }
    }
    var isShowingFullLyrics = false
    /// Progress-bar position (0...1) while the user drags it, otherwise `nil`.
    var scrubFraction: Double?
    private(set) var isPeeking = false
    private(set) var isShowingVolume = false
    private(set) var activity: SystemActivity?

    @ObservationIgnored private var peekTask: Task<Void, Never>?
    @ObservationIgnored private var volumeTask: Task<Void, Never>?
    @ObservationIgnored private var activityTask: Task<Void, Never>?

    init(nowPlaying: NowPlaying, preferences: Preferences) {
        self.nowPlaying = nowPlaying
        self.preferences = preferences
        nowPlaying.onTrackChange = { [weak self] in self?.peek() }
    }

    var mode: Mode {
        let hasTrack = nowPlaying.track != nil
        if isHovering && hasTrack { return isShowingFullLyrics ? .lyrics : .expanded }
        if activity != nil { return .activity }
        guard hasTrack else { return .idle }
        if isPeeking { return .peek }
        return nowPlaying.isPlaying ? .compact : .idle
    }

    func size(for mode: Mode) -> CGSize {
        switch mode {
        case .idle:
            notchSize
        case .compact:
            CGSize(width: notchSize.width + 2 * Theme.Island.compactWingWidth, height: notchSize.height)
        case .peek:
            CGSize(width: Theme.Island.peekWidth, height: notchSize.height + Theme.Island.peekContentHeight)
        case .activity:
            CGSize(width: Theme.Island.peekWidth, height: notchSize.height + Theme.Island.activityContentHeight)
        case .expanded:
            CGSize(
                width: Theme.Island.expandedSize.width,
                height: Theme.Island.expandedSize.height + (preferences.showLyrics ? Theme.Island.lyricsRowHeight : 0)
            )
        case .lyrics:
            Theme.Island.lyricsSize
        }
    }

    func adjustVolume(by delta: Double) {
        nowPlaying.setVolume(nowPlaying.volume + delta)
        isShowingVolume = true
        volumeTask?.cancel()
        volumeTask = after(Theme.Gestures.volumeIndicatorDuration) { $0.isShowingVolume = false }
    }

    func show(_ activity: SystemActivity) {
        guard preferences.showSystemActivity else { return }
        self.activity = activity
        activityTask?.cancel()
        let duration = activity.kind == .volume ? Theme.Island.volumeActivityDuration : Theme.Island.activityDuration
        activityTask = after(duration) { $0.activity = nil }
    }

    private func peek() {
        isPeeking = true
        peekTask?.cancel()
        peekTask = after(Theme.Island.peekDuration) { $0.isPeeking = false }
    }

    /// Runs `action` after `seconds` unless the returned task is cancelled first.
    private func after(_ seconds: Double, _ action: @escaping @MainActor (IslandModel) -> Void) -> Task<Void, Never> {
        Task { [weak self] in
            try? await Task.sleep(for: .seconds(seconds))
            guard !Task.isCancelled, let self else { return }
            action(self)
        }
    }
}
