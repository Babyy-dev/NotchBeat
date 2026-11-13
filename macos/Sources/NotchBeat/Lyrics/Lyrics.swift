import Foundation

/// Time-synced lyrics parsed from the LRC format (`[mm:ss.xx] line`).
struct Lyrics: Sendable {
    struct Line: Sendable {
        let time: TimeInterval
        let text: String
    }

    let lines: [Line]

    init?(lrc: String) {
        let timestamp = /\[(\d+):(\d+(?:\.\d+)?)\]/
        var lines: [Line] = []

        for rawLine in lrc.split(whereSeparator: \.isNewline) {
            // A line can carry several timestamps when it repeats, e.g. "[00:12.00][01:30.00] Chorus".
            let stamps = rawLine.matches(of: timestamp)
            guard let lastStamp = stamps.last else { continue }
            let text = rawLine[lastStamp.range.upperBound...].trimmingCharacters(in: .whitespaces)

            for stamp in stamps {
                guard let minutes = Double(stamp.1), let seconds = Double(stamp.2) else { continue }
                lines.append(Line(time: minutes * 60 + seconds, text: text))
            }
        }

        guard !lines.isEmpty else { return nil }
        self.lines = lines.sorted { $0.time < $1.time }
    }

    /// The line being sung at `position`, or `nil` before the first line.
    func line(at position: TimeInterval) -> String? {
        progress(at: position).map { lines[$0.index].text }
    }

    /// Index of the line being sung and how far through it we are (0...1),
    /// estimated from the time until the next line.
    func progress(at position: TimeInterval) -> (index: Int, fraction: Double)? {
        guard let index = lines.lastIndex(where: { $0.time <= position }) else { return nil }
        let start = lines[index].time
        let end = index + 1 < lines.count ? lines[index + 1].time : start + 4
        let fraction = (position - start) / max(end - start, 0.1)
        return (index, min(max(fraction, 0), 1))
    }
}
