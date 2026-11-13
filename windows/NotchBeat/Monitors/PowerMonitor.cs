using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace NotchBeat;

/// <summary>Reports charger plugged in or out, and low battery. Does nothing on PCs without a battery.</summary>
public sealed class PowerMonitor : IDisposable
{
    private const int LowBatteryPercent = 20;

    private readonly DispatcherTimer _timer;
    private bool? _wasPluggedIn;
    private bool _warnedLow;

    public event Action<SystemActivity>? Activity;

    public PowerMonitor()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Check();
        Check();
        _timer.Start();
    }

    public void Dispose() => _timer.Stop();

    private void Check()
    {
        if (!GetSystemPowerStatus(out var status)) return;
        // 128 = no battery, 255 = unknown.
        if (status.BatteryFlag == 128 || status.BatteryFlag == 255 || status.BatteryLifePercent > 100) return;

        bool isPluggedIn = status.ACLineStatus == 1;
        int percent = status.BatteryLifePercent;
        double level = percent / 100.0;

        if (_wasPluggedIn is { } was && was != isPluggedIn)
        {
            Activity?.Invoke(new SystemActivity(
                SystemActivity.ActivityKind.Power,
                isPluggedIn ? Theme.Glyphs.BatteryCharging : Theme.Glyphs.Battery(percent),
                isPluggedIn ? $"Charging · {percent}%" : $"On Battery · {percent}%",
                level));
        }
        _wasPluggedIn = isPluggedIn;

        if (isPluggedIn || percent > LowBatteryPercent)
        {
            _warnedLow = false;
        }
        else if (!_warnedLow)
        {
            _warnedLow = true;
            Activity?.Invoke(new SystemActivity(
                SystemActivity.ActivityKind.Power, Theme.Glyphs.Battery(percent), $"Low Battery · {percent}%", level));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);
}
