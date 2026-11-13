import SwiftUI

/// A short system pop-up: volume, audio device or charging.
struct ActivityView: View {
    let activity: SystemActivity
    let model: IslandModel

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: activity.symbol)
                .font(.system(size: 16, weight: .medium))
                .foregroundStyle(.white)
                .frame(width: 26)
                .contentTransition(.symbolEffect(.replace))

            Text(activity.title)
                .font(.system(size: 12, weight: .semibold))
                .foregroundStyle(.white)
                .lineLimit(1)

            Spacer(minLength: 8)

            if let level = activity.level {
                GeometryReader { geometry in
                    ZStack(alignment: .leading) {
                        Capsule().fill(.white.opacity(0.2))
                        Capsule().fill(.white).frame(width: geometry.size.width * level)
                    }
                }
                .frame(width: 90, height: 5)
                .animation(.easeOut(duration: 0.15), value: level)
            }
        }
        .padding(.top, model.notchSize.height + 6)
        .padding(.horizontal, 24)
        .frame(width: Theme.Island.peekWidth, height: model.size(for: .activity).height, alignment: .top)
    }
}
