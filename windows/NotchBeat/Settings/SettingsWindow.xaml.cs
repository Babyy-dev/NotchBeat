using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Screen = System.Windows.Forms.Screen;

namespace NotchBeat;

public partial class SettingsWindow : Window
{
    private readonly Preferences _preferences;

    public SettingsWindow(Preferences preferences)
    {
        InitializeComponent();
        _preferences = preferences;

        Bind(SourcePicker, Enum.GetValues<MusicSource>(), s => s.Title(), preferences.Source, v => preferences.Source = v);
        Bind(PositionPicker, Enum.GetValues<IslandPosition>(), p => p.Title(), preferences.Position, v =>
        {
            preferences.Position = v;
            UpdateMarginRow();
        });
        Bind(ThemePicker, Enum.GetValues<IslandTheme>(), t => t.Title(), preferences.Theme, v => preferences.Theme = v);
        Bind(WaveStylePicker, Enum.GetValues<WaveStyle>(), w => w.ToString(), preferences.WaveStyle, v => preferences.WaveStyle = v);

        // Screens: empty name means "whichever is the main screen".
        var screens = new[] { "" }.Concat(Screen.AllScreens.Select(s => s.DeviceName)).ToArray();
        string current = screens.Contains(preferences.Display) ? preferences.Display : "";
        Bind(DisplayPicker, screens, ScreenTitle, current, v => preferences.Display = v);

        MarginSlider.Value = preferences.EdgeMargin;
        MarginValue.Text = $"{preferences.EdgeMargin:0} px";
        MarginSlider.ValueChanged += (_, e) =>
        {
            preferences.EdgeMargin = e.NewValue;
            MarginValue.Text = $"{e.NewValue:0} px";
        };
        UpdateMarginRow();

        Bind(SpinArtworkToggle, preferences.SpinArtwork, v => preferences.SpinArtwork = v);
        Bind(ReactiveWavesToggle, preferences.ReactiveWaves, v => preferences.ReactiveWaves = v);
        Bind(ShowLyricsToggle, preferences.ShowLyrics, v => preferences.ShowLyrics = v);
        Bind(SystemActivityToggle, preferences.ShowSystemActivity, v => preferences.ShowSystemActivity = v);
        Bind(LaunchAtLoginToggle, preferences.LaunchAtLogin, v => preferences.LaunchAtLogin = v);
    }

    /// <summary>The distance slider only matters for the left and right positions.</summary>
    private void UpdateMarginRow() =>
        MarginRow.IsEnabled = _preferences.Position != IslandPosition.TopCenter;

    private static string ScreenTitle(string deviceName)
    {
        if (deviceName.Length == 0) return "Main screen";
        var screens = Screen.AllScreens;
        int index = Array.FindIndex(screens, s => s.DeviceName == deviceName);
        var screen = screens[index];
        return $"Screen {index + 1} ({screen.Bounds.Width}×{screen.Bounds.Height})";
    }

    private static void Bind<T>(ComboBox picker, T[] values, Func<T, string> title, T selected, Action<T> onChange)
    {
        picker.ItemsSource = values.Select(title).ToList();
        picker.SelectedIndex = Math.Max(0, Array.IndexOf(values, selected));
        picker.SelectionChanged += (_, _) =>
        {
            if (picker.SelectedIndex >= 0) onChange(values[picker.SelectedIndex]);
        };
    }

    private static void Bind(CheckBox toggle, bool value, Action<bool> onChange)
    {
        toggle.IsChecked = value;
        toggle.Checked += (_, _) => onChange(true);
        toggle.Unchecked += (_, _) => onChange(false);
    }
}
