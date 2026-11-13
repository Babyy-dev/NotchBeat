/// A short system event the island pops up for, like a volume change or AirPods connecting.
struct SystemActivity: Equatable {
    enum Kind {
        case volume, outputDevice, power
    }

    let kind: Kind
    /// SF Symbol name.
    let symbol: String
    let title: String
    /// Optional 0...1 value shown as a bar (volume, battery).
    var level: Double?
}
