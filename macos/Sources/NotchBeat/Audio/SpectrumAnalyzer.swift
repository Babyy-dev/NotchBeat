import Accelerate
import CoreGraphics
import Foundation
import os

/// Turns raw audio into smoothed frequency-band levels (0...1).
///
/// `process` is called only on the audio queue; `levels` can be read from any thread.
final class SpectrumAnalyzer: @unchecked Sendable {
    private static let bandCount = 32
    private static let fftSize = 1024
    /// How long after the last real sound the levels stay valid before the waves fall back to animation.
    private static let signalTimeout: TimeInterval = 1.5

    private struct Output: Sendable {
        var levels = [Float](repeating: 0, count: SpectrumAnalyzer.bandCount)
        var lastSignal = Date.distantPast
    }

    // Audio-queue state.
    private let fft = vDSP.FFT(log2n: vDSP_Length(log2(Double(fftSize))), radix: .radix2, ofType: DSPSplitComplex.self)
    private let window = vDSP.window(ofType: Float.self, usingSequence: .hanningDenormalized, count: fftSize, isHalfWindow: false)
    private var samples: [Float] = []
    private var smoothed = [Float](repeating: 0, count: bandCount)

    // Shared with the UI.
    private let output = OSAllocatedUnfairLock(initialState: Output())

    func process(_ input: UnsafeBufferPointer<Float>, channels: Int, sampleRate: Double) {
        let channels = max(channels, 1)
        let frameCount = input.count / channels

        // Mix down to mono and keep the most recent `fftSize` samples.
        var loudness: Float = 0
        for frame in 0..<frameCount {
            var sum: Float = 0
            for channel in 0..<channels { sum += input[frame * channels + channel] }
            let sample = sum / Float(channels)
            samples.append(sample)
            loudness = max(loudness, abs(sample))
        }
        if samples.count > Self.fftSize { samples.removeFirst(samples.count - Self.fftSize) }
        guard samples.count == Self.fftSize else { return }

        let bands = bandLevels(from: magnitudes(), sampleRate: sampleRate)
        for index in bands.indices {
            // Rise quickly, fall slowly, so the bars feel punchy but not jittery.
            let weight: Float = bands[index] > smoothed[index] ? 0.6 : 0.15
            smoothed[index] += (bands[index] - smoothed[index]) * weight
        }

        let hasSignal = loudness > 0.001
        output.withLock { [smoothed] state in
            state.levels = smoothed
            if hasSignal { state.lastSignal = .now }
        }
    }

    /// Levels for `count` bars, mirrored so bass sits in the middle and treble at the edges.
    /// Returns `nil` when there has been no sound recently (paused, or permission denied).
    func levels(count: Int) -> [CGFloat]? {
        let state = output.withLock { $0 }
        guard Date.now.timeIntervalSince(state.lastSignal) < Self.signalTimeout else { return nil }

        let center = Double(count - 1) / 2
        return (0..<count).map { index in
            let distance = center > 0 ? abs(Double(index) - center) / center : 0
            let band = Int((distance * Double(Self.bandCount - 1)).rounded())
            return CGFloat(state.levels[band])
        }
    }

    // MARK: - DSP

    private func magnitudes() -> [Float] {
        let half = Self.fftSize / 2
        let windowed = vDSP.multiply(samples, window)
        var real = [Float](repeating: 0, count: half)
        var imaginary = [Float](repeating: 0, count: half)
        var result = [Float](repeating: 0, count: half)

        real.withUnsafeMutableBufferPointer { realPointer in
            imaginary.withUnsafeMutableBufferPointer { imaginaryPointer in
                guard let realBase = realPointer.baseAddress, let imaginaryBase = imaginaryPointer.baseAddress else { return }
                var split = DSPSplitComplex(realp: realBase, imagp: imaginaryBase)
                windowed.withUnsafeBytes { bytes in
                    guard let complex = bytes.bindMemory(to: DSPComplex.self).baseAddress else { return }
                    vDSP_ctoz(complex, 2, &split, 1, vDSP_Length(half))
                }
                let input = split
                fft?.forward(input: input, output: &split)
                vDSP.absolute(split, result: &result)
            }
        }
        return result
    }

    /// Groups FFT bins into log-spaced bands from 40 Hz to 16 kHz and maps their loudness to 0...1.
    private func bandLevels(from magnitudes: [Float], sampleRate: Double) -> [Float] {
        let binWidth = sampleRate / Double(Self.fftSize)
        let low = 40.0
        let high = min(16_000, sampleRate / 2)

        return (0..<Self.bandCount).map { band in
            let lowerFrequency = low * pow(high / low, Double(band) / Double(Self.bandCount))
            let upperFrequency = low * pow(high / low, Double(band + 1) / Double(Self.bandCount))
            let lowerBin = max(1, Int(lowerFrequency / binWidth))
            let upperBin = min(magnitudes.count - 1, max(lowerBin, Int(upperFrequency / binWidth)))

            let peak = magnitudes[lowerBin...upperBin].max() ?? 0
            let decibels = 20 * log10(max(peak / Float(Self.fftSize), 1e-9))
            // Treble is naturally quieter; tilt it up so all bars move.
            let tilted = decibels + Float(band) * 0.6
            return min(max((tilted + 60) / 50, 0), 1) // -60 dB…-10 dB → 0…1
        }
    }
}
