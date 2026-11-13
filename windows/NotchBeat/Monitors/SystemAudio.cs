using System;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace NotchBeat;

/// <summary>
/// The default speakers or headphones: reads and sets their volume,
/// and reports volume changes and newly connected output devices.
/// </summary>
public sealed class SystemAudio : IMMNotificationClient, IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;
    private string? _deviceId;

    /// <summary>A system pop-up to show.</summary>
    public event Action<SystemActivity>? Activity;
    /// <summary>The default output device changed.</summary>
    public event Action? DeviceChanged;

    public SystemAudio(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        AttachDefaultDevice();
        _enumerator.RegisterEndpointNotificationCallback(this);
    }

    /// <summary>Master volume, 0...100.</summary>
    public double Volume
    {
        get
        {
            try { return (_device?.AudioEndpointVolume.MasterVolumeLevelScalar ?? 0.5f) * 100; }
            catch (Exception) { return 50; }
        }
        set
        {
            if (_device == null) return;
            try
            {
                float scalar = (float)Math.Clamp(value / 100, 0, 1);
                _device.AudioEndpointVolume.MasterVolumeLevelScalar = scalar;
                if (scalar > 0 && _device.AudioEndpointVolume.Mute) _device.AudioEndpointVolume.Mute = false;
            }
            catch (Exception) { /* Device went away. */ }
        }
    }

    public void Dispose()
    {
        try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch (Exception) { }
        DetachDevice();
        _enumerator.Dispose();
    }

    private void AttachDefaultDevice()
    {
        DetachDevice();
        try
        {
            _device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _deviceId = _device.ID;
            _device.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
        }
        catch (Exception)
        {
            _device = null; // No speakers connected.
            _deviceId = null;
        }
    }

    private void DetachDevice()
    {
        if (_device == null) return;
        try { _device.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification; } catch (Exception) { }
        _device.Dispose();
        _device = null;
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        double level = data.Muted ? 0 : data.MasterVolume;
        string glyph = level == 0 ? Theme.Glyphs.Mute
            : level < 0.34 ? Theme.Glyphs.VolumeLow
            : level < 0.67 ? Theme.Glyphs.VolumeMedium
            : Theme.Glyphs.VolumeHigh;
        _dispatcher.BeginInvoke(() =>
            Activity?.Invoke(new SystemActivity(SystemActivity.ActivityKind.Volume, glyph, "Volume", level)));
    }

    // IMMNotificationClient (called on a background thread)

    void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow != DataFlow.Render || role != Role.Multimedia) return;
        _dispatcher.BeginInvoke(() =>
        {
            if (defaultDeviceId == _deviceId) return;
            AttachDefaultDevice();
            DeviceChanged?.Invoke();
            if (_device == null) return;

            string name = _device.FriendlyName;
            Activity?.Invoke(new SystemActivity(SystemActivity.ActivityKind.OutputDevice, GlyphFor(name), ShortName(name)));
        });
    }

    void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState) { }
    void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) { }
    void IMMNotificationClient.OnDeviceRemoved(string deviceId) { }
    void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    private static string GlyphFor(string name)
    {
        string lower = name.ToLowerInvariant();
        if (lower.Contains("headphone") || lower.Contains("headset") || lower.Contains("airpods") ||
            lower.Contains("buds") || lower.Contains("earphone"))
        {
            return Theme.Glyphs.Headphones;
        }
        return lower.Contains("bluetooth") ? Theme.Glyphs.Bluetooth : Theme.Glyphs.Speakers;
    }

    /// <summary>"Headphones (WH-1000XM4 Stereo)" reads better as "WH-1000XM4 Stereo".</summary>
    private static string ShortName(string name)
    {
        int open = name.IndexOf('(');
        int close = name.LastIndexOf(')');
        return open >= 0 && close > open + 1 ? name[(open + 1)..close] : name;
    }
}
