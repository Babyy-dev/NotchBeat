import Foundation
import IOKit.ps

/// Reports when the charger is plugged in or out, and when the battery runs low.
/// Does nothing on Macs without a battery.
@MainActor
final class PowerMonitor {
    private struct PowerState: Equatable {
        let isPluggedIn: Bool
        let percent: Int
    }

    private static let lowBatteryWarnings = [20, 10]

    private let onActivity: @MainActor (SystemActivity) -> Void
    private var lastState: PowerState?

    init(onActivity: @escaping @MainActor (SystemActivity) -> Void) {
        self.onActivity = onActivity
        lastState = Self.readState()

        // IOKit calls a plain C function, so `self` travels through the context pointer.
        // The monitor lives as long as the app, so it is never freed while the callback is registered.
        let context = Unmanaged.passUnretained(self).toOpaque()
        let source = IOPSNotificationCreateRunLoopSource({ context in
            guard let context else { return }
            let monitor = Unmanaged<PowerMonitor>.fromOpaque(context).takeUnretainedValue()
            MainActor.assumeIsolated { monitor.powerChanged() }
        }, context)?.takeRetainedValue()

        if let source {
            CFRunLoopAddSource(CFRunLoopGetMain(), source, .defaultMode)
        }
    }

    private func powerChanged() {
        guard let state = Self.readState() else { return }
        defer { lastState = state }
        guard let lastState, state != lastState else { return }

        let level = Double(state.percent) / 100
        if state.isPluggedIn != lastState.isPluggedIn {
            onActivity(SystemActivity(
                kind: .power,
                symbol: state.isPluggedIn ? "battery.100percent.bolt" : Self.batterySymbol(for: state.percent),
                title: state.isPluggedIn ? "Charging · \(state.percent)%" : "On Battery · \(state.percent)%",
                level: level
            ))
        } else if !state.isPluggedIn, Self.lowBatteryWarnings.contains(state.percent) {
            onActivity(SystemActivity(kind: .power, symbol: "battery.25percent", title: "Low Battery · \(state.percent)%", level: level))
        }
    }

    private static func readState() -> PowerState? {
        guard
            let info = IOPSCopyPowerSourcesInfo()?.takeRetainedValue(),
            let sources = IOPSCopyPowerSourcesList(info)?.takeRetainedValue() as? [CFTypeRef]
        else { return nil }

        for source in sources {
            guard
                let description = IOPSGetPowerSourceDescription(info, source)?.takeUnretainedValue() as? [String: Any],
                description[kIOPSTypeKey] as? String == kIOPSInternalBatteryType
            else { continue }

            let current = description[kIOPSCurrentCapacityKey] as? Int ?? 0
            let maximum = description[kIOPSMaxCapacityKey] as? Int ?? 100
            return PowerState(
                isPluggedIn: description[kIOPSPowerSourceStateKey] as? String == kIOPSACPowerValue,
                percent: maximum > 0 ? current * 100 / maximum : 0
            )
        }
        return nil
    }

    private static func batterySymbol(for percent: Int) -> String {
        switch percent {
        case ..<13: "battery.0percent"
        case ..<38: "battery.25percent"
        case ..<63: "battery.50percent"
        case ..<88: "battery.75percent"
        default: "battery.100percent"
        }
    }
}
