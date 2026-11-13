using System.Threading;
using System.Windows;

namespace NotchBeat;

/// <summary>Creates the long-lived objects and wires them together.</summary>
public partial class App : Application
{
    private Mutex? _singleInstance;
    private SystemAudio? _systemAudio;
    private PowerMonitor? _powerMonitor;
    private NowPlaying? _nowPlaying;
    private TrayIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Only one island at a time.
        _singleInstance = new Mutex(initiallyOwned: true, "NotchBeat.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        var preferences = Preferences.Load();
        _systemAudio = new SystemAudio(Dispatcher);
        _powerMonitor = new PowerMonitor();
        _nowPlaying = new NowPlaying(preferences, _systemAudio, Dispatcher);
        var island = new IslandModel(_nowPlaying, preferences);

        new IslandWindow(island).Show();
        _trayIcon = new TrayIcon(preferences);

        _systemAudio.Activity += island.Show;
        _systemAudio.DeviceChanged += _nowPlaying.Visualizer.Restart;
        _powerMonitor.Activity += island.Show;
        preferences.Changed += _nowPlaying.PreferencesChanged;
        _nowPlaying.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _nowPlaying?.Visualizer.Dispose();
        _powerMonitor?.Dispose();
        _systemAudio?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
