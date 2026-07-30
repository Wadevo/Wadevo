namespace Wadevo.Services;

using System.Text.RegularExpressions;

// VirtualDJ writes its play history to a local text file - no API, no OAuth, just a file
// on disk, updated a short while after each track starts (configurable in VirtualDJ via
// the "historyDelay" setting, default ~45 seconds - lowering it makes Wadevo's "now
// playing" feel more responsive). The exact line format is also user-configurable in
// VirtualDJ (the "tracklistFormat" setting), so this parses defensively rather than
// assuming one exact layout.
public sealed class VirtualDjNowPlayingReader
{
    // Matches VirtualDJ's common default format, e.g. "22:45 : Artist - Title" - strips
    // a leading timestamp if present, since that's the most common source of parse misses.
    private static readonly Regex LeadingTimestampPattern = new(@"^\s*\d{1,2}:\d{2}(:\d{2})?\s*:\s*", RegexOptions.Compiled);

    public string? FilePath { get; }

    public VirtualDjNowPlayingReader(string? filePath = null)
    {
        FilePath = filePath ?? GetDefaultFilePath();
    }

    private static string GetDefaultFilePath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "VirtualDJ", "History", "tracklist.txt");
    }

    public bool IsFileAvailable()
    {
        return FilePath is not null && File.Exists(FilePath);
    }

    // Returns "Artist - Title" (matching the same combined format SeratoService already
    // returns, so MainForm's existing SplitSong() parsing works unchanged for either
    // source), or null if nothing could be read yet.
    public async Task<string?> GetCurrentSongAsync()
    {
        if (!IsFileAvailable())
        {
            return null;
        }

        try
        {
            // VirtualDJ may have the file open/locked while writing to it - FileShare.ReadWrite
            // lets Wadevo read alongside that instead of failing every time it happens to poll
            // at the wrong moment.
            using FileStream stream = new(FilePath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);

            string? lastNonEmptyLine = null;
            string? line;

            while ((line = await reader.ReadLineAsync()) is not null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lastNonEmptyLine = line;
                }
            }

            return lastNonEmptyLine is null ? null : ParseLine(lastNonEmptyLine);
        }
        catch (IOException)
        {
            // File genuinely locked at this exact moment - treat as "no update this poll"
            // rather than a hard failure, matching how Serato's reader handles the same thing.
            return null;
        }
    }

    private static string ParseLine(string line)
    {
        string withoutTimestamp = LeadingTimestampPattern.Replace(line, "");
        return withoutTimestamp.Trim();
    }
}
