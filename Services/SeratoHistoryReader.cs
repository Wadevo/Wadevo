namespace Wadevo.Services;

using System.Text;

// Reads Serato's local History session files directly, instead of scraping the
// "Serato Live Playlists" web page (see SeratoService). This is the more reliable
// method: no internet connection required, no need for the user to enable Live
// Playlists on serato.com, and truer timing since it reflects local session data
// rather than whatever Serato has synced up to their site.
//
// Serato's session/crate files use an unofficial, reverse-engineered binary "TLV"
// chunk format (tag/length/value), the same structure used across their .crate
// files. There is no public spec for this - the tag names below match what the
// wider DJ-tooling community (sslscrobbler, serato-connect, etc.) has documented
// through reverse engineering. Because of that, this should be treated as a first
// pass: verify against real session files and adjust tag names if a given Serato
// version has drifted.
public class SeratoHistoryReader
{
    // Container tag for a single track entry within a session file.
    private const string TrackEntryTag = "otrk";

    // Field tags nested inside an "otrk" container.
    private const string ArtistTag = "tart";
    private const string TitleTag = "tsng";

    public string? SessionsFolder { get; }

    public SeratoHistoryReader(string? sessionsFolder = null)
    {
        SessionsFolder = sessionsFolder ?? GetDefaultSessionsFolder();
    }

    private static string GetDefaultSessionsFolder()
    {
        string musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        return Path.Combine(musicFolder, "_Serato_", "History", "Sessions");
    }

    public bool IsSeratoDataAvailable()
    {
        return SessionsFolder is not null && Directory.Exists(SessionsFolder);
    }

    // Finds the most recently modified session file - that's the active/current session
    // while Serato is open and being played on.
    public string? GetCurrentSessionFilePath()
    {
        if (!IsSeratoDataAvailable())
        {
            return null;
        }

        return Directory.EnumerateFiles(SessionsFolder!, "*.session")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    // Returns the most recently played track (artist, title) from the current session,
    // or null if no session/track could be read.
    public (string Artist, string Title)? GetCurrentTrack()
    {
        string? sessionFile = GetCurrentSessionFilePath();

        if (sessionFile is null)
        {
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(sessionFile);
            List<(string Artist, string Title)> tracks = ParseTracks(bytes);

            // The last track entry in the file is the most recently played/loaded one.
            return tracks.Count > 0 ? tracks[^1] : null;
        }
        catch (IOException)
        {
            // Serato may have the file open/locked while writing to it - treat as
            // "no update this poll" rather than a hard failure.
            return null;
        }
    }

    // "Artist - Title" combined form, matching the same shape SeratoService (URL-based) and
    // VirtualDjNowPlayingReader both already return - lets MainForm treat all three DJ
    // sources through one uniform interface rather than three different shapes.
    public Task<string?> GetCurrentSongAsync()
    {
        (string Artist, string Title)? track = GetCurrentTrack();

        if (track is null || (string.IsNullOrWhiteSpace(track.Value.Artist) && string.IsNullOrWhiteSpace(track.Value.Title)))
        {
            return Task.FromResult<string?>(null);
        }

        string combined = string.IsNullOrWhiteSpace(track.Value.Artist)
            ? track.Value.Title
            : $"{track.Value.Artist} - {track.Value.Title}";

        return Task.FromResult<string?>(combined);
    }

    private static List<(string Artist, string Title)> ParseTracks(byte[] data)
    {
        List<(string, string)> results = new();
        int offset = 0;

        while (offset + 8 <= data.Length)
        {
            string tag = Encoding.ASCII.GetString(data, offset, 4);
            int length = ReadInt32BigEndian(data, offset + 4);
            int valueStart = offset + 8;

            if (length < 0 || valueStart + length > data.Length)
            {
                // Malformed/unexpected chunk - stop rather than read out of bounds.
                break;
            }

            if (tag == TrackEntryTag)
            {
                (string Artist, string Title)? track = ParseTrackEntry(data, valueStart, length);

                if (track is not null && (!string.IsNullOrWhiteSpace(track.Value.Artist) || !string.IsNullOrWhiteSpace(track.Value.Title)))
                {
                    results.Add(track.Value);
                }
            }

            offset = valueStart + length;
        }

        return results;
    }

    private static (string Artist, string Title)? ParseTrackEntry(byte[] data, int start, int length)
    {
        string artist = "";
        string title = "";

        int offset = start;
        int end = start + length;

        while (offset + 8 <= end)
        {
            string fieldTag = Encoding.ASCII.GetString(data, offset, 4);
            int fieldLength = ReadInt32BigEndian(data, offset + 4);
            int fieldValueStart = offset + 8;

            if (fieldLength < 0 || fieldValueStart + fieldLength > end)
            {
                break;
            }

            switch (fieldTag)
            {
                case ArtistTag:
                    artist = ReadUtf16BeString(data, fieldValueStart, fieldLength);
                    break;

                case TitleTag:
                    title = ReadUtf16BeString(data, fieldValueStart, fieldLength);
                    break;
            }

            offset = fieldValueStart + fieldLength;
        }

        return (artist, title);
    }

    private static int ReadInt32BigEndian(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    // Serato's text fields are UTF-16 big-endian, unlike .NET's native UTF-16LE strings.
    private static string ReadUtf16BeString(byte[] data, int offset, int length)
    {
        byte[] swapped = new byte[length];

        for (int i = 0; i + 1 < length; i += 2)
        {
            swapped[i] = data[offset + i + 1];
            swapped[i + 1] = data[offset + i];
        }

        return Encoding.Unicode.GetString(swapped).TrimEnd('\0').Trim();
    }
}
