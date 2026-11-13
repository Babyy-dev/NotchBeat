import AppKit

/// The menu bar icon with Settings and Quit.
@MainActor
final class StatusItemController: NSObject {
    private let statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
    private let settingsWindow: SettingsWindowController

    init(preferences: Preferences) {
        settingsWindow = SettingsWindowController(preferences: preferences)
        super.init()

        statusItem.button?.image = NSImage(systemSymbolName: "waveform", accessibilityDescription: "NotchBeat")

        let menu = NSMenu()
        menu.addItem(withTitle: "Settings…", action: #selector(openSettings), keyEquivalent: ",").target = self
        menu.addItem(.separator())
        menu.addItem(withTitle: "Quit NotchBeat", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        statusItem.menu = menu
    }

    @objc private func openSettings() {
        settingsWindow.show()
    }
}
