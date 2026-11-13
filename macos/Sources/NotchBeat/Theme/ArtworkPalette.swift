import AppKit
import SwiftUI

/// Extracts an accent color from album art so the waves match the song.
enum ArtworkPalette {
    /// Returns the most vivid color in the image, brightened to stay visible on black.
    /// Returns `nil` for grayscale art.
    static func accentColor(of image: NSImage) -> Color? {
        let side = 12
        guard
            let cgImage = image.cgImage(forProposedRect: nil, context: nil, hints: nil),
            let context = CGContext(
                data: nil, width: side, height: side, bitsPerComponent: 8, bytesPerRow: side * 4,
                space: CGColorSpaceCreateDeviceRGB(),
                bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
            )
        else { return nil }

        context.draw(cgImage, in: CGRect(x: 0, y: 0, width: side, height: side))
        guard let data = context.data else { return nil }
        let pixels = data.bindMemory(to: UInt8.self, capacity: side * side * 4)

        var best: NSColor?
        var bestScore: CGFloat = 0
        for index in 0..<(side * side) {
            let color = NSColor(
                srgbRed: CGFloat(pixels[index * 4]) / 255,
                green: CGFloat(pixels[index * 4 + 1]) / 255,
                blue: CGFloat(pixels[index * 4 + 2]) / 255,
                alpha: 1
            )
            let score = color.saturationComponent * color.brightnessComponent
            if score > bestScore {
                bestScore = score
                best = color
            }
        }

        guard let best, bestScore > 0.08 else { return nil }
        return Color(nsColor: NSColor(
            hue: best.hueComponent,
            saturation: best.saturationComponent,
            brightness: max(best.brightnessComponent, 0.8),
            alpha: 1
        ))
    }
}
