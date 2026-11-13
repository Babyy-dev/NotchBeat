import SwiftUI

/// Small artwork on the left of the notch, waves on the right.
struct CompactView: View {
    let model: IslandModel

    var body: some View {
        let nowPlaying = model.nowPlaying
        HStack(spacing: 0) {
            SpinningArtworkView(nowPlaying: nowPlaying, size: 20, isSpinning: model.preferences.spinArtwork)
                .frame(maxWidth: .infinity)

            Color.clear.frame(width: model.notchSize.width)

            WaveView(
                style: model.preferences.waveStyle,
                layout: Theme.Waves.compact,
                color: nowPlaying.accent,
                isPlaying: nowPlaying.isPlaying,
                visualizer: nowPlaying.visualizer
            )
            .frame(width: Theme.Waves.compactSize.width, height: Theme.Waves.compactSize.height)
            .frame(maxWidth: .infinity)
        }
        .frame(maxHeight: .infinity)
    }
}
