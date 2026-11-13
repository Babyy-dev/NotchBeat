import SwiftUI

/// Draws wave levels (0...1) in one of the `WaveStyle`s.
struct WaveRenderer {
    let style: WaveStyle
    let layout: WaveLayout
    let color: Color

    func draw(_ levels: [CGFloat], in context: inout GraphicsContext, size: CGSize) {
        guard !levels.isEmpty else { return }
        switch style {
        case .flow: drawFlow(levels, in: &context, size: size)
        case .bars: drawBars(levels, in: &context, size: size)
        case .line: drawLine(levels, in: &context, size: size)
        case .dots: drawDots(levels, in: &context, size: size)
        case .circle: drawCircle(levels, in: &context, size: size)
        }
    }

    /// Three overlapping translucent hills, each fed a shifted copy of the levels so they move independently.
    private func drawFlow(_ levels: [CGFloat], in context: inout GraphicsContext, size: CGSize) {
        let count = levels.count
        guard count > 1 else { return }
        let layers: [(shift: Int, scale: CGFloat, opacity: Double)] = [
            (count / 3, 0.75, 0.3),
            (count * 2 / 3, 0.9, 0.45),
            (0, 1, 0.85),
        ]

        for (index, layer) in layers.enumerated() {
            let points = (0..<count).map { i in
                let x = CGFloat(i) / CGFloat(count - 1)
                let taper = sin(x * .pi) // Ends meet the baseline, like a hill.
                let level = levels[(i + layer.shift) % count] * layer.scale * taper
                return CGPoint(x: x * size.width, y: size.height * (1 - level))
            }

            var hill = smoothPath(through: points)
            hill.addLine(to: CGPoint(x: size.width, y: size.height))
            hill.addLine(to: CGPoint(x: 0, y: size.height))
            hill.closeSubpath()
            context.fill(hill, with: .linearGradient(
                Gradient(colors: [color.opacity(layer.opacity), color.opacity(layer.opacity * 0.5)]),
                startPoint: .zero, endPoint: CGPoint(x: 0, y: size.height)
            ))

            // A soft highlight on the front wave's crest.
            if index == layers.count - 1 {
                context.stroke(smoothPath(through: points), with: .color(.white.opacity(0.35)), lineWidth: 1)
            }
        }
    }

    private func drawBars(_ levels: [CGFloat], in context: inout GraphicsContext, size: CGSize) {
        let shading = verticalGradient(height: size.height)
        for (x, level) in zip(columns(levels.count, width: size.width), levels) {
            let height = max(layout.barWidth, size.height * level)
            let rect = CGRect(x: x - layout.barWidth / 2, y: (size.height - height) / 2, width: layout.barWidth, height: height)
            context.fill(Capsule().path(in: rect), with: shading)
        }
    }

    private func drawLine(_ levels: [CGFloat], in context: inout GraphicsContext, size: CGSize) {
        let xs = columns(levels.count, width: size.width)
        let middle = size.height / 2

        // Upper line at full strength, mirrored lower line fainter.
        for (direction, opacity) in [(-1.0, 1.0), (1.0, 0.5)] {
            let points = zip(xs, levels).map { CGPoint(x: $0, y: middle + direction * $1 * size.height / 2) }
            context.stroke(smoothPath(through: points), with: .color(color.opacity(opacity)),
                           style: StrokeStyle(lineWidth: 2, lineCap: .round, lineJoin: .round))
        }
    }

    private func drawDots(_ levels: [CGFloat], in context: inout GraphicsContext, size: CGSize) {
        let dot = layout.barWidth
        let step = dot + layout.spacing
        let maxDots = max(1, Int((size.height + layout.spacing) / step))

        for (x, level) in zip(columns(levels.count, width: size.width), levels) {
            let count = max(1, Int((CGFloat(maxDots) * level).rounded()))
            var y = (size.height - (CGFloat(count) * step - layout.spacing)) / 2
            for _ in 0..<count {
                context.fill(Circle().path(in: CGRect(x: x - dot / 2, y: y, width: dot, height: dot)), with: .color(color))
                y += step
            }
        }
    }

    private func drawCircle(_ levels: [CGFloat], in context: inout GraphicsContext, size: CGSize) {
        let center = CGPoint(x: size.width / 2, y: size.height / 2)
        let outer = min(size.width, size.height) / 2
        let inner = outer * 0.45

        var path = Path()
        for (index, level) in levels.enumerated() {
            let angle = Double(index) / Double(levels.count) * 2 * .pi - .pi / 2
            let direction = CGPoint(x: cos(angle), y: sin(angle))
            let length = inner + (outer - inner) * level
            path.move(to: CGPoint(x: center.x + direction.x * inner, y: center.y + direction.y * inner))
            path.addLine(to: CGPoint(x: center.x + direction.x * length, y: center.y + direction.y * length))
        }
        context.stroke(path, with: .color(color), style: StrokeStyle(lineWidth: max(1, layout.barWidth * 0.5), lineCap: .round))
    }

    /// A smooth curve through `points`, using their midpoints as curve ends.
    private func smoothPath(through points: [CGPoint]) -> Path {
        var path = Path()
        guard let first = points.first, let last = points.last else { return path }
        path.move(to: first)
        for index in points.indices.dropFirst() {
            let previous = points[index - 1]
            let midpoint = CGPoint(x: (previous.x + points[index].x) / 2, y: (previous.y + points[index].y) / 2)
            path.addQuadCurve(to: midpoint, control: previous)
        }
        path.addLine(to: last)
        return path
    }

    /// Center x of each bar, with the whole row centered in `width`.
    private func columns(_ count: Int, width: CGFloat) -> [CGFloat] {
        let rowWidth = CGFloat(count) * layout.barWidth + CGFloat(count - 1) * layout.spacing
        let start = (width - rowWidth) / 2 + layout.barWidth / 2
        return (0..<count).map { start + CGFloat($0) * (layout.barWidth + layout.spacing) }
    }

    private func verticalGradient(height: CGFloat) -> GraphicsContext.Shading {
        .linearGradient(Gradient(colors: [color, color.opacity(0.55)]),
                        startPoint: .zero, endPoint: CGPoint(x: 0, y: height))
    }
}
