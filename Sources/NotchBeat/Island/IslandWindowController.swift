import AppKit

/// Places the island panel at the notch and handles hovering and scroll gestures.
///
/// The panel is sized for the largest island but ignores the mouse outside the island,
/// so clicks on the transparent area reach the windows underneath.
@MainActor
final class IslandWindowController {
    private let model: IslandModel
    private let panel: IslandPanel
    private let gestures: ScrollGestures
    private var hoverPollTimer: Timer?

    /// Room around the expanded island for its shadow.
    private let shadowMargin: CGFloat = 24

    init(model: IslandModel) {
        self.model = model
        panel = IslandPanel(rootView: IslandView(model: model))
        gestures = ScrollGestures(model: model)
        layout()
        observe()
        panel.orderFrontRegardless()
    }

    private func observe() {
        NotificationCenter.default.addObserver(
            forName: NSApplication.didChangeScreenParametersNotification,
            object: nil,
            queue: .main
        ) { [weak self] _ in
            MainActor.assumeIsolated { self?.layout() }
        }

        NSEvent.addGlobalMonitorForEvents(matching: [.mouseMoved, .leftMouseDragged]) { [weak self] _ in
            MainActor.assumeIsolated { self?.updateHover() }
        }

        NSEvent.addLocalMonitorForEvents(matching: .scrollWheel) { [weak self] event in
            MainActor.assumeIsolated { self?.gestures.handle(event) }
            return event
        }
    }

    private func layout() {
        guard let screen = NotchGeometry.preferredScreen() else { return }
        model.notchSize = NotchGeometry.notchSize(on: screen)
        model.hasNotch = NotchGeometry.hasNotch(screen)

        let largest = IslandModel.largestSize
        let size = CGSize(width: largest.width + 2 * shadowMargin, height: largest.height + shadowMargin)
        panel.setFrame(
            NSRect(x: screen.frame.midX - size.width / 2, y: screen.frame.maxY - size.height,
                   width: size.width, height: size.height),
            display: true
        )
    }

    private func updateHover() {
        let size = model.size(for: model.mode)
        let frame = panel.frame
        let islandRect = NSRect(x: frame.midX - size.width / 2, y: frame.maxY - size.height,
                                width: size.width, height: size.height)
        let isInside = islandRect.insetBy(dx: -4, dy: -4).contains(NSEvent.mouseLocation)
        guard isInside != model.isHovering else { return }

        model.isHovering = isInside
        panel.ignoresMouseEvents = !isInside

        // While hovered, our own panel receives the mouse events, so the global monitor
        // goes quiet. Poll briefly to notice when the pointer leaves.
        hoverPollTimer?.invalidate()
        hoverPollTimer = isInside
            ? Timer.scheduledTimer(withTimeInterval: 0.05, repeats: true) { [weak self] _ in
                MainActor.assumeIsolated { self?.updateHover() }
            }
            : nil
    }
}
