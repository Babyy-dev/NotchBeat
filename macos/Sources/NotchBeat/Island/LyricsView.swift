import SwiftUI

/// Full-screen karaoke lyrics: the current line fills with color as it's sung and stays centered.
/// Tap a line to jump to it; tap the header to go back to the player.
struct LyricsView: View {
    let model: IslandModel

    private var nowPlaying: NowPlaying { model.nowPlaying }

    var body: some View {
        VStack(spacing: 10) {
            header

            if let lyrics = nowPlaying.lyrics {
                lyricsList(lyrics)
            } else {
                Text("No synced lyrics for this song")
                    .font(.system(size: 13))
                    .foregroundStyle(.white.opacity(0.5))
                    .frame(maxHeight: .infinity)
            }
        }
        .padding(.top, model.notchSize.height + 6)
        .padding(.horizontal, 24)
        .padding(.bottom, 14)
        .frame(width: Theme.Island.lyricsSize.width, height: Theme.Island.lyricsSize.height, alignment: .top)
    }

    private var header: some View {
        HStack(spacing: 10) {
            SpinningArtworkView(nowPlaying: nowPlaying, size: 28, isSpinning: model.preferences.spinArtwork)
            MarqueeText(text: nowPlaying.track?.title ?? "", font: Theme.Typography.title)
                .foregroundStyle(.white)
            Image(systemName: "chevron.up")
                .font(.system(size: 12, weight: .semibold))
                .foregroundStyle(.white.opacity(0.6))
        }
        .contentShape(Rectangle())
        .onTapGesture { model.isShowingFullLyrics = false }
    }

    private func lyricsList(_ lyrics: Lyrics) -> some View {
        TimelineView(.periodic(from: .now, by: 0.1)) { context in
            let progress = lyrics.progress(at: nowPlaying.position(at: context.date))

            ScrollViewReader { proxy in
                ScrollView(.vertical, showsIndicators: false) {
                    VStack(alignment: .leading, spacing: 12) {
                        ForEach(lyrics.lines.indices, id: \.self) { index in
                            KaraokeLine(
                                text: lyrics.lines[index].text,
                                state: lineState(index, progress: progress),
                                color: nowPlaying.accent
                            )
                            .id(index)
                            .onTapGesture { nowPlaying.seek(to: lyrics.lines[index].time) }
                        }
                    }
                    .padding(.vertical, 70) // Lets the first and last lines reach the center.
                }
                .onChange(of: progress?.index) { _, index in
                    withAnimation(.easeInOut(duration: 0.4)) { proxy.scrollTo(index, anchor: .center) }
                }
                .onAppear { proxy.scrollTo(progress?.index, anchor: .center) }
            }
        }
        .mask(
            LinearGradient(
                stops: [
                    .init(color: .clear, location: 0),
                    .init(color: .black, location: 0.2),
                    .init(color: .black, location: 0.8),
                    .init(color: .clear, location: 1),
                ],
                startPoint: .top,
                endPoint: .bottom
            )
        )
    }

    private func lineState(_ index: Int, progress: (index: Int, fraction: Double)?) -> KaraokeLine.State {
        guard let progress else { return .upcoming }
        if index < progress.index { return .sung }
        if index > progress.index { return .upcoming }
        return .current(fill: progress.fraction)
    }
}

/// One lyric line. The current line fills left to right with the accent color.
private struct KaraokeLine: View {
    enum State: Equatable {
        case sung, upcoming
        case current(fill: Double)
    }

    let text: String
    let state: State
    let color: Color

    var body: some View {
        let isCurrent = if case .current = state { true } else { false }
        let label = Text(text.isEmpty ? "♪" : text)
            .font(.system(size: isCurrent ? 17 : 14, weight: isCurrent ? .bold : .semibold))

        label
            .foregroundStyle(.white.opacity(state == .sung ? 0.5 : 0.3))
            .overlay(alignment: .leading) {
                if case .current(let fill) = state {
                    label
                        .foregroundStyle(color)
                        .mask(alignment: .leading) {
                            GeometryReader { geometry in
                                Rectangle().frame(width: geometry.size.width * fill)
                            }
                        }
                }
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            .animation(.easeOut(duration: 0.25), value: isCurrent)
    }
}
