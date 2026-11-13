<div align="center">

# 🎵 NotchBeat

**A Dynamic Island for your Mac's notch.**

See what's playing in Spotify or Apple Music, with wave bars that move with the music and take their color from the album art.

![macOS](https://img.shields.io/badge/macOS-14.2%2B-black?logo=apple&logoColor=white)
![Swift](https://img.shields.io/badge/Swift-6.0-F05138?logo=swift&logoColor=white)
![SwiftUI](https://img.shields.io/badge/UI-SwiftUI-0A84FF)
![License](https://img.shields.io/badge/license-MIT-green)
![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen)

[Features](#-features) • [Installation](#-installation) • [Usage](#-usage) • [Customization](#-customization) • [Contributing](#-contributing) • [License](#-license)

</div>

<!-- Tip: add a screenshot or GIF here, e.g. ![NotchBeat](docs/demo.gif) -->

---

## ✨ Features

|                              |                                                                                                          |
| ---------------------------- | -------------------------------------------------------------------------------------------------------- |
| 🎧 **Now playing**           | A spinning vinyl-style cover peeks out on the left of the notch, waves roll on the right                 |
| 🌊 **Music-reactive waves**  | Follow the real audio, with a smooth animation fallback if audio access is off                           |
| 🎨 **Album-art colors**      | Waves and accents take their colors from the current cover                                               |
| 🪄 **Song-change pop**       | A short bouncy pop shows the new title and artist                                                        |
| 🖱️ **Hover to expand**       | A Now Bar–style player with a scrolling title, controls, a wavy draggable progress bar and synced lyrics |
| 🎤 **Karaoke lyrics**        | Full lyrics that fill with color as they're sung. Click any line to jump to it                           |
| 🔔 **System pop-ups**        | Volume changes, AirPods and other audio devices connecting, charging and low battery                     |
| 🖐️ **Gestures**              | Swipe left/right to skip tracks, scroll up/down to change volume                                         |
| 🎭 **Themes**                | Now Bar (blurred album art), Classic (pure black), Neon (glowing outline), Glass (frosted)               |
| 〰️ **Wave styles**           | Flow, Bars, Line, Dots or Circle                                                                         |
| 🎶 **Spotify + Apple Music** | Follows whichever is playing, or the one you pick                                                        |
| ⚙️ **Menu bar app**          | Settings, Quit and Launch at login from the menu bar icon                                                |

Works on Macs **with or without a notch**.

## 📋 Requirements

- macOS **14.2** or later
- Spotify and/or Apple Music
- Swift 6 toolchain (Xcode, or just the Command Line Tools: `xcode-select --install`)

## 🚀 Installation

```sh
git clone https://github.com/<your-username>/notchbeat.git
cd notchbeat
./scripts/build-app.sh
open build/NotchBeat.app
```

To keep it around, drag `build/NotchBeat.app` into your `/Applications` folder.

### Permissions

macOS asks for these once:

| Permission                       | Why                                                                      |
| -------------------------------- | ------------------------------------------------------------------------ |
| **Automation** (Spotify / Music) | Read the current song and send play, skip, seek and volume commands      |
| **System Audio Recording**       | Let the waves react to the music. Audio is analyzed live and never saved |

You can change them later in **System Settings → Privacy & Security**.

## 🎮 Usage

| State                       | What you see                          |
| --------------------------- | ------------------------------------- |
| **Idle**                    | Hidden behind the notch               |
| **Playing**                 | Cover on the left, waves on the right |
| **Hover**                   | Expands into the full player          |
| **Click the lyric line**    | Opens full karaoke lyrics             |
| **Swipe ← / →** (expanded)  | Previous / next track                 |
| **Scroll ↑ / ↓** (expanded) | Volume up / down                      |

Open **Settings** from the menu bar icon to pick a theme, wave style, music app, lyrics and launch at login.

## 🎨 Customization

Every size, color, corner radius, animation, wave and gesture setting lives in one file:
[`Sources/NotchBeat/Theme/Theme.swift`](Sources/NotchBeat/Theme/Theme.swift).

## 🏗️ Project structure

```
Sources/NotchBeat/
├── App/       Entry point, wiring, menu bar icon
├── Island/    Notch window, hover and gestures, and the island views
├── Music/     Music app connections (Spotify, Apple Music) and the now-playing state
├── Audio/     Audio capture and frequency analysis for reactive waves
├── Waves/     Wave styles and drawing
├── Lyrics/    Synced lyrics from lrclib.net
├── System/    Volume, audio device and battery monitors for system pop-ups
├── Settings/  User preferences and the Settings window
└── Theme/     All visual settings, the themes, and album-art color extraction
Resources/     Info.plist for the .app bundle
scripts/       Build script
```

Data flows one way:

```
Music apps  →  NowPlaying  →  IslandModel  →  Views
```

### Adding another music app

Create a type that conforms to `MusicPlayer` (see [`SpotifyPlayer.swift`](Sources/NotchBeat/Music/SpotifyPlayer.swift) for an example) and add it to `NowPlaying.players`.

## 🛠️ Development

```sh
swift build      # debug build
swift run        # build and run
```

> **Note:** with `swift run`, permissions are credited to your terminal app, and Launch at login only works from the `.app` bundle. Use `./scripts/build-app.sh` to test the real app.

## 🤝 Contributing

Contributions, bug reports and feature ideas are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) to get started.

1. Fork the repo
2. Create a branch: `git checkout -b feature/my-feature`
3. Commit your changes: `git commit -m "Add my feature"`
4. Push the branch: `git push origin feature/my-feature`
5. Open a Pull Request

## 🔒 Privacy

- Lyrics lookups send the song title, artist, album and duration to [lrclib.net](https://lrclib.net). Turn lyrics off in Settings to disable this.
- Spotify artwork is downloaded from Spotify's image servers.
- Nothing else leaves your Mac. Audio is analyzed live and never recorded.

## 🙏 Acknowledgements

- [LRCLIB](https://lrclib.net) for free synced lyrics

## 📄 License

Released under the [MIT License](LICENSE).

<div align="center">

If you like NotchBeat, consider giving it a ⭐

</div>
