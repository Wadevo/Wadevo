namespace Wadevo.Services;

using Wadevo.Models;
using Wadevo.Services.Blaze;
using Wadevo.Services.Obs;
using Wadevo.Services.Twitch;

public static class ConnectionsHubService
{
    public static List<ConnectionInfoModel> GetConnections()
    {
        List<ConnectionInfoModel> connections = new();

        connections.AddRange(GetStreamingConnections());
        connections.AddRange(GetMusicConnections());
        connections.AddRange(GetSoftwareConnections());

        return connections;
    }

    private static IEnumerable<ConnectionInfoModel> GetStreamingConnections()
    {
        BlazeAuthenticationService blaze = BlazeAuthenticationService.Shared;

        yield return blaze.IsAuthenticated
            ? new ConnectionInfoModel
            {
                Name = "Blaze",
                Glyph = "🔥",
                Category = ConnectionCategory.Streaming,
                State = ConnectionState.Connected,
                StatusText = "Connected",
                Description = "Chat, events, raids, gifts, and alerts.",
                CanOpen = true
            }
            : new ConnectionInfoModel
            {
                Name = "Blaze",
                Glyph = "🔥",
                Category = ConnectionCategory.Streaming,
                State = blaze.IsConfigured ? ConnectionState.NotConnected : ConnectionState.Warning,
                StatusText = blaze.IsConfigured ? "Not connected" : "Not configured",
                Description = "Chat, events, raids, gifts, and alerts.",
                CanOpen = true
            };

        TwitchAuthenticationService twitch = TwitchAuthenticationService.Shared;
        TwitchLiveEventService twitchEvents = TwitchLiveEventService.Shared;

        yield return twitch.IsAuthenticated
            ? new ConnectionInfoModel
            {
                Name = "Twitch",
                Glyph = "🟣",
                Category = ConnectionCategory.Streaming,
                State = twitchEvents.IsListening ? ConnectionState.Connected : ConnectionState.Warning,
                StatusText = twitchEvents.IsListening ? $"Connected — {twitch.Connection.Username}" : "Authenticated, events stopped",
                Description = "Chat, follows, raids, and subs.",
                CanOpen = true
            }
            : new ConnectionInfoModel
            {
                Name = "Twitch",
                Glyph = "🟣",
                Category = ConnectionCategory.Streaming,
                State = twitch.IsConfigured ? ConnectionState.NotConnected : ConnectionState.Warning,
                StatusText = twitch.IsConfigured ? "Not connected" : "Not configured",
                Description = "Chat, follows, raids, and subs.",
                CanOpen = true
            };

        yield return ComingSoon("YouTube", "▶️", ConnectionCategory.Streaming, "Live chat and channel events.");
        yield return ComingSoon("Kick", "🟢", ConnectionCategory.Streaming, "Chat and channel events.");
        yield return ComingSoon("TikTok", "🎵", ConnectionCategory.Streaming, "Live chat and gifts.");
    }

    private static IEnumerable<ConnectionInfoModel> GetMusicConnections()
    {
        yield return WadevoLiveStatus.IsSeratoConnected
            ? new ConnectionInfoModel
            {
                Name = "Serato",
                Glyph = "🎧",
                Category = ConnectionCategory.Music,
                State = ConnectionState.Connected,
                StatusText = $"Connected — {WadevoLiveStatus.CurrentSong}",
                Description = "Reads your current playlist for Song ID overlays.",
                CanOpen = true
            }
            : new ConnectionInfoModel
            {
                Name = "Serato",
                Glyph = "🎧",
                Category = ConnectionCategory.Music,
                State = ConnectionState.Warning,
                StatusText = "Waiting for a playlist",
                Description = "Reads your current playlist for Song ID overlays.",
                CanOpen = true
            };

        yield return ComingSoon("TIDAL", "🌊", ConnectionCategory.Music, "Track metadata and playback.");
        yield return ComingSoon("Spotify", "🟢", ConnectionCategory.Music, "Track metadata and playback.");
        yield return WadevoLiveStatus.IsVirtualDjConnected
            ? new ConnectionInfoModel
            {
                Name = "VirtualDJ",
                Glyph = "🎚",
                Category = ConnectionCategory.Music,
                State = ConnectionState.Connected,
                StatusText = $"Connected — {WadevoLiveStatus.CurrentSong}",
                Description = "Reads your local tracklist.txt for Song ID overlays.",
                CanOpen = true
            }
            : new ConnectionInfoModel
            {
                Name = "VirtualDJ",
                Glyph = "🎚",
                Category = ConnectionCategory.Music,
                State = ConnectionState.NotConnected,
                StatusText = "Waiting for a track",
                Description = "Reads your local tracklist.txt for Song ID overlays.",
                CanOpen = true
            };
        yield return ComingSoon("rekordbox", "🎛", ConnectionCategory.Music, "Now-playing data for rekordbox.");
    }

    private static IEnumerable<ConnectionInfoModel> GetSoftwareConnections()
    {
        yield return WadevoLiveStatus.IsOverlayEngineRunning
            ? new ConnectionInfoModel
            {
                Name = "Overlay Engine",
                Glyph = "🚀",
                Category = ConnectionCategory.Software,
                State = ConnectionState.Connected,
                StatusText = "Running — localhost:5050",
                Description = "Serves your overlays to OBS browser sources.",
                CanOpen = true
            }
            : new ConnectionInfoModel
            {
                Name = "Overlay Engine",
                Glyph = "🚀",
                Category = ConnectionCategory.Software,
                State = ConnectionState.NotConnected,
                StatusText = "Not running",
                Description = "Serves your overlays to OBS browser sources.",
                CanOpen = true
            };

        ObsConnectionService obs = ObsConnectionService.Shared;

        yield return obs.IsConnected
            ? new ConnectionInfoModel
            {
                Name = "OBS Studio",
                Glyph = "🖥",
                Category = ConnectionCategory.Software,
                State = obs.IsStreaming ? ConnectionState.Connected : ConnectionState.Warning,
                StatusText = obs.IsStreaming ? $"Streaming — {obs.CurrentSceneName}" : $"Connected — {obs.CurrentSceneName}",
                Description = "Scene and source control via OBS WebSocket.",
                CanOpen = true
            }
            : new ConnectionInfoModel
            {
                Name = "OBS Studio",
                Glyph = "🖥",
                Category = ConnectionCategory.Software,
                State = ConnectionState.NotConnected,
                StatusText = "Not connected",
                Description = "Scene and source control via OBS WebSocket.",
                CanOpen = true
            };
    }

    private static ConnectionInfoModel ComingSoon(string name, string glyph, ConnectionCategory category, string description)
    {
        return new ConnectionInfoModel
        {
            Name = name,
            Glyph = glyph,
            Category = category,
            State = ConnectionState.ComingSoon,
            StatusText = "Coming soon",
            Description = description,
            CanOpen = false
        };
    }
}
