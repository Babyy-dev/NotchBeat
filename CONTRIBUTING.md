# Contributing to NotchBeat

Thanks for your interest in improving NotchBeat! 🎵

## Reporting bugs

Open an issue and include:

- Your platform: macOS version and Mac model (notch or no notch), or Windows version
- Which music app you use (Spotify, Apple Music, a browser…)
- What you expected and what happened
- A screenshot or screen recording if possible

## Suggesting features

Open an issue describing the idea and why it would be useful.

## Making changes

1. Fork the repo and create a branch from `main`.
2. Build and test:
   - macOS: `cd macos && ./scripts/build-app.sh && open build/NotchBeat.app`
   - Windows: `cd windows && .\build.ps1 && .\publish\NotchBeat.exe`
3. Keep pull requests focused on one change.
4. Describe what you changed and how you tested it in the PR.

## Code guidelines

The macOS (`macos/`) and Windows (`windows/`) apps share the same design, file layout and names. When you add a feature to one, keep it shaped so it can be ported to the other, and mention in the PR if it's single-platform.

**Both**

- Data flows one way: music apps → `NowPlaying` → `IslandModel` → views.
- Visual settings (sizes, colors, animations) belong in `Theme.swift` / `Theme.cs`, not in views.

**macOS**

- The project builds with only the Command Line Tools, so avoid SwiftUI macros that need Xcode's plugins (such as `@State`). Keep view state in the `@Observable` models instead.
- To support a new music app, conform to `MusicPlayer` and register it in `NowPlaying.players`.

**Windows**

- The project uses WPF, plus Windows Forms only for the tray icon and screen list. Implicit usings are off because the two share type names, so add the usings each file needs.
- The project also builds on macOS and Linux (`dotnet build windows/NotchBeat`) for quick checks, but it only runs on Windows.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
