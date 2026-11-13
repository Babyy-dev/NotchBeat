import SwiftUI

/// The root view: a notch-shaped island that morphs between modes.
struct IslandView: View {
    let model: IslandModel

    var body: some View {
        let mode = model.mode
        let size = model.size(for: mode)
        let isOpen = mode != .idle && mode != .compact
        let theme = model.preferences.theme
        let shape = NotchShape(corners: isOpen ? Theme.Island.expandedCorners : Theme.Island.compactCorners)

        ZStack(alignment: .top) {
            switch mode {
            case .idle:
                Color.clear
            case .compact:
                CompactView(model: model)
                    .transition(.opacity)
            case .peek:
                PeekView(model: model)
                    .transition(.opacity)
            case .activity:
                if let activity = model.activity {
                    ActivityView(activity: activity, model: model)
                        .transition(.opacity)
                }
            case .expanded:
                ExpandedView(model: model)
                    .transition(.opacity.combined(with: .scale(scale: 0.9, anchor: .top)))
            case .lyrics:
                LyricsView(model: model)
                    .transition(.opacity)
            }
        }
        .frame(width: size.width, height: size.height)
        .background { IslandBackground(theme: theme, isOpen: isOpen, nowPlaying: model.nowPlaying) }
        .clipShape(shape)
        .overlay { outline(theme: theme, isOpen: isOpen, shape: shape) }
        // Without a physical notch there is nothing to hide behind, so idle disappears.
        .opacity(mode == .idle && !model.hasNotch ? 0 : 1)
        .shadow(color: .black.opacity(isOpen ? 0.45 : 0), radius: 12)
        .animation(mode == .peek || mode == .activity ? Theme.Motion.bounce : Theme.Motion.morph, value: mode)
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
    }

    /// Neon: a glowing edge in the album's color. Glass: a faint light edge.
    @ViewBuilder
    private func outline(theme: IslandTheme, isOpen: Bool, shape: NotchShape) -> some View {
        if isOpen && theme == .neon {
            shape
                .stroke(model.nowPlaying.accent, lineWidth: 1.5)
                .shadow(color: model.nowPlaying.accent, radius: 6)
        } else if isOpen && theme == .glass {
            shape.stroke(.white.opacity(0.15), lineWidth: 1)
        }
    }
}
