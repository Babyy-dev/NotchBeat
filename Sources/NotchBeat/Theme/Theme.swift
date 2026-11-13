import SwiftUI

/// Every visual knob lives here. Change these values to make the island your own.
enum Theme {
    enum Island {
        /// Extra width shown on each side of the notch while music plays.
        static let compactWingWidth: CGFloat = 34

        static let expandedSize = CGSize(width: 360, height: 160)
        /// Extra height added to the expanded island when lyrics are on.
        static let lyricsRowHeight: CGFloat = 26

        /// The pop shown when the song changes.
        static let peekWidth: CGFloat = 300
        static let peekContentHeight: CGFloat = 46
        static let peekDuration: Double = 3

        /// Full karaoke lyrics view.
        static let lyricsSize = CGSize(width: 360, height: 300)

        /// System pop-ups (volume, audio device, charging).
        static let activityContentHeight: CGFloat = 38
        static let activityDuration: Double = 2.5
        static let volumeActivityDuration: Double = 1.5

        static let compactCorners = NotchCorners(top: 6, bottom: 10)
        static let expandedCorners = NotchCorners(top: 14, bottom: 36)

        /// Used on Macs without a physical notch.
        static let fallbackNotchWidth: CGFloat = 190
    }

    enum Waves {
        static let compact = WaveLayout(barCount: 5, barWidth: 2.5, spacing: 2)
        static let compactSize = CGSize(width: 24, height: 14)
        /// Waves along the bottom edge of the expanded island.
        static let bottomEdge = WaveLayout(barCount: 40, barWidth: 4, spacing: 3)
        static let bottomEdgeHeight: CGFloat = 22
        static let bottomEdgeOpacity: Double = 0.6
        /// Hills riding on the played part of the progress bar.
        static let progress = WaveLayout(barCount: 20, barWidth: 3, spacing: 2)
        static let progressHeight: CGFloat = 16
        /// Bar height (0...1) when music is paused.
        static let restLevel: CGFloat = 0.18
        /// Speed of the animated (non-reactive) waves. Higher is faster.
        static let speed: Double = 1.0
        static let glowRadius: CGFloat = 4
    }

    enum Colors {
        static let background = Color.black
        /// Used until album art loads, or when the art has no vivid color.
        static let fallbackAccent = Color(red: 0.36, green: 0.55, blue: 1.0)
        /// Album-color tint used by the Now Bar theme until artwork loads.
        static let albumTintOpacity: Double = 0.28
        /// Now Bar theme: how blurry and how darkened the album art background is.
        static let artworkBlurRadius: CGFloat = 28
        static let artworkDimming: Double = 0.45
    }

    enum Artwork {
        /// Spinning-cover speed. 20 means one full turn every 18 seconds.
        static let spinDegreesPerSecond: Double = 20
        /// Covers smaller than this skip the vinyl center hole.
        static let vinylHoleMinimumSize: CGFloat = 32
    }

    enum Typography {
        static var title: NSFont { .systemFont(ofSize: 13, weight: .semibold) }
        /// Scrolling speed (points per second) for titles that don't fit.
        static let marqueeSpeed: CGFloat = 30
        /// Pause before each scroll, in seconds.
        static let marqueePause: Double = 2
    }

    enum Motion {
        static let morph = Animation.spring(response: 0.42, dampingFraction: 0.78)
        static let bounce = Animation.spring(response: 0.45, dampingFraction: 0.6)
    }

    enum Gestures {
        /// Horizontal swipe distance (points) that skips a track.
        static let swipeThreshold: CGFloat = 40
        /// Volume change per point of trackpad scrolling.
        static let volumePerPoint: Double = 0.25
        /// Volume change per notch of a mouse wheel.
        static let volumePerWheelStep: Double = 2
        static let volumeIndicatorDuration: Double = 1.2
    }
}
