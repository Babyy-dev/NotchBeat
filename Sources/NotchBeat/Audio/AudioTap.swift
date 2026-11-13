import CoreAudio
import Foundation

/// Captures one app's audio output with a Core Audio process tap (macOS 14.2+).
/// The user keeps hearing the audio normally; we only receive a copy.
final class AudioTap {
    enum TapError: Error {
        case appNotPlayingAudio
        case noOutputDevice
        case coreAudio(OSStatus)
    }

    private var tapID = AudioObjectID(kAudioObjectUnknown)
    private var aggregateID = AudioObjectID(kAudioObjectUnknown)
    private var procID: AudioDeviceIOProcID?
    private let queue = DispatchQueue(label: "NotchBeat.AudioTap", qos: .userInteractive)

    /// Starts capturing audio from the app with `bundleID` (and its helper processes)
    /// and feeds it to `analyzer`.
    init(bundleID: String, analyzer: SpectrumAnalyzer) throws {
        do {
            try start(bundleID: bundleID, analyzer: analyzer)
        } catch {
            stop()
            throw error
        }
    }

    deinit {
        stop()
    }

    func stop() {
        if let procID {
            AudioDeviceStop(aggregateID, procID)
            AudioDeviceDestroyIOProcID(aggregateID, procID)
            self.procID = nil
        }
        if aggregateID != kAudioObjectUnknown {
            AudioHardwareDestroyAggregateDevice(aggregateID)
            aggregateID = AudioObjectID(kAudioObjectUnknown)
        }
        if tapID != kAudioObjectUnknown {
            AudioHardwareDestroyProcessTap(tapID)
            tapID = AudioObjectID(kAudioObjectUnknown)
        }
    }

    private func start(bundleID: String, analyzer: SpectrumAnalyzer) throws {
        let processes = Self.audioProcesses(matching: bundleID)
        guard !processes.isEmpty else { throw TapError.appNotPlayingAudio }

        // 1. A private tap that copies the app's audio without muting it.
        let description = CATapDescription(stereoMixdownOfProcesses: processes)
        description.uuid = UUID()
        description.muteBehavior = .unmuted
        description.isPrivate = true
        try check(AudioHardwareCreateProcessTap(description, &tapID))

        // 2. A private aggregate device that exposes the tap as an input we can read.
        let outputUID = try Self.defaultOutputDeviceUID()
        let aggregate: [String: Any] = [
            kAudioAggregateDeviceNameKey: "NotchBeat Tap",
            kAudioAggregateDeviceUIDKey: UUID().uuidString,
            kAudioAggregateDeviceMainSubDeviceKey: outputUID,
            kAudioAggregateDeviceIsPrivateKey: true,
            kAudioAggregateDeviceIsStackedKey: false,
            kAudioAggregateDeviceTapAutoStartKey: true,
            kAudioAggregateDeviceSubDeviceListKey: [[kAudioSubDeviceUIDKey: outputUID]],
            kAudioAggregateDeviceTapListKey: [[
                kAudioSubTapDriftCompensationKey: true,
                kAudioSubTapUIDKey: description.uuid.uuidString,
            ]],
        ]
        try check(AudioHardwareCreateAggregateDevice(aggregate as CFDictionary, &aggregateID))

        // 3. Read the audio as it arrives.
        let sampleRate = Self.sampleRate(of: tapID)
        try check(AudioDeviceCreateIOProcIDWithBlock(&procID, aggregateID, queue) { _, input, _, _, _ in
            let buffers = UnsafeMutableAudioBufferListPointer(UnsafeMutablePointer(mutating: input))
            guard let buffer = buffers.first, let data = buffer.mData else { return }
            let samples = UnsafeBufferPointer(
                start: data.assumingMemoryBound(to: Float.self),
                count: Int(buffer.mDataByteSize) / MemoryLayout<Float>.size
            )
            analyzer.process(samples, channels: Int(buffer.mNumberChannels), sampleRate: sampleRate)
        })
        try check(AudioDeviceStart(aggregateID, procID))
    }

    private func check(_ status: OSStatus) throws {
        guard status == noErr else { throw TapError.coreAudio(status) }
    }

    // MARK: - Core Audio lookups

    /// Audio process objects whose bundle ID starts with `bundleID` (includes helper processes).
    private static func audioProcesses(matching bundleID: String) -> [AudioObjectID] {
        CoreAudioProperty.objectIDs(of: CoreAudioProperty.systemObject, kAudioHardwarePropertyProcessObjectList)
            .filter { CoreAudioProperty.string(of: $0, kAudioProcessPropertyBundleID)?.hasPrefix(bundleID) == true }
    }

    private static func defaultOutputDeviceUID() throws -> String {
        guard
            let device = CoreAudioProperty.value(
                of: CoreAudioProperty.systemObject,
                kAudioHardwarePropertyDefaultSystemOutputDevice,
                initial: AudioObjectID(kAudioObjectUnknown)
            ),
            let uid = CoreAudioProperty.string(of: device, kAudioDevicePropertyDeviceUID)
        else { throw TapError.noOutputDevice }
        return uid
    }

    private static func sampleRate(of tapID: AudioObjectID) -> Double {
        let format = CoreAudioProperty.value(of: tapID, kAudioTapPropertyFormat, initial: AudioStreamBasicDescription())
        guard let rate = format?.mSampleRate, rate > 0 else { return 48_000 }
        return rate
    }
}
