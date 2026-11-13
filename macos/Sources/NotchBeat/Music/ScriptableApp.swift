import AppKit

/// Runs AppleScript against a music app and listens for its playback notifications.
@MainActor
final class ScriptableApp {
    let name: String
    let bundleID: String
    private let changeNotification: Notification.Name

    init(name: String, bundleID: String, changeNotification: String) {
        self.name = name
        self.bundleID = bundleID
        self.changeNotification = Notification.Name(changeNotification)
    }

    var isRunning: Bool {
        !NSRunningApplication.runningApplications(withBundleIdentifier: bundleID).isEmpty
    }

    /// Calls `handler` when playback changes or the app quits.
    func observeChanges(_ handler: @escaping @MainActor @Sendable () -> Void) {
        DistributedNotificationCenter.default().addObserver(forName: changeNotification, object: nil, queue: .main) { _ in
            MainActor.assumeIsolated { handler() }
        }

        let bundleID = bundleID
        NSWorkspace.shared.notificationCenter.addObserver(
            forName: NSWorkspace.didTerminateApplicationNotification,
            object: nil,
            queue: .main
        ) { note in
            let app = note.userInfo?[NSWorkspace.applicationUserInfoKey] as? NSRunningApplication
            guard app?.bundleIdentifier == bundleID else { return }
            MainActor.assumeIsolated { handler() }
        }
    }

    /// Runs `body` inside `tell application "<name>"`. Returns `nil` on failure.
    @discardableResult
    func tell(_ body: String) -> NSAppleEventDescriptor? {
        // Checked first so scripting never launches the app by accident.
        guard isRunning, let script = NSAppleScript(source: "tell application \"\(name)\"\n\(body)\nend tell") else {
            return nil
        }
        var error: NSDictionary?
        let result = script.executeAndReturnError(&error)
        return error == nil ? result : nil
    }
}
