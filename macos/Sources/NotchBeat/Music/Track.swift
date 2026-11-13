import Foundation

struct Track: Equatable, Sendable {
    let id: String
    let title: String
    let artist: String
    let album: String
    /// Set for Spotify; Apple Music artwork is read from the app instead.
    let artworkURL: URL?
    let duration: TimeInterval
}

/// A snapshot of the player at the moment it was read.
struct PlayerState {
    let track: Track
    let isPlaying: Bool
    let position: TimeInterval
    /// 0...100
    let volume: Int
}
