import AudioToolbox
import CoreAudio
import Foundation

/// Reports system volume changes and output device switches (e.g. AirPods connecting).
@MainActor
final class AudioOutputMonitor {
    private let onActivity: @MainActor (SystemActivity) -> Void
    private var deviceID = AudioObjectID(kAudioObjectUnknown)
    private var volumeListener: AudioObjectPropertyListenerBlock?
    /// Volume events right after a device switch are echoes of the switch, not the user changing volume.
    private var ignoreVolumeUntil = Date.distantPast

    private let volumeAddresses = [
        CoreAudioProperty.address(kAudioHardwareServiceDeviceProperty_VirtualMainVolume, scope: kAudioDevicePropertyScopeOutput),
        CoreAudioProperty.address(kAudioDevicePropertyMute, scope: kAudioDevicePropertyScopeOutput),
    ]

    init(onActivity: @escaping @MainActor (SystemActivity) -> Void) {
        self.onActivity = onActivity

        var address = CoreAudioProperty.address(kAudioHardwarePropertyDefaultOutputDevice)
        AudioObjectAddPropertyListenerBlock(CoreAudioProperty.systemObject, &address, .main) { [weak self] _, _ in
            MainActor.assumeIsolated { self?.outputDeviceChanged() }
        }
        watchVolume(of: Self.defaultOutputDevice())
    }

    private func outputDeviceChanged() {
        let device = Self.defaultOutputDevice()
        guard device != deviceID else { return }
        watchVolume(of: device)
        ignoreVolumeUntil = .now + 0.5

        let name = CoreAudioProperty.string(of: device, kAudioObjectPropertyName) ?? "Speakers"
        onActivity(SystemActivity(kind: .outputDevice, symbol: Self.symbol(for: device, named: name), title: name))
    }

    private func volumeChanged() {
        guard Date.now >= ignoreVolumeUntil else { return }
        let output = kAudioDevicePropertyScopeOutput
        let volume = CoreAudioProperty.value(
            of: deviceID, kAudioHardwareServiceDeviceProperty_VirtualMainVolume, scope: output, initial: Float32(0)
        ) ?? 0
        let isMuted = CoreAudioProperty.value(of: deviceID, kAudioDevicePropertyMute, scope: output, initial: UInt32(0)) == 1
        let level = isMuted ? 0 : Double(volume)

        onActivity(SystemActivity(kind: .volume, symbol: Self.volumeSymbol(for: level), title: "Volume", level: level))
    }

    /// Moves the volume listeners to `device`.
    private func watchVolume(of device: AudioObjectID) {
        if let volumeListener {
            for var address in volumeAddresses {
                AudioObjectRemovePropertyListenerBlock(deviceID, &address, .main, volumeListener)
            }
        }

        deviceID = device
        let listener: AudioObjectPropertyListenerBlock = { [weak self] _, _ in
            MainActor.assumeIsolated { self?.volumeChanged() }
        }
        for var address in volumeAddresses {
            AudioObjectAddPropertyListenerBlock(device, &address, .main, listener)
        }
        volumeListener = listener
    }

    private static func defaultOutputDevice() -> AudioObjectID {
        CoreAudioProperty.value(
            of: CoreAudioProperty.systemObject,
            kAudioHardwarePropertyDefaultOutputDevice,
            initial: AudioObjectID(kAudioObjectUnknown)
        ) ?? AudioObjectID(kAudioObjectUnknown)
    }

    private static func symbol(for device: AudioObjectID, named name: String) -> String {
        let lowercased = name.lowercased()
        if lowercased.contains("airpods max") { return "airpodsmax" }
        if lowercased.contains("airpods pro") { return "airpodspro" }
        if lowercased.contains("airpods") { return "airpods" }

        let transport = CoreAudioProperty.value(of: device, kAudioDevicePropertyTransportType, initial: UInt32(0))
        if transport == kAudioDeviceTransportTypeBluetooth || transport == kAudioDeviceTransportTypeBluetoothLE {
            return "headphones"
        }
        return "hifispeaker.fill"
    }

    private static func volumeSymbol(for level: Double) -> String {
        switch level {
        case 0: "speaker.slash.fill"
        case ..<0.34: "speaker.wave.1.fill"
        case ..<0.67: "speaker.wave.2.fill"
        default: "speaker.wave.3.fill"
        }
    }
}
