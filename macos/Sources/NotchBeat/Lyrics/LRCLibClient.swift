import Foundation

/// Looks up synced lyrics on lrclib.net, a free and open lyrics database. No API key needed.
enum LRCLibClient {
    private struct Result: Decodable {
        let syncedLyrics: String?
    }

    static func lyrics(for track: Track) async -> Lyrics? {
        // Exact match first, then a looser search when the album or duration differ.
        if let result = await fetch(Result.self, path: "get", query: [
            "track_name": track.title,
            "artist_name": track.artist,
            "album_name": track.album,
            "duration": String(Int(track.duration.rounded())),
        ]), let lyrics = result.syncedLyrics.flatMap(Lyrics.init(lrc:)) {
            return lyrics
        }

        let results = await fetch([Result].self, path: "search", query: [
            "track_name": track.title,
            "artist_name": track.artist,
        ])
        return results?.lazy.compactMap { $0.syncedLyrics.flatMap(Lyrics.init(lrc:)) }.first
    }

    private static func fetch<T: Decodable>(_ type: T.Type, path: String, query: [String: String]) async -> T? {
        guard var components = URLComponents(string: "https://lrclib.net/api/\(path)") else { return nil }
        components.queryItems = query.map { URLQueryItem(name: $0.key, value: $0.value) }
        guard let url = components.url else { return nil }

        var request = URLRequest(url: url)
        // LRCLIB asks clients to identify themselves.
        request.setValue("NotchBeat (open-source macOS notch player)", forHTTPHeaderField: "User-Agent")

        guard
            let (data, response) = try? await URLSession.shared.data(for: request),
            (response as? HTTPURLResponse)?.statusCode == 200
        else { return nil }
        return try? JSONDecoder().decode(T.self, from: data)
    }
}
