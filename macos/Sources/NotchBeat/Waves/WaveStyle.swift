import CoreGraphics

/// How the wave levels are drawn. Picked in Settings.
enum WaveStyle: String, CaseIterable, Identifiable {
    /// Samsung-style layered translucent waves rising from the bottom.
    case flow
    /// Rounded bars mirrored around the center line.
    case bars
    /// A smooth mirrored line.
    case line
    /// Columns of dots, like a dot-matrix equalizer.
    case dots
    /// Bars radiating from a ring.
    case circle

    var id: Self { self }

    var title: String {
        switch self {
        case .flow: "Flow"
        case .bars: "Bars"
        case .line: "Line"
        case .dots: "Dots"
        case .circle: "Circle"
        }
    }
}

/// How many bars to draw and how big they are.
struct WaveLayout {
    let barCount: Int
    let barWidth: CGFloat
    let spacing: CGFloat
}
