import AppKit
import SwiftUI

/// Single-line text that scrolls sideways when it's too long to fit, with faded edges.
struct MarqueeText: View {
    let text: String
    let font: NSFont

    private let gap: CGFloat = 40

    var body: some View {
        let textWidth = (text as NSString).size(withAttributes: [.font: font]).width

        GeometryReader { geometry in
            let overflows = textWidth > geometry.size.width
            TimelineView(.animation(paused: !overflows)) { context in
                HStack(spacing: gap) {
                    label
                    if overflows { label } // Second copy follows the first for a seamless loop.
                }
                .offset(x: overflows ? scrollOffset(at: context.date, distance: textWidth + gap) : 0)
                .frame(width: geometry.size.width, alignment: overflows ? .leading : .center)
            }
        }
        .frame(height: ceil(font.boundingRectForFont.height))
        .clipped()
        .mask(
            LinearGradient(
                stops: [
                    .init(color: .clear, location: 0),
                    .init(color: .black, location: 0.08),
                    .init(color: .black, location: 0.92),
                    .init(color: .clear, location: 1),
                ],
                startPoint: .leading,
                endPoint: .trailing
            )
        )
    }

    private var label: some View {
        Text(text)
            .font(Font(font as CTFont))
            .fixedSize()
    }

    /// Waits `marqueePause`, scrolls one full `distance`, then repeats.
    private func scrollOffset(at date: Date, distance: CGFloat) -> CGFloat {
        let scrollDuration = Double(distance / Theme.Typography.marqueeSpeed)
        let cycle = Theme.Typography.marqueePause + scrollDuration
        let phase = date.timeIntervalSinceReferenceDate.truncatingRemainder(dividingBy: cycle)
        return -CGFloat(max(0, phase - Theme.Typography.marqueePause)) * Theme.Typography.marqueeSpeed
    }
}
