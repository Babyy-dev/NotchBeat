import SwiftUI

/// The full player shown on hover, styled after Samsung's Now Bar:
/// round artwork, scrolling title, controls, wavy progress bar and waves along the bottom edge.
struct ExpandedView: View {
    let model: IslandModel

    private var nowPlaying: NowPlaying { model.nowPlaying }

    var body: some View {
        VStack(spacing: 8) {
            header

            if model.preferences.showLyrics {
                LyricLineView(nowPlaying: nowPlaying)
                    .contentShape(Rectangle())
                    .onTapGesture { model.isShowingFullLyrics = true }
            }

            if model.isShowingVolume {
                VolumeRow(volume: nowPlaying.volume, color: nowPlaying.accent)
            } else {
                ProgressRow(model: model)
            }
        }
        .padding(.top, model.notchSize.height + 6)
        .padding(.horizontal, 22)
        .padding(.bottom, 18)
        .frame(width: Theme.Island.expandedSize.width, height: model.size(for: .expanded).height, alignment: .top)
        .background(alignment: .bottom) {
            if model.preferences.theme.showsBottomWaves {
                WaveView(
                    style: model.preferences.waveStyle,
                    layout: Theme.Waves.bottomEdge,
                    color: nowPlaying.accent,
                    isPlaying: nowPlaying.isPlaying,
                    visualizer: nowPlaying.visualizer
                )
                .frame(height: Theme.Waves.bottomEdgeHeight)
                .opacity(Theme.Waves.bottomEdgeOpacity)
            }
        }
    }

    private var header: some View {
        HStack(spacing: 14) {
            SpinningArtworkView(nowPlaying: nowPlaying, size: 50, isSpinning: model.preferences.spinArtwork)
            VStack(spacing: 6) {
                MarqueeText(text: titleLine, font: Theme.Typography.title)
                    .foregroundStyle(.white)
                controls
            }
        }
    }

    private var titleLine: String {
        guard let track = nowPlaying.track else { return "" }
        return track.artist.isEmpty ? track.title : "\(track.title) • \(track.artist)"
    }

    private var controls: some View {
        HStack(spacing: 40) {
            ControlButton(symbol: "backward.end.fill", size: 17) { nowPlaying.perform(.previous) }
            ControlButton(symbol: nowPlaying.isPlaying ? "pause.fill" : "play.fill", size: 22) {
                nowPlaying.perform(.playPause)
            }
            ControlButton(symbol: "forward.end.fill", size: 17) { nowPlaying.perform(.next) }
        }
    }
}

/// The current lyric line, sliding up as the song moves on. Tap to open the full lyrics.
private struct LyricLineView: View {
    let nowPlaying: NowPlaying

    var body: some View {
        TimelineView(.periodic(from: .now, by: 0.25)) { context in
            let line = nowPlaying.lyrics?.line(at: nowPlaying.position(at: context.date)) ?? ""
            let text = line.isEmpty ? "♪" : line
            HStack(spacing: 6) {
                ZStack {
                    Text(text)
                        .font(.system(size: 12, weight: .medium))
                        .foregroundStyle(nowPlaying.accent)
                        .lineLimit(1)
                        .id(text)
                        .transition(.push(from: .bottom))
                }
                .frame(maxWidth: .infinity)
                .animation(.easeOut(duration: 0.3), value: text)

                if nowPlaying.lyrics != nil {
                    Image(systemName: "quote.bubble")
                        .font(.system(size: 11))
                        .foregroundStyle(.white.opacity(0.5))
                }
            }
        }
        .frame(height: Theme.Island.lyricsRowHeight - 8)
        .clipped()
    }
}

/// Samsung-style progress: wave hills ride on the played part, a glowing thumb, then a gray track.
/// Drag anywhere on it to seek.
private struct ProgressRow: View {
    let model: IslandModel

    private let barHeight: CGFloat = 5
    private let thumbSize: CGFloat = 13
    /// Space between the thumb and the unplayed track.
    private let thumbGap: CGFloat = 9

    var body: some View {
        let nowPlaying = model.nowPlaying
        TimelineView(.periodic(from: .now, by: 0.5)) { context in
            let duration = nowPlaying.track?.duration ?? 0
            let played = duration > 0 ? nowPlaying.position(at: context.date) / duration : 0
            let fraction = model.scrubFraction ?? played

            VStack(spacing: 3) {
                GeometryReader { geometry in
                    let width = geometry.size.width
                    let x = width * fraction
                    let barY = geometry.size.height - thumbSize / 2

                    ZStack {
                        Capsule()
                            .fill(.white.opacity(0.25))
                            .frame(width: max(0, width - x - thumbGap), height: barHeight - 1)
                            .position(x: x + thumbGap + max(0, width - x - thumbGap) / 2, y: barY)

                        Capsule()
                            .fill(nowPlaying.accent)
                            .frame(width: max(0, x), height: barHeight)
                            .position(x: x / 2, y: barY)

                        WaveView(
                            style: .flow,
                            layout: Theme.Waves.progress,
                            color: nowPlaying.accent,
                            isPlaying: nowPlaying.isPlaying,
                            visualizer: nowPlaying.visualizer
                        )
                        .frame(width: max(1, x), height: Theme.Waves.progressHeight)
                        .position(x: x / 2, y: barY - Theme.Waves.progressHeight / 2)

                        Circle()
                            .fill(nowPlaying.accent)
                            .overlay(Circle().stroke(.white, lineWidth: 2.5))
                            .frame(width: thumbSize, height: thumbSize)
                            .shadow(color: nowPlaying.accent.opacity(0.8), radius: 6)
                            .position(x: x, y: barY)
                    }
                    .contentShape(Rectangle())
                    .gesture(
                        DragGesture(minimumDistance: 0)
                            .onChanged { model.scrubFraction = Self.clamp($0.location.x / width) }
                            .onEnded { value in
                                nowPlaying.seek(to: Self.clamp(value.location.x / width) * duration)
                                model.scrubFraction = nil
                            }
                    )
                }
                .frame(height: Theme.Waves.progressHeight + thumbSize / 2)

                HStack {
                    Text(Self.format(fraction * duration))
                    Spacer()
                    Text(Self.format(duration))
                }
                .font(.system(size: 10, weight: .medium).monospacedDigit())
                .foregroundStyle(.white.opacity(0.5))
            }
        }
    }

    private static func clamp(_ value: Double) -> Double {
        min(max(value, 0), 1)
    }

    private static func format(_ seconds: TimeInterval) -> String {
        Duration.seconds(seconds).formatted(.time(pattern: .minuteSecond))
    }
}

/// Shown in place of the progress bar while the volume is being changed.
private struct VolumeRow: View {
    let volume: Double
    let color: Color

    var body: some View {
        HStack(spacing: 8) {
            Image(systemName: volume == 0 ? "speaker.slash.fill" : "speaker.wave.2.fill")
                .frame(width: 16)
            GeometryReader { geometry in
                ZStack(alignment: .leading) {
                    Capsule().fill(.white.opacity(0.25))
                    Capsule().fill(color).frame(width: geometry.size.width * volume / 100)
                }
                .frame(height: 5)
                .frame(maxHeight: .infinity)
            }
            Text("\(Int(volume.rounded()))")
                .frame(width: 24, alignment: .trailing)
        }
        .frame(height: Theme.Waves.progressHeight + 18)
        .font(.system(size: 10, weight: .medium).monospacedDigit())
        .foregroundStyle(.white.opacity(0.7))
    }
}

private struct ControlButton: View {
    let symbol: String
    let size: CGFloat
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Image(systemName: symbol)
                .font(.system(size: size))
                .foregroundStyle(.white)
                .frame(width: 30, height: 26)
                .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
    }
}
