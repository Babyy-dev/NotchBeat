import SwiftUI

struct SettingsView: View {
    @Bindable var preferences: Preferences

    var body: some View {
        Form {
            Section("Music") {
                Picker("Follow", selection: $preferences.source) {
                    ForEach(MusicSource.allCases) { Text($0.title).tag($0) }
                }
            }

            Section("Look") {
                Picker("Theme", selection: $preferences.theme) {
                    ForEach(IslandTheme.allCases) { Text($0.title).tag($0) }
                }
                .pickerStyle(.segmented)
                Toggle("Spinning album cover", isOn: $preferences.spinArtwork)
            }

            Section("Waves") {
                Picker("Style", selection: $preferences.waveStyle) {
                    ForEach(WaveStyle.allCases) { Text($0.title).tag($0) }
                }
                .pickerStyle(.segmented)
                Toggle("Move with the music", isOn: $preferences.reactiveWaves)
                Text("Listens to the music app's audio (asks for permission once). Off uses a smooth animation instead.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Section("Lyrics") {
                Toggle("Show synced lyrics", isOn: $preferences.showLyrics)
                Text("Click the lyric line in the player for full karaoke lyrics. Lyrics come from lrclib.net; the song title, artist and album are sent to look them up.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Section("System") {
                Toggle("Show volume, audio device and charging", isOn: $preferences.showSystemActivity)
            }

            Section("General") {
                Toggle("Launch at login", isOn: Binding(
                    get: { preferences.launchAtLogin },
                    set: { preferences.setLaunchAtLogin($0) }
                ))
            }
        }
        .formStyle(.grouped)
        .frame(width: 460)
        .fixedSize()
    }
}
