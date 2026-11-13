<div align="center">

# 🎵 NotchBeat

**A Dynamic Island–style music player for your Mac's notch, and now for Windows too.**

See what's playing in Spotify, Apple Music and more, with wave bars that move with the music and take their color from the album art.

![macOS](https://img.shields.io/badge/macOS-14.2%2B-black?logo=apple&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white)
![Swift](https://img.shields.io/badge/Swift-6.0-F05138?logo=swift&logoColor=white)
![C#](https://img.shields.io/badge/C%23-.NET%208-512BD4?logo=dotnet&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green)
![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen)

[Features](#-features) • [macOS](#-macos) • [Windows](#-windows) • [Usage](#-usage) • [Customization](#-customization) • [Contributing](#-contributing) • [License](#-license)

</div>

<!-- Tip: add a screenshot or GIF here, e.g. ![NotchBeat](docs/demo.gif) -->

---

## ✨ Features

|                              |                                                                                                          |
| ---------------------------- | -------------------------------------------------------------------------------------------------------- |
| 🎧 **Now playing**           | A spinning vinyl-style cover peeks out on the left, waves roll on the right                              |
| 🌊 **Music-reactive waves**  | Follow the real audio, with a smooth animation fallback if audio access is off                           |
| 🎨 **Album-art colors**      | Waves and accents take their colors from the current cover                                               |
| 🪄 **Song-change pop**       | A short bouncy pop shows the new title and artist                                                        |
| 🖱️ **Hover to expand**       | A Now Bar–style player with a scrolling title, controls, a wavy draggable progress bar and synced lyrics |
| 🎤 **Karaoke lyrics**        | Full lyrics that fill with color as they're sung. Click any line to jump to it                           |
| 🔔 **System pop-ups**        | Volume changes, headphones and other audio devices connecting, charging and low battery                  |
| 🖐️ **Gestures**              | Swipe left/right to skip tracks, scroll up/down to change volume                                         |
| 🎭 **Themes**                | Now Bar (blurred album art), Classic (pure black), Neon (glowing outline), Glass (frosted)               |
| 〰️ **Wave styles**           | Flow, Bars, Line, Dots or Circle                                                                         |
| ⚙️ **Lives in the corner**   | Settings, Quit and Launch at login from the menu bar (macOS) or tray icon (Windows)                      |

### Platform differences

|                    | 🍎 macOS                                    | 🪟 Windows                                                         |
| ------------------ | ------------------------------------------- | ------------------------------------------------------------------ |
| **Where it sits**  | Around the notch (or top center)            | **Top left, top center or top right**, any distance from the edge  |
| **Screen**         | The built-in screen                         | Any screen you pick                                                |
| **Music apps**     | Spotify and Apple Music                     | Spotify, Apple Music, browsers, and any app in the media controls  |
| **Compact view**   | Cover and waves around the notch            | Cover, scrolling title and waves                                   |
| **Reactive waves** | Listens to the music app only               | Listens to everything your speakers play                           |
| **Volume**         | The music app's volume                      | The system volume                                                  |
| **Full screen**    | —                                           | Hides itself during full-screen videos, games and presentations    |

## 🍎 macOS

**Requirements:** macOS 14.2 or later, Spotify and/or Apple Music, and the Swift 6 toolchain (Xcode, or just the Command Line Tools: `xcode-select --install`). Works on Macs with or without a notch.

```sh
git clone https://github.com/<your-username>/notchbeat.git
cd notchbeat/macos
./scripts/build-app.sh
open build/NotchBeat.app
```

To keep it around, drag `macos/build/NotchBeat.app` into your `/Applications` folder.

macOS asks for these permissions once:

| Permission                       | Why                                                                      |
| -------------------------------- | ------------------------------------------------------------------------ |
| **Automation** (Spotify / Music) | Read the current song and send play, skip, seek and volume commands      |
| **System Audio Recording**       | Let the waves react to the music. Audio is analyzed live and never saved |

You can change them later in **System Settings → Privacy & Security**.

## 🪟 Windows

**Requirements:** Windows 10 (version 2004 or later) or Windows 11, and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build.

```powershell
git clone https://github.com/<your-username>/notchbeat.git
cd notchbeat\windows
.\build.ps1 -SelfContained
.\publish\NotchBeat.exe
```

- `-SelfContained` makes an exe that runs on any Windows 10/11 PC as is. Without it, the exe is much smaller but needs the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
- No permissions are needed. Music apps report what they're playing to Windows, the same info shown in the volume flyout's media controls.
- To pick the position, click the **tray icon** (next to the clock) → **Settings → Position**.

> **Tip:** each push to `main` builds both apps on GitHub Actions. Download the ready-made `NotchBeat-Windows` exe from the run's **Artifacts**.

## 🎮 Usage

| State                       | What you see                                    |
| --------------------------- | ----------------------------------------------- |
| **Idle**                    | Hidden (behind the notch on Macs that have one) |
| **Playing**                 | Cover on the left, waves on the right           |
| **Hover**                   | Expands into the full player                    |
| **Click the lyric line**    | Opens full karaoke lyrics                       |
| **Swipe ← / →** (expanded)  | Previous / next track                           |
| **Scroll ↑ / ↓** (expanded) | Volume up / down                                |

On Windows, swiping means two-finger sideways scrolling on a touchpad, or a mouse with a tilt wheel.

## 🎨 Customization

Every size, color, corner radius, animation, wave and gesture setting lives in one file per platform:

- macOS: [`macos/Sources/NotchBeat/Theme/Theme.swift`](macos/Sources/NotchBeat/Theme/Theme.swift)
- Windows: [`windows/NotchBeat/Theme/Theme.cs`](windows/NotchBeat/Theme/Theme.cs)

## 🏗️ Project structure

```
macos/                     Swift + SwiftUI app
├── Sources/NotchBeat/
│   ├── App/               Entry point, wiring, menu bar icon
│   ├── Island/            Notch window, hover and gestures, and the island views
│   ├── Music/             Music app connections (Spotify, Apple Music) and the now-playing state
│   ├── Audio/             Audio capture and frequency analysis for reactive waves
│   ├── Waves/             Wave styles and drawing
│   ├── Lyrics/            Synced lyrics from lrclib.net
│   ├── System/            Volume, audio device and battery monitors for system pop-ups
│   ├── Settings/          User preferences and the Settings window
│   └── Theme/             All visual settings, the themes, and album-art color extraction
├── Resources/             Info.plist for the .app bundle
└── scripts/               Build script

windows/                   C# + WPF app (same structure, same names)
├── NotchBeat/
│   ├── App/               Tray icon and Windows API calls
│   ├── Island/            Island window, hover and gestures, and the custom-drawn views
│   ├── Music/             Windows media sessions and the now-playing state
│   ├── Audio/             Loopback capture and frequency analysis for reactive waves
│   ├── Waves/             Wave styles and drawing
│   ├── Lyrics/            Synced lyrics from lrclib.net
│   ├── Monitors/          Volume, audio device and battery monitors for system pop-ups
│   ├── Settings/          User preferences and the Settings window
│   └── Theme/             All visual settings, the themes, and album-art color extraction
└── build.ps1              Build script
```

Both apps share the same design. Data flows one way:

```
Music apps  →  NowPlaying  →  IslandModel  →  Views
```

### Adding another music app

- **macOS:** create a type that conforms to `MusicPlayer` (see [`SpotifyPlayer.swift`](macos/Sources/NotchBeat/Music/SpotifyPlayer.swift)) and add it to `NowPlaying.players`.
- **Windows:** nothing to do. Any app that shows up in the Windows media controls already works.

## 🛠️ Development

**macOS**

```sh
cd macos
swift build      # debug build
swift run        # build and run
```

> **Note:** with `swift run`, permissions are credited to your terminal app, and Launch at login only works from the `.app` bundle. Use `./scripts/build-app.sh` to test the real app.

**Windows**

```powershell
cd windows\NotchBeat
dotnet run
```

Or open `windows/NotchBeat/NotchBeat.csproj` in Visual Studio 2022 or JetBrains Rider.

## 🤝 Contributing

Contributions, bug reports and feature ideas are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) to get started.

1. Fork the repo
2. Create a branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m "Add my feature"`
4. Push the branch: `git push origin feature/my-feature`
5. Open a Pull Request

## 🔒 Privacy

- Lyrics lookups send the song title, artist, album and duration to [lrclib.net](https://lrclib.net). Turn lyrics off in Settings to disable this.
- On macOS, Spotify artwork is downloaded from Spotify's image servers. On Windows, artwork comes straight from the music app.
- Nothing else leaves your computer. Audio is analyzed live and never recorded.

## 🙏 Acknowledgements

- [LRCLIB](https://lrclib.net) for free synced lyrics
- [NAudio](https://github.com/naudio/NAudio) for Windows audio capture

## 📄 License

Released under the [MIT License](LICENSE).

<div align="center">

If you like NotchBeat, consider giving it a ⭐

</div>
