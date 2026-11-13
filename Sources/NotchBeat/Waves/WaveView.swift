import SwiftUI

/// Live wave visualizer. Uses real audio levels when available, otherwise a smooth animation.
struct WaveView: View {
    let style: WaveStyle
    let layout: WaveLayout
    let color: Color
    let isPlaying: Bool
    let visualizer: AudioVisualizer

    var body: some View {
        TimelineView(.animation(paused: !isPlaying)) { context in
            let levels = levels(at: context.date)
            let renderer = WaveRenderer(style: style, layout: layout, color: color)
            Canvas { canvas, size in
                canvas.addFilter(.shadow(color: color.opacity(0.6), radius: Theme.Waves.glowRadius))
                renderer.draw(levels, in: &canvas, size: size)
            }
        }
    }

    private func levels(at date: Date) -> [CGFloat] {
        let rest = Theme.Waves.restLevel
        guard isPlaying else { return Array(repeating: rest, count: layout.barCount) }

        let raw = visualizer.levels(count: layout.barCount)
            ?? SyntheticWave.levels(count: layout.barCount, time: date.timeIntervalSinceReferenceDate * Theme.Waves.speed)
        return raw.map { rest + (1 - rest) * $0 }
    }
}
