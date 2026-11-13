# Contributing to NotchBeat

Thanks for your interest in improving NotchBeat! 🎵

## Reporting bugs

Open an issue and include:

- Your macOS version and Mac model (notch or no notch)
- Which music app you use (Spotify or Apple Music)
- What you expected and what happened
- A screenshot or screen recording if possible

## Suggesting features

Open an issue describing the idea and why it would be useful.

## Making changes

1. Fork the repo and create a branch from `main`.
2. Build and test with `./scripts/build-app.sh` and `open build/NotchBeat.app`.
3. Keep pull requests focused on one change.
4. Describe what you changed and how you tested it in the PR.

## Code guidelines

- The project builds with only the Command Line Tools, so avoid SwiftUI macros that need Xcode's plugins (such as `@State`). Keep view state in the `@Observable` models instead.
- Visual settings (sizes, colors, animations) belong in `Sources/NotchBeat/Theme/Theme.swift`.
- Data flows one way: music apps → `NowPlaying` → `IslandModel` → views.
- To support a new music app, conform to `MusicPlayer` and register it in `NowPlaying.players`.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
