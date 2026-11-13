import CoreGraphics
import Foundation

/// Animated wave levels used when real audio isn't available.
enum SyntheticWave {
    /// Levels (0...1) for `count` bars at `time` seconds.
    static func levels(count: Int, time: Double) -> [CGFloat] {
        (0..<count).map { index in
            let x = Double(index) / Double(max(count - 1, 1)) // 0...1 across the row
            // Three sine waves at different speeds blend into an organic, non-repeating motion.
            let wave = sin(time * 5.1 + x * 7) * 0.5
                + sin(time * 3.3 - x * 11 + 1.3) * 0.3
                + sin(time * 8.7 + x * 3 + 2.1) * 0.2
            let normalized = (wave + 1) / 2
            // Taller in the middle, shorter at the edges.
            let envelope = 0.35 + 0.65 * sin(x * .pi)
            return CGFloat(normalized * envelope)
        }
    }
}
