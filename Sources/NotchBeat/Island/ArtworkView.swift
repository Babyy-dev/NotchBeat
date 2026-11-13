import SwiftUI

/// Album art with a music-note placeholder while it loads.
struct ArtworkView: View {
    let image: NSImage?
    let size: CGFloat
    let cornerRadius: CGFloat

    var body: some View {
        Group {
            if let image {
                Image(nsImage: image)
                    .resizable()
                    .aspectRatio(contentMode: .fill)
            } else {
                ZStack {
                    Color.white.opacity(0.1)
                    Image(systemName: "music.note")
                        .font(.system(size: size * 0.45))
                        .foregroundStyle(.white.opacity(0.6))
                }
            }
        }
        .frame(width: size, height: size)
        .clipShape(RoundedRectangle(cornerRadius: cornerRadius, style: .continuous))
    }
}

/// Round album art that turns like a vinyl record while music plays.
/// The angle follows the song position, so it stops on pause and resumes smoothly.
struct SpinningArtworkView: View {
    let nowPlaying: NowPlaying
    let size: CGFloat
    let isSpinning: Bool

    var body: some View {
        TimelineView(.animation(paused: !(isSpinning && nowPlaying.isPlaying))) { context in
            let degrees = isSpinning ? nowPlaying.position(at: context.date) * Theme.Artwork.spinDegreesPerSecond : 0
            ArtworkView(image: nowPlaying.artwork, size: size, cornerRadius: size / 2)
                .overlay {
                    if isSpinning && size >= Theme.Artwork.vinylHoleMinimumSize {
                        vinylHole
                    }
                }
                .rotationEffect(.degrees(degrees))
        }
        .frame(width: size, height: size)
    }

    private var vinylHole: some View {
        Circle()
            .fill(.black.opacity(0.7))
            .overlay(Circle().stroke(.white.opacity(0.25), lineWidth: 1))
            .frame(width: size * 0.18, height: size * 0.18)
    }
}
