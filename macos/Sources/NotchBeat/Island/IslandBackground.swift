import AppKit
import SwiftUI

/// What sits behind the island's content, depending on the theme.
/// Closed modes are always black so the island blends into the notch.
struct IslandBackground: View {
    let theme: IslandTheme
    let isOpen: Bool
    let nowPlaying: NowPlaying

    var body: some View {
        ZStack {
            if isOpen && theme == .glass {
                GlassBlur()
            } else {
                Theme.Colors.background
            }

            if isOpen {
                if theme == .nowBar {
                    blurredArtwork
                }
                // Keep the top black so the island still looks attached to the notch.
                LinearGradient(colors: [.black, .clear], startPoint: .top, endPoint: UnitPoint(x: 0.5, y: 0.35))
            }
        }
    }

    @ViewBuilder
    private var blurredArtwork: some View {
        if let artwork = nowPlaying.artwork {
            Image(nsImage: artwork)
                .resizable()
                .aspectRatio(contentMode: .fill)
                .scaleEffect(1.4) // Hides the blur's soft transparent edges.
                .blur(radius: Theme.Colors.artworkBlurRadius)
                .saturation(1.4)
                .overlay(Color.black.opacity(Theme.Colors.artworkDimming))
        } else {
            nowPlaying.accent.opacity(Theme.Colors.albumTintOpacity)
        }
    }
}

/// Frosted glass that blurs whatever is behind the window.
private struct GlassBlur: NSViewRepresentable {
    func makeNSView(context: Context) -> NSVisualEffectView {
        let view = NSVisualEffectView()
        view.material = .hudWindow
        view.blendingMode = .behindWindow
        view.state = .active
        view.appearance = NSAppearance(named: .darkAqua)
        return view
    }

    func updateNSView(_ view: NSVisualEffectView, context: Context) {}
}
