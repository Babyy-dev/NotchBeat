using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace NotchBeat;

/// <summary>
/// Listens to what the speakers are playing (WASAPI loopback) while music plays,
/// and serves live levels to the waves. Nothing is recorded or saved.
/// </summary>
public sealed class AudioVisualizer : IDisposable
{
    private readonly SpectrumAnalyzer _analyzer = new();
    private WasapiLoopbackCapture? _capture;
    private float[] _buffer = Array.Empty<float>();

    /// <summary>Starts listening when <paramref name="enabled"/>, stops otherwise. Retries if the last start failed.</summary>
    public void Update(bool enabled)
    {
        if (enabled && _capture == null) Start();
        else if (!enabled && _capture != null) Stop();
    }

    /// <summary>Follows a new default output device (e.g. headphones were connected).</summary>
    public void Restart()
    {
        if (_capture == null) return;
        Stop();
        Start();
    }

    /// <summary>Live levels, or <c>null</c> when there is no real audio to show.</summary>
    public double[]? Levels(int count) => _analyzer.Levels(count);

    public void Dispose() => Stop();

    private void Start()
    {
        try
        {
            _analyzer.Reset();
            var capture = new WasapiLoopbackCapture();
            capture.DataAvailable += OnDataAvailable;
            capture.RecordingStopped += (_, _) => { if (ReferenceEquals(_capture, capture)) _capture = null; };
            capture.StartRecording();
            _capture = capture;
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException or ArgumentException)
        {
            // No output device, or it's busy. The waves fall back to animation.
            _capture = null;
        }
    }

    private void Stop()
    {
        var capture = _capture;
        _capture = null;
        if (capture == null) return;
        capture.DataAvailable -= OnDataAvailable;
        try { capture.StopRecording(); } catch (Exception) { /* Already stopped. */ }
        capture.Dispose();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (sender is not WasapiLoopbackCapture capture || e.BytesRecorded == 0) return;
        var format = capture.WaveFormat;

        int count;
        if (format.BitsPerSample == 32 && format.Encoding is WaveFormatEncoding.IeeeFloat or WaveFormatEncoding.Extensible)
        {
            count = e.BytesRecorded / 4;
            EnsureBuffer(count);
            MemoryMarshal.Cast<byte, float>(e.Buffer.AsSpan(0, count * 4)).CopyTo(_buffer);
        }
        else if (format.BitsPerSample == 16)
        {
            count = e.BytesRecorded / 2;
            EnsureBuffer(count);
            var shorts = MemoryMarshal.Cast<byte, short>(e.Buffer.AsSpan(0, count * 2));
            for (int i = 0; i < count; i++) _buffer[i] = shorts[i] / 32768f;
        }
        else
        {
            return; // Unusual format; the waves fall back to animation.
        }

        _analyzer.Process(_buffer.AsSpan(0, count), format.Channels, format.SampleRate);
    }

    private void EnsureBuffer(int count)
    {
        if (_buffer.Length < count) _buffer = new float[count];
    }
}
