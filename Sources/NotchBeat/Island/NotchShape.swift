import SwiftUI

struct NotchCorners {
    /// Outward flare where the island meets the top of the screen.
    var top: CGFloat
    /// Rounded bottom corners.
    var bottom: CGFloat
}

/// The island outline: flared top corners that blend into the menu bar, rounded bottom corners.
struct NotchShape: Shape {
    var corners: NotchCorners

    var animatableData: AnimatablePair<CGFloat, CGFloat> {
        get { AnimatablePair(corners.top, corners.bottom) }
        set { corners = NotchCorners(top: newValue.first, bottom: newValue.second) }
    }

    func path(in rect: CGRect) -> Path {
        let top = corners.top
        let bottom = corners.bottom
        var path = Path()

        path.move(to: CGPoint(x: rect.minX, y: rect.minY))
        path.addQuadCurve(to: CGPoint(x: rect.minX + top, y: rect.minY + top),
                          control: CGPoint(x: rect.minX + top, y: rect.minY))
        path.addLine(to: CGPoint(x: rect.minX + top, y: rect.maxY - bottom))
        path.addQuadCurve(to: CGPoint(x: rect.minX + top + bottom, y: rect.maxY),
                          control: CGPoint(x: rect.minX + top, y: rect.maxY))
        path.addLine(to: CGPoint(x: rect.maxX - top - bottom, y: rect.maxY))
        path.addQuadCurve(to: CGPoint(x: rect.maxX - top, y: rect.maxY - bottom),
                          control: CGPoint(x: rect.maxX - top, y: rect.maxY))
        path.addLine(to: CGPoint(x: rect.maxX - top, y: rect.minY + top))
        path.addQuadCurve(to: CGPoint(x: rect.maxX, y: rect.minY),
                          control: CGPoint(x: rect.maxX - top, y: rect.minY))
        path.closeSubpath()
        return path
    }
}
