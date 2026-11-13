/// The look of the open island. Picked in Settings.
enum IslandTheme: String, CaseIterable, Identifiable {
    /// Blurred album art behind the player, waves along the bottom.
    case nowBar
    /// Pure black and minimal, no bottom waves.
    case classic
    /// Black with a glowing outline in the album's color.
    case neon
    /// Frosted, see-through glass.
    case glass

    var id: Self { self }

    var title: String {
        switch self {
        case .nowBar: "Now Bar"
        case .classic: "Classic"
        case .neon: "Neon"
        case .glass: "Glass"
        }
    }

    var showsBottomWaves: Bool {
        self != .classic
    }
}
