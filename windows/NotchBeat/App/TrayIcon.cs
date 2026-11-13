using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NotchBeat;

/// <summary>The tray icon (next to the clock) with Settings and Quit.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Icon _image;
    private readonly Preferences _preferences;
    private SettingsWindow? _settings;

    public TrayIcon(Preferences preferences)
    {
        _preferences = preferences;
        _image = DrawIcon();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit NotchBeat", null, (_, _) => System.Windows.Application.Current.Shutdown());

        _icon = new NotifyIcon
        {
            Icon = _image,
            Text = "NotchBeat",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) OpenSettings();
        };
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        DestroyIcon(_image.Handle);
        _image.Dispose();
    }

    private void OpenSettings()
    {
        if (_settings is { IsLoaded: true })
        {
            _settings.Activate();
            return;
        }
        _settings = new SettingsWindow(_preferences);
        _settings.Show();
        _settings.Activate();
    }

    /// <summary>A small waveform in the default accent color, drawn at runtime so the repo needs no binary .ico.</summary>
    private static Icon DrawIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            var accent = Theme.Colors.FallbackAccent;
            using var background = new SolidBrush(Color.FromArgb(accent.R, accent.G, accent.B));
            graphics.FillEllipse(background, 0, 0, 31, 31);

            using var bar = new SolidBrush(Color.White);
            float[] heights = { 8, 16, 22, 14, 8 };
            for (int i = 0; i < heights.Length; i++)
            {
                float x = 7 + i * 4;
                float y = 16 - heights[i] / 2;
                using var path = RoundedBar(x, y, 2.6f, heights[i]);
                graphics.FillPath(bar, path);
            }
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static GraphicsPath RoundedBar(float x, float y, float width, float height)
    {
        var path = new GraphicsPath();
        path.AddArc(x, y, width, width, 180, 180);
        path.AddArc(x, y + height - width, width, width, 0, 180);
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
