import CoreGraphics

/// Keeps an audio tap on the active music app while it plays, and serves live levels to the waves.
@MainActor
final class AudioVisualizer {
    private let analyzer = SpectrumAnalyzer()
    private var tap: AudioTap?
    private var tappedBundleID: String?

    /// Taps `bundleID`, or stops tapping when it is `nil` or `enabled` is false.
    func update(bundleID: String?, enabled: Bool) {
        let target = enabled ? bundleID : nil
        // Retry when the last attempt failed (e.g. the app hadn't started playing audio yet).
        guard target != tappedBundleID || (target != nil && tap == nil) else { return }

        tap?.stop()
        tap = nil
        tappedBundleID = target
        if let target {
            tap = try? AudioTap(bundleID: target, analyzer: analyzer)
        }
    }

    /// Live levels, or `nil` when there is no real audio to show.
    nonisolated func levels(count: Int) -> [CGFloat]? {
        analyzer.levels(count: count)
    }
}
