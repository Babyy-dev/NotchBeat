import SwiftUI

/// Briefly shown when the song changes: artwork, title and artist under the notch.
struct PeekView: View {
    let model: IslandModel

    var body: some View {
        let nowPlaying = model.nowPlaying
        HStack(spacing: 10) {
            SpinningArtworkView(nowPlaying: nowPlaying, size: 32, isSpinning: model.preferences.spinArtwork)
            VStack(alignment: .leading, spacing: 1) {
                Text(nowPlaying.track?.title ?? "")
                    .font(.system(size: 12, weight: .semibold))
                    .foregroundStyle(.white)
                Text(nowPlaying.track?.artist ?? "")
                    .font(.system(size: 11))
                    .foregroundStyle(.white.opacity(0.6))
            }
            .lineLimit(1)
            Spacer(minLength: 0)
        }
        .padding(.top, model.notchSize.height + 6)
        .padding(.horizontal, 24)
        .frame(width: Theme.Island.peekWidth, height: model.size(for: .peek).height, alignment: .top)
    }
}
