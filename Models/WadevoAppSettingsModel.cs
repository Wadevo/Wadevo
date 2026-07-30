namespace Wadevo.Models;

public sealed class WadevoAppSettingsModel
{
    public string SeratoPlaylistUrl { get; set; } = "https://serato.com/playlists/id:8891596/live";

    // "Serato" or "VirtualDJ" - which local DJ software Wadevo reads now-playing data from.
    public string DjSoftware { get; set; } = "Serato";

    // "LivePlaylistUrl" (the original, URL-scraping method) or "LocalHistoryFile" (reads
    // Serato's own local session files directly - no internet needed, doesn't require
    // Live Playlists enabled). Only relevant when DjSoftware is "Serato".
    public string SeratoReadMethod { get; set; } = "LivePlaylistUrl";

    public bool BlazeCommandsEnabled { get; set; } = true;

    public bool UseBotIdentityForCommands { get; set; } = false;

    public bool UseTwitchBotIdentityForCommands { get; set; } = false;
}
