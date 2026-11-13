import AppKit

/// Turns scrolling over the expanded island into actions:
/// swipe left/right to skip tracks, scroll up/down to change volume.
@MainActor
final class ScrollGestures {
    private let model: IslandModel
    private var swipeDistance: CGFloat = 0
    private var didSkipThisSwipe = false

    init(model: IslandModel) {
        self.model = model
    }

    func handle(_ event: NSEvent) {
        guard model.mode == .expanded, event.momentumPhase.isEmpty else { return }
        if event.phase == .began {
            swipeDistance = 0
            didSkipThisSwipe = false
        }

        // Convert to finger direction so natural scrolling doesn't flip the gestures.
        let sign: CGFloat = event.isDirectionInvertedFromDevice ? 1 : -1
        let fingerX = event.scrollingDeltaX * sign
        let fingerY = event.scrollingDeltaY * sign

        if abs(fingerX) > abs(fingerY) {
            swipeDistance += fingerX
            if !didSkipThisSwipe, abs(swipeDistance) > Theme.Gestures.swipeThreshold {
                didSkipThisSwipe = true
                model.nowPlaying.perform(swipeDistance < 0 ? .next : .previous)
            }
        } else if fingerY != 0 {
            let step = event.hasPreciseScrollingDeltas
                ? Theme.Gestures.volumePerPoint * Double(abs(fingerY))
                : Theme.Gestures.volumePerWheelStep
            model.adjustVolume(by: fingerY < 0 ? step : -step) // Fingers up = louder.
        }
    }
}
