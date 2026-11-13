using System;
using System.Diagnostics;

namespace NotchBeat;

/// <summary>
/// Turns raw audio into smoothed frequency-band levels (0...1).
/// <see cref="Process"/> is called on the audio thread; <see cref="Levels"/> can be read from any thread.
/// </summary>
public sealed class SpectrumAnalyzer
{
    private const int BandCount = 32;
    private const int FftSize = 1024;
    /// <summary>How long after the last real sound the levels stay valid before the waves fall back to animation.</summary>
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(1.5);

    // Audio-thread state.
    private readonly float[] _window = new float[FftSize];
    private readonly float[] _samples = new float[FftSize];
    private int _sampleCount;
    private int _writeIndex;
    private readonly float[] _smoothed = new float[BandCount];
    private readonly double[] _real = new double[FftSize];
    private readonly double[] _imaginary = new double[FftSize];

    // Shared with the UI.
    private readonly object _lock = new();
    private readonly float[] _levels = new float[BandCount];
    private long _lastSignalTicks = -1;

    public SpectrumAnalyzer()
    {
        for (int i = 0; i < FftSize; i++)
        {
            _window[i] = (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / FftSize))); // Hann
        }
    }

    /// <summary>Feeds interleaved samples (-1...1) into the analyzer.</summary>
    public void Process(ReadOnlySpan<float> input, int channels, int sampleRate)
    {
        channels = Math.Max(channels, 1);
        int frameCount = input.Length / channels;

        // Mix down to mono and keep the most recent FftSize samples in a ring buffer.
        float loudness = 0;
        for (int frame = 0; frame < frameCount; frame++)
        {
            float sum = 0;
            for (int channel = 0; channel < channels; channel++) sum += input[frame * channels + channel];
            float sample = sum / channels;
            _samples[_writeIndex] = sample;
            _writeIndex = (_writeIndex + 1) % FftSize;
            _sampleCount = Math.Min(_sampleCount + 1, FftSize);
            loudness = Math.Max(loudness, Math.Abs(sample));
        }
        if (_sampleCount < FftSize) return;

        var bands = BandLevels(Magnitudes(), sampleRate);
        for (int i = 0; i < BandCount; i++)
        {
            // Rise quickly, fall slowly, so the bars feel punchy but not jittery.
            float weight = bands[i] > _smoothed[i] ? 0.6f : 0.15f;
            _smoothed[i] += (bands[i] - _smoothed[i]) * weight;
        }

        lock (_lock)
        {
            Array.Copy(_smoothed, _levels, BandCount);
            if (loudness > 0.001f) _lastSignalTicks = Stopwatch.GetTimestamp();
        }
    }

    /// <summary>
    /// Levels for <paramref name="count"/> bars, mirrored so bass sits in the middle and treble at the edges.
    /// Returns <c>null</c> when there has been no sound recently (paused, or capture unavailable).
    /// </summary>
    public double[]? Levels(int count)
    {
        float[] snapshot = new float[BandCount];
        lock (_lock)
        {
            if (_lastSignalTicks < 0 || Stopwatch.GetElapsedTime(_lastSignalTicks) > SignalTimeout) return null;
            Array.Copy(_levels, snapshot, BandCount);
        }

        double center = (count - 1) / 2.0;
        var result = new double[count];
        for (int i = 0; i < count; i++)
        {
            double distance = center > 0 ? Math.Abs(i - center) / center : 0;
            int band = (int)Math.Round(distance * (BandCount - 1));
            result[i] = snapshot[band];
        }
        return result;
    }

    /// <summary>Clears the history, e.g. when the audio device changes.</summary>
    public void Reset()
    {
        _sampleCount = 0;
        _writeIndex = 0;
        Array.Clear(_smoothed);
    }

    // DSP

    private double[] Magnitudes()
    {
        for (int i = 0; i < FftSize; i++)
        {
            // Oldest sample first.
            _real[i] = _samples[(_writeIndex + i) % FftSize] * _window[i];
            _imaginary[i] = 0;
        }
        Fft.Forward(_real, _imaginary);

        int half = FftSize / 2;
        var result = new double[half];
        for (int i = 0; i < half; i++)
        {
            // Doubled to match the one-sided spectrum scale used by the macOS version.
            result[i] = 2 * Math.Sqrt(_real[i] * _real[i] + _imaginary[i] * _imaginary[i]);
        }
        return result;
    }

    /// <summary>Groups FFT bins into log-spaced bands from 40 Hz to 16 kHz and maps their loudness to 0...1.</summary>
    private static float[] BandLevels(double[] magnitudes, int sampleRate)
    {
        double binWidth = (double)sampleRate / FftSize;
        double low = 40.0;
        double high = Math.Min(16_000, sampleRate / 2.0);
        var bands = new float[BandCount];

        for (int band = 0; band < BandCount; band++)
        {
            double lowerFrequency = low * Math.Pow(high / low, (double)band / BandCount);
            double upperFrequency = low * Math.Pow(high / low, (double)(band + 1) / BandCount);
            int lowerBin = Math.Max(1, (int)(lowerFrequency / binWidth));
            int upperBin = Math.Min(magnitudes.Length - 1, Math.Max(lowerBin, (int)(upperFrequency / binWidth)));

            double peak = 0;
            for (int bin = lowerBin; bin <= upperBin; bin++) peak = Math.Max(peak, magnitudes[bin]);
            double decibels = 20 * Math.Log10(Math.Max(peak / FftSize, 1e-9));
            // Treble is naturally quieter; tilt it up so all bars move.
            double tilted = decibels + band * 0.6;
            bands[band] = (float)Math.Clamp((tilted + 60) / 50, 0, 1); // -60 dB…-10 dB → 0…1
        }
        return bands;
    }

    /// <summary>In-place iterative radix-2 FFT.</summary>
    private static class Fft
    {
        public static void Forward(double[] real, double[] imaginary)
        {
            int n = real.Length;

            // Bit-reversal permutation.
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    (real[i], real[j]) = (real[j], real[i]);
                    (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
                }
            }

            for (int length = 2; length <= n; length <<= 1)
            {
                double angle = -2 * Math.PI / length;
                double stepReal = Math.Cos(angle), stepImaginary = Math.Sin(angle);
                for (int start = 0; start < n; start += length)
                {
                    double wReal = 1, wImaginary = 0;
                    for (int k = 0; k < length / 2; k++)
                    {
                        int even = start + k, odd = even + length / 2;
                        double oddReal = real[odd] * wReal - imaginary[odd] * wImaginary;
                        double oddImaginary = real[odd] * wImaginary + imaginary[odd] * wReal;
                        real[odd] = real[even] - oddReal;
                        imaginary[odd] = imaginary[even] - oddImaginary;
                        real[even] += oddReal;
                        imaginary[even] += oddImaginary;
                        (wReal, wImaginary) = (wReal * stepReal - wImaginary * stepImaginary,
                                               wReal * stepImaginary + wImaginary * stepReal);
                    }
                }
            }
        }
    }
}
