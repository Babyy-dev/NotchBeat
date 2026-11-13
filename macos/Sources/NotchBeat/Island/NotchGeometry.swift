import AppKit

/// Finds where the notch is and how big it is.
@MainActor
enum NotchGeometry {
    /// The display with a notch if there is one, otherwise the main display.
    static func preferredScreen() -> NSScreen? {
        NSScreen.screens.first(where: hasNotch) ?? NSScreen.main
    }

    static func hasNotch(_ screen: NSScreen) -> Bool {
        screen.safeAreaInsets.top > 0
    }

    static func notchSize(on screen: NSScreen) -> CGSize {
        guard
            hasNotch(screen),
            let left = screen.auxiliaryTopLeftArea?.width,
            let right = screen.auxiliaryTopRightArea?.width
        else {
            let menuBarHeight = screen.frame.maxY - screen.visibleFrame.maxY
            return CGSize(width: Theme.Island.fallbackNotchWidth, height: max(menuBarHeight, 24))
        }
        return CGSize(width: screen.frame.width - left - right, height: screen.safeAreaInsets.top)
    }
}
