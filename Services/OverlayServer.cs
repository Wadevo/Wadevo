namespace Wadevo.Services;

using System.Net;
using System.Text;
using System.Text.Json;
using Wadevo.Controls;
using Wadevo.Models;
using Wadevo.Services.Blaze;
using Wadevo.Services.Platforms;

public class OverlayServer
{
    private static readonly Dictionary<string, DateTime> RouteLastSeenUtc = new();
    private static readonly List<OverlayEvent> OverlayEvents = new();
    private static readonly object RouteStatusLock = new();
    private static readonly object EventLock = new();

    // Unlike OverlayEvents (a one-shot queue consumed and cleared as alerts fire), this is
    // a persistent rolling log - the Combined Chat overlay stays populated with recent
    // history the whole stream, not just messages that arrived since the browser last polled.
    private static readonly List<ChatOverlayMessage> ChatMessages = new();
    private static readonly object ChatLock = new();
    private const int MaxChatMessages = 100;

    private static int _nextEventId;
    private static int _nextChatMessageId;

    private readonly HttpListener _listener = new();
    private readonly Func<string> _getCurrentSong;
    private readonly Func<MusicMetadataModel?> _getCurrentMetadata;

    private bool _isRunning;

    public static string BaseUrl => "http://localhost:5050/";
    public static string HomeUrl => BaseUrl;
    public static string NowPlayingUrl => $"{BaseUrl}nowplaying";
    public static string CommandsOverlayUrl => $"{BaseUrl}commands";
    public static string AlertsOverlayUrl => $"{BaseUrl}alerts";
    public static string GifsOverlayUrl => $"{BaseUrl}gifs";
    public static string VotesOverlayUrl => $"{BaseUrl}votes";
    public static string ChatOverlayUrl => $"{BaseUrl}chat";
    public static string DebugUrl => $"{BaseUrl}debug";

    public static event Action? RouteStatusChanged;

    public OverlayServer(string url, Func<string> getCurrentSong, Func<MusicMetadataModel?>? getCurrentMetadata = null)
    {
        _getCurrentSong = getCurrentSong;
        _getCurrentMetadata = getCurrentMetadata ?? (() => null);

        if (!url.EndsWith('/'))
        {
            url += "/";
        }

        _listener.Prefixes.Add(url);
    }

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _listener.Start();

        Task.Run(ListenLoop);
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        try
        {
            _listener.Stop();
        }
        catch
        {
        }
    }

    public static bool IsRouteActive(string route)
    {
        route = NormalizeRoute(route);

        lock (RouteStatusLock)
        {
            return RouteLastSeenUtc.TryGetValue(route, out DateTime lastSeenUtc)
                && DateTime.UtcNow - lastSeenUtc <= TimeSpan.FromSeconds(6);
        }
    }

    public static IReadOnlyDictionary<string, bool> GetRouteStatuses()
    {
        string[] routes =
        [
            "/nowplaying",
            "/commands",
            "/alerts",
            "/gifs",
            "/votes",
            "/chat",
            "/debug"
        ];

        Dictionary<string, bool> statuses = new();

        foreach (string route in routes)
        {
            statuses[route] = IsRouteActive(route);
        }

        return statuses;
    }

    public static void TriggerTestCommand()
    {
        OverlaySettingsModel settings = new OverlayDesignerService().GetPreviewSettings();

        TriggerCommand(
            settings.CommandTitle,
            settings.CommandMessage);
    }

    public static void TriggerAlert(string title, string message)
    {
        TriggerAlert(title, message, "");
    }

    public static void TriggerAlert(string title, string message, string gifPath)
    {
        AddOverlayEvent("alert", title, message, gifPath, 0);
    }

    public static void TriggerAlert(AlertProfileModel profile, BlazeEventCommandContext context)
    {
        if (string.IsNullOrWhiteSpace(profile.LinkedOverlayPresetId))
        {
            // No appearance designed yet for this alert - a plain fallback message still
            // gives visible signal that it fired, rather than silently doing nothing.
            string fallbackMessage = AlertProfileTextFormatter.Format(
                string.IsNullOrWhiteSpace(profile.Name) ? "Alert triggered" : profile.Name,
                context);

            AddOverlayEvent("alert", "Wadevo Alert", fallbackMessage, "", profile.DurationMilliseconds);
            return;
        }

        WadevoDesignerPresetModel? preset = new WadevoDesignerPresetStore()
            .LoadAll()
            .FirstOrDefault(p => p.Id == profile.LinkedOverlayPresetId);

        if (preset is null)
        {
            AddOverlayEvent(
                "alert",
                "Wadevo Alert",
                "This alert's design was deleted - open it in Overlay Designer to fix.",
                "",
                profile.DurationMilliseconds);
            return;
        }

        List<object> elements = new();

        foreach (WadevoDesignerElementState element in preset.Elements.Where(e => e.IsVisible))
        {
            if (element.Kind == WadevoDesignerElementKind.Text)
            {
                // Token substitution happens here, server-side, same as the alert system
                // already did before this conversion - safe because the *result* is still
                // treated as plain text on the client (via the same safe emote-substitution
                // DOM construction used elsewhere), never injected as raw HTML.
                string substituted = AlertProfileTextFormatter.Format(element.Text, context);
                System.Drawing.Color color = System.Drawing.Color.FromArgb(element.FontColorArgb);

                elements.Add(new
                {
                    Kind = "Text",
                    element.X,
                    element.Y,
                    element.Width,
                    element.Height,
                    Text = substituted,
                    element.FontFamily,
                    element.FontSize,
                    element.FontBold,
                    TextColor = $"rgb({color.R},{color.G},{color.B})"
                });
            }
            else if (element.Kind == WadevoDesignerElementKind.Image && !string.IsNullOrWhiteSpace(element.ImagePath))
            {
                string extension = Path.GetExtension(element.ImagePath).ToLowerInvariant();
                bool isVideo = extension is ".mp4" or ".webm" or ".mov";

                elements.Add(new
                {
                    Kind = "Image",
                    element.X,
                    element.Y,
                    element.Width,
                    element.Height,
                    ImageUrl = "/media?path=" + Uri.EscapeDataString(element.ImagePath),
                    IsVideo = isVideo
                });
            }
        }

        int canvasWidth = Math.Max(320, elements.Count == 0 ? 900 : Math.Max(900, preset.Elements.Where(e => e.IsVisible).Max(e => e.X + e.Width) + 40));
        int canvasHeight = Math.Max(160, elements.Count == 0 ? 400 : Math.Max(400, preset.Elements.Where(e => e.IsVisible).Max(e => e.Y + e.Height) + 40));

        object? background = null;

        if (!string.IsNullOrWhiteSpace(preset.BackgroundImagePath) && File.Exists(preset.BackgroundImagePath))
        {
            try
            {
                int imageWidth;
                int imageHeight;

                using (System.Drawing.Image image = System.Drawing.Image.FromFile(preset.BackgroundImagePath))
                {
                    imageWidth = image.Width;
                    imageHeight = image.Height;
                }

                (int x, int y, int width, int height) = BackgroundRectCalculator.ComputeDestRect(
                    0, 0, canvasWidth, canvasHeight, imageWidth, imageHeight, preset.BackgroundScaleMode,
                    preset.BackgroundWidthPercent, preset.BackgroundHeightPercent,
                    preset.BackgroundOffsetX, preset.BackgroundOffsetY);

                background = new
                {
                    ImageUrl = "/media?path=" + Uri.EscapeDataString(preset.BackgroundImagePath),
                    X = x,
                    Y = y,
                    Width = width,
                    Height = height,
                    preset.BackgroundRoundedCorners,
                    preset.BackgroundOpacityPercent
                };
            }
            catch
            {
                // Falls through with background left null - a corrupt/unreadable image
                // file shouldn't prevent the rest of the alert (text, other elements) from
                // still showing.
            }
        }

        string elementsJson = JsonSerializer.Serialize(new { background, elements });

        AddOverlayEvent(
            "alert",
            profile.Name,
            "",
            "",
            profile.DurationMilliseconds,
            elementsJson: elementsJson);
    }

    public static void TriggerCommand(string title, string message)
    {
        TriggerCommand(title, message, "");
    }

    public static void TriggerCommand(string title, string message, string gifPath)
    {
        TriggerCommand(title, message, gifPath, 0, 0);
    }

    public static void TriggerCommand(string title, string message, string gifPath, int width, int height)
    {
        AddOverlayEvent("command", title, message, gifPath, 0, width, height);
    }

    public static void TriggerGif(string gifPath, int durationMilliseconds, string title = "")
    {
        if (string.IsNullOrWhiteSpace(gifPath))
        {
            return;
        }

        if (durationMilliseconds <= 0)
        {
            durationMilliseconds = 5000;
        }

        AddOverlayEvent("gif", title, "", gifPath, durationMilliseconds);
    }

    // Feeds the Combined Chat overlay (/chat) - called once per incoming chat message from
    // any platform's event execution service. Uses the platform registry for glyph/color
    // so a newly added platform (Kick, TikTok) needs no changes here at all.
    public static void AddChatMessage(CommandSourcePlatform platform, string username, string message)
    {
        if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        ChatOverlaySettings chatSettings = new ChatOverlaySettingsStore().Load();

        bool platformIsVisible = platform switch
        {
            CommandSourcePlatform.Blaze => chatSettings.ShowBlaze,
            CommandSourcePlatform.Twitch => chatSettings.ShowTwitch,
            _ => true
        };

        if (!platformIsVisible)
        {
            return;
        }

        PlatformDescriptor descriptor = PlatformRegistry.Get(platform);
        string colorHex = ColorTranslator.ToHtml(descriptor.AccentColor);

        lock (ChatLock)
        {
            _nextChatMessageId++;

            ChatMessages.Add(new ChatOverlayMessage(
                _nextChatMessageId,
                descriptor.Name,
                descriptor.Glyph,
                colorHex,
                username,
                message,
                DateTime.UtcNow));

            if (ChatMessages.Count > MaxChatMessages)
            {
                ChatMessages.RemoveAt(0);
            }
        }
    }

    private static void AddOverlayEvent(
        string type,
        string title,
        string message,
        string gifPath,
        int durationMilliseconds,
        int width = 0,
        int height = 0,
        string accentHex = "",
        string backgroundHex = "",
        string textHex = "",
        string fontFamily = "",
        string animationName = "",
        string backgroundImagePath = "",
        string elementsJson = "")
    {
        lock (EventLock)
        {
            _nextEventId++;

            OverlayEvents.Add(new OverlayEvent(
                _nextEventId,
                type,
                title,
                message,
                gifPath,
                durationMilliseconds,
                width,
                height,
                DateTime.UtcNow,
                accentHex,
                backgroundHex,
                textHex,
                fontFamily,
                animationName,
                backgroundImagePath,
                elementsJson));

            if (OverlayEvents.Count > 50)
            {
                OverlayEvents.RemoveAt(0);
            }
        }
    }

    private async Task ListenLoop()
    {
        while (_isRunning)
        {
            try
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch
            {
                if (!_isRunning)
                {
                    break;
                }
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        string route = NormalizeRoute(context.Request.Url?.AbsolutePath ?? "/");

        MarkRouteSeen(route);

        try
        {
            bool isMediaResponse = false;

            string response = route switch
            {
                "/" => CreateHomePage(),
                "/nowplaying" => CreateNowPlayingOverlay(),
                "/overlay" => CreateSpecificOverlayPage(context),
                "/nowplaying-data" => CreateNowPlayingDataJson(),
                "/commands" => CreateCommandsOverlay(),
                "/alerts" => CreateAlertsOverlay(),
                "/gifs" => CreateGifsOverlay(),
                "/songqueue-data" => CreateSongQueueDataJson(),
                "/goal-data" => CreateGoalDataJson(context),
                "/votes" => CreateVotesOverlay(),
                "/votes-data" => CreateVotesDataJson(),
                "/chat" => CreateCombinedChatOverlay(),
                "/chat-data" => CreateChatDataJson(),
                "/debug" => CreateDebugPage(),
                "/events" => CreateEventsJson(context),
                "/media" => CreateMediaResponse(context, out isMediaResponse),
                "/ping" => CreatePingJson(context),
                _ => CreateNotFoundPage(route)
            };

            byte[] buffer = Encoding.UTF8.GetBytes(response);

            if (route is "/media" && isMediaResponse)
            {
                await WriteMediaResponse(context, response);
                return;
            }

            context.Response.ContentType = route is "/events" or "/ping" or "/nowplaying-data" or "/songqueue-data" or "/goal-data" or "/votes-data" or "/chat-data"
                ? "application/json; charset=utf-8"
                : "text/html; charset=utf-8";

            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = IsKnownRoute(route) ? 200 : 404;

            await context.Response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception ex)
        {
            WadevoLogger.Error($"Overlay request failed for route '{route}'", ex);

            try
            {
                context.Response.StatusCode = 500;
            }
            catch
            {
            }
        }
        finally
        {
            try
            {
                context.Response.OutputStream.Close();
            }
            catch
            {
            }

            RouteStatusChanged?.Invoke();
        }
    }

    private static void MarkRouteSeen(string route)
    {
        lock (RouteStatusLock)
        {
            RouteLastSeenUtc[route] = DateTime.UtcNow;
        }

        RouteStatusChanged?.Invoke();
    }

    private static bool IsKnownRoute(string route)
    {
        return route is "/" or "/nowplaying" or "/nowplaying-data" or "/overlay" or "/commands" or "/alerts" or "/gifs" or "/songqueue-data" or "/goal-data" or "/votes" or "/votes-data" or "/chat" or "/chat-data" or "/debug" or "/events" or "/media" or "/ping";
    }

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        route = route.Trim().ToLowerInvariant();

        if (!route.StartsWith('/'))
        {
            route = "/" + route;
        }

        if (route.Length > 1 && route.EndsWith('/'))
        {
            route = route[..^1];
        }

        return route;
    }

    private static string CreateEventsJson(HttpListenerContext context)
    {
        MarkClientRouteFromQuery(context);

        int afterId = 0;

        string? afterQuery = context.Request.QueryString["after"];

        if (!string.IsNullOrWhiteSpace(afterQuery))
        {
            int.TryParse(afterQuery, out afterId);
        }

        List<OverlayEvent> events;

        lock (EventLock)
        {
            events = OverlayEvents
                .Where(overlayEvent => overlayEvent.Id > afterId)
                .ToList();
        }

        return JsonSerializer.Serialize(events);
    }

    private static string CreateSongQueueDataJson()
    {
        // Visibility and how many to show are now controlled per-widget in the Overlay
        // Designer (like any other widget's own IsVisible flag, and its own configured
        // max-visible count) rather than a single global setting - this just returns
        // everything pending and lets each widget instance decide how much of it to show.
        var queue = WadevoSongRequestHub.SongRequestService.GetQueue()
            .Where(r => !r.IsPlayed)
            .OrderBy(r => r.RequestedAtUtc)
            .Take(50)
            .Select(r => new { r.SongText, r.RequesterUsername })
            .ToList();

        return JsonSerializer.Serialize(new { Items = queue });
    }

    private static string CreateGoalDataJson(HttpListenerContext context)
    {
        string metric = context.Request.QueryString["metric"] ?? "Followers";
        string platform = context.Request.QueryString["platform"] ?? "All";
        DashboardStatsModel stats = WadevoDashboardHub.StatsService.GetStats();

        // Followers/Subscriptions use the real, current total (WadevoChannelStatsCache) -
        // either combined across every connected platform, or one specific platform if the
        // widget was configured to track just that one - a goal like "28 / 50 followers"
        // needs your actual current count, not "how many new follows happened since Wadevo
        // opened" (which is what the session-based DashboardStatsService counters track,
        // correctly, for their own purpose). GiftSubs/Votes don't have a meaningful
        // "lifetime total" from any platform API, so those stay session-based, which is the
        // right behavior for a "gifted subs this stream" or "votes this stream" goal.
        int current = metric switch
        {
            "Subscriptions" => platform switch
            {
                "Twitch" => WadevoChannelStatsCache.TwitchSubscriberCount,
                "Blaze" => WadevoChannelStatsCache.BlazeSubscriberCount,
                _ => WadevoChannelStatsCache.TotalSubscriberCount
            },
            "GiftSubs" => stats.GiftSubCount,
            "Votes" => stats.VoteCount,
            _ => platform switch
            {
                "Twitch" => WadevoChannelStatsCache.TwitchFollowerCount,
                "Blaze" => WadevoChannelStatsCache.BlazeFollowerCount,
                _ => WadevoChannelStatsCache.TotalFollowerCount
            }
        };

        return JsonSerializer.Serialize(new { Current = current });
    }

    private static string CreateVotesDataJson()
    {
        int voteCount = WadevoDashboardHub.StatsService.GetStats().VoteCount;

        return JsonSerializer.Serialize(new
        {
            VoteCount = voteCount
        });
    }

    // A persistent widget (unlike the alert popup that already fires per individual vote)
    // showing the running tally for the session, so viewers can see progress build up over
    // the whole stream rather than only catching a brief per-vote alert if they look away.
    private static string CreateVotesOverlay()
    {
        return """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Wadevo Votes</title>
            <style>
                body {
                    margin: 0;
                    background: transparent;
                    overflow: hidden;
                    font-family: Segoe UI, Arial, sans-serif;
                }

                .votes-panel {
                    display: inline-flex;
                    align-items: center;
                    gap: 14px;
                    margin: 20px;
                    padding: 14px 24px;
                    border-radius: 999px;
                    background: linear-gradient(135deg, rgba(3, 20, 15, .96), rgba(0, 6, 5, .94));
                    border: 2px solid #ffbf57;
                    box-shadow: 0 0 18px #ffbf57, 0 20px 60px rgba(0, 0, 0, .8);
                }

                .votes-icon {
                    font-size: 26px;
                }

                .votes-count {
                    font-size: 28px;
                    font-weight: 900;
                    color: #ffbf57;
                    text-shadow: 0 0 14px #ffbf57;
                    transition: transform 180ms ease;
                }

                .votes-count.bump {
                    transform: scale(1.25);
                }

                .votes-label {
                    font-size: 13px;
                    letter-spacing: 0.1em;
                    text-transform: uppercase;
                    color: #f5fff9;
                    opacity: 0.75;
                }
            </style>
        </head>
        <body>
            <div class="votes-panel">
                <div class="votes-icon">🗳️</div>
                <div>
                    <div class="votes-count" id="count">0</div>
                    <div class="votes-label">Votes this stream</div>
                </div>
            </div>

            <script>
                let lastCount = null;
                const countEl = document.getElementById("count");

                async function ping() {
                    try {
                        await fetch("/ping?route=/votes");
                    } catch {
                    }
                }

                async function refresh() {
                    try {
                        const response = await fetch("/votes-data");
                        const data = await response.json();

                        if (lastCount !== null && data.VoteCount > lastCount) {
                            countEl.classList.add("bump");
                            setTimeout(() => countEl.classList.remove("bump"), 180);
                        }

                        countEl.textContent = data.VoteCount;
                        lastCount = data.VoteCount;
                    } catch {
                    }
                }

                setInterval(ping, 2000);
                setInterval(refresh, 1500);
                ping();
                refresh();
            </script>
        </body>
        </html>
        """;
    }

    private static string CreateChatDataJson()
    {
        lock (ChatLock)
        {
            var payload = ChatMessages.Select(m => new
            {
                m.Id,
                m.Platform,
                m.PlatformGlyph,
                m.PlatformColorHex,
                m.Username,
                m.Message
            });

            return JsonSerializer.Serialize(payload);
        }
    }

    // Merges chat from every connected platform into one scrolling feed, badge-tagged by
    // platform so viewers (and the streamer) can still tell who's talking where - this is
    // the feature that makes running Blaze and Twitch at once actually feel unified,
    // instead of needing two separate chat windows/overlays side by side.
    private static string CreateCombinedChatOverlay()
    {
        ChatOverlaySettings settings = new ChatOverlaySettingsStore().Load();

        string bubbleBgRgba = HexToRgba(settings.BubbleBackgroundHex, settings.BubbleOpacityPercent / 100.0);
        string justify = settings.Alignment == "right" ? "flex-end" : "flex-start";
        string flexAlign = settings.Alignment == "right" ? "row-reverse" : "row";
        string platformLabelDisplay = settings.ShowPlatformLabel ? "inline" : "none";

        string html = $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Wadevo Combined Chat</title>
            <style>
                body {
                    margin: 0;
                    background: transparent;
                    overflow: hidden;
                    font-family: {{settings.FontFamily}}, Segoe UI, Arial, sans-serif;
                }

                #feed {
                    display: flex;
                    flex-direction: column;
                    justify-content: flex-end;
                    align-items: {{justify}};
                    width: 480px;
                    max-height: 100vh;
                    padding: 16px;
                    box-sizing: border-box;
                    gap: 6px;
                }

                .message {
                    display: flex;
                    flex-direction: {{flexAlign}};
                    align-items: flex-start;
                    gap: 8px;
                    padding: 8px 12px;
                    border-radius: 10px;
                    background: {{bubbleBgRgba}};
                    border-left: 4px solid #45d9ff;
                    animation: slideIn 220ms ease-out;
                    word-break: break-word;
                    max-width: 100%;
                }

                @keyframes slideIn {
                    from { opacity: 0; transform: translateY(8px); }
                    to { opacity: 1; transform: translateY(0); }
                }

                .badge {
                    flex: 0 0 auto;
                    font-size: {{settings.FontSizePx}}px;
                    line-height: {{settings.FontSizePx + 6}}px;
                }

                .body {
                    flex: 1 1 auto;
                    min-width: 0;
                }

                .username {
                    font-weight: 700;
                    font-size: {{settings.FontSizePx}}px;
                    margin-right: 6px;
                }

                .text {
                    font-size: {{settings.FontSizePx}}px;
                    color: {{settings.TextColorHex}};
                }

                .platform-label {
                    display: {{platformLabelDisplay}};
                    font-size: {{Math.Max(9, settings.FontSizePx - 4)}}px;
                    text-transform: uppercase;
                    letter-spacing: 0.06em;
                    opacity: 0.6;
                    margin-left: 6px;
                }
            </style>
        </head>
        <body>
            <div id="feed"></div>

            <script>
                let lastSeenId = 0;
                const feedEl = document.getElementById("feed");
                const MAX_VISIBLE = {{Math.Max(1, settings.MaxVisibleMessages)}};

                async function ping() {
                    try {
                        await fetch("/ping?route=/chat");
                    } catch {
                    }
                }

                function renderMessage(m) {
                    const row = document.createElement("div");
                    row.className = "message";
                    row.style.borderLeftColor = m.PlatformColorHex;

                    row.innerHTML = `
                        <div class="badge">${m.PlatformGlyph}</div>
                        <div class="body">
                            <span class="username" style="color:${m.PlatformColorHex}">${escapeHtml(m.Username)}</span>
                            <span class="text">${escapeHtml(m.Message)}</span>
                            <span class="platform-label">${escapeHtml(m.Platform)}</span>
                        </div>
                    `;

                    feedEl.appendChild(row);

                    while (feedEl.children.length > MAX_VISIBLE) {
                        feedEl.removeChild(feedEl.firstChild);
                    }
                }

                function escapeHtml(text) {
                    const div = document.createElement("div");
                    div.textContent = text ?? "";
                    return div.innerHTML;
                }

                async function refresh() {
                    try {
                        const response = await fetch("/chat-data");
                        const messages = await response.json();

                        const newMessages = messages.filter(m => m.Id > lastSeenId);

                        for (const message of newMessages) {
                            renderMessage(message);
                            lastSeenId = Math.max(lastSeenId, message.Id);
                        }
                    } catch {
                    }
                }

                setInterval(ping, 2000);
                setInterval(refresh, 1000);
                ping();
                refresh();
            </script>
        </body>
        </html>
        """;

        return html;
    }

    private static string HexToRgba(string hex, double opacity)
    {
        try
        {
            System.Drawing.Color color = System.Drawing.ColorTranslator.FromHtml(hex);
            return $"rgba({color.R}, {color.G}, {color.B}, {opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        }
        catch
        {
            return $"rgba(16, 24, 39, {opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
        }
    }

    private string CreateNowPlayingDataJson()
    {
        (string liveArtist, string liveSong) = SplitCurrentSong(_getCurrentSong());
        MusicMetadataModel? metadata = _getCurrentMetadata();

        return JsonSerializer.Serialize(new
        {
            Artist = liveArtist,
            Song = liveSong,
            Album = metadata?.AlbumName ?? "",
            ReleaseDate = metadata?.ReleaseDate ?? "",
            ArtworkUrl = metadata?.ArtworkUrl ?? ""
        });
    }

    private static string CreatePingJson(HttpListenerContext context)
    {
        string route = MarkClientRouteFromQuery(context);

        return JsonSerializer.Serialize(new
        {
            Route = route,
            Active = IsRouteActive(route),
            ServerTimeUtc = DateTime.UtcNow
        });
    }

    private static string CreateMediaResponse(HttpListenerContext context, out bool isMediaResponse)
    {
        isMediaResponse = false;

        string? requestedPath = context.Request.QueryString["path"];

        if (string.IsNullOrWhiteSpace(requestedPath) || !File.Exists(requestedPath))
        {
            return CreateNotFoundPage("/media");
        }

        isMediaResponse = true;
        return requestedPath;
    }

    private static async Task WriteMediaResponse(HttpListenerContext context, string mediaPath)
    {
        string extension = Path.GetExtension(mediaPath).ToLowerInvariant();

        context.Response.ContentType = extension switch
        {
            ".gif" => "image/gif",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };

        byte[] buffer = await File.ReadAllBytesAsync(mediaPath);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.StatusCode = 200;

        await context.Response.OutputStream.WriteAsync(buffer);
    }

    private static string MarkClientRouteFromQuery(HttpListenerContext context)
    {
        string route = context.Request.QueryString["route"] ?? "";
        route = NormalizeRoute(route);

        if (route is "/nowplaying" or "/commands" or "/alerts" or "/votes" or "/chat" or "/debug")
        {
            MarkRouteSeen(route);
        }

        return route;
    }

    private static string CreateHomePage()
    {
        return """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Wadevo Overlay Engine</title>
        </head>
        <body style="margin:0;background:#08111f;color:#e9f7ff;font-family:Segoe UI,Arial,sans-serif;">
            <div style="padding:36px;">
                <h1 style="color:#45d9ff;">🚀 Wadevo Overlay Engine</h1>
                <p>Overlay Engine is running.</p>
                <p><a style="color:#45d9ff;" href="/nowplaying">Song ID</a></p>
                <p><a style="color:#45d9ff;" href="/commands">Commands</a></p>
                <p><a style="color:#45d9ff;" href="/alerts">Alerts</a></p>
                <p><a style="color:#45d9ff;" href="/chat">Combined Chat</a></p>
                <p><a style="color:#45d9ff;" href="/debug">Debug</a></p>
            </div>
        </body>
        </html>
        """;
    }

    // Renders one specific saved overlay by id, independent of whichever overlay (if any)
    // is currently marked "live" - this is what makes it possible to add several overlays
    // as separate OBS Browser Sources at the same time, each showing its own design,
    // instead of everything being funneled through the single "live" slot /nowplaying uses.
    private string CreateSpecificOverlayPage(HttpListenerContext context)
    {
        string presetId = context.Request.QueryString["id"] ?? "";

        if (string.IsNullOrWhiteSpace(presetId))
        {
            return CreateMissingOverlayPage("No overlay id was given in the URL.");
        }

        bool exists = new WadevoDesignerPresetStore().LoadAll().Any(preset => preset.Id == presetId);

        if (!exists)
        {
            return CreateMissingOverlayPage("This overlay was deleted or its id is no longer valid.");
        }

        return CreateNowPlayingOverlay(presetId);
    }

    private static string CreateMissingOverlayPage(string reason)
    {
        string safeReason = WebUtility.HtmlEncode(reason);

        return $$"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Overlay Not Found</title></head>
        <body style="margin:0;background:transparent;color:#e9f7ff;font-family:Segoe UI,Arial,sans-serif;">
            <div style="padding:16px;font-size:13px;opacity:.7;">⚠️ {{safeReason}}</div>
        </body>
        </html>
        """;
    }

    // explicitPresetId lets a specific saved overlay be rendered directly (via the new
    // /overlay?id=X route), instead of always resolving to whichever one preset happens to
    // be marked "live" - that single-slot model meant only one overlay could ever be shown
    // in OBS at a time. Passing null preserves /nowplaying's original behavior exactly.
    private string CreateNowPlayingOverlay(string? explicitPresetId = null)
    {
        string? livePresetId = explicitPresetId ?? new LiveOverlaySettingsStore().GetLivePresetId();
        WadevoDesignerPresetModel? livePreset = livePresetId is null
            ? null
            : new WadevoDesignerPresetStore().LoadAll().FirstOrDefault(preset => preset.Id == livePresetId);

        List<WadevoDesignerElementState> allElements;
        string backgroundImagePath;
        string backgroundScaleMode;
        bool backgroundRoundedCorners;
        int backgroundWidthPercent;
        int backgroundHeightPercent;
        int backgroundOpacityPercent;
        int backgroundOffsetX;
        int backgroundOffsetY;
        string animationType;
        int animationDurationMs;
        int autoHideSeconds;
        bool alwaysOn;

        if (livePreset is not null)
        {
            allElements = livePreset.Elements;
            backgroundImagePath = livePreset.BackgroundImagePath;
            backgroundScaleMode = livePreset.BackgroundScaleMode;
            backgroundRoundedCorners = livePreset.BackgroundRoundedCorners;
            backgroundWidthPercent = livePreset.BackgroundWidthPercent;
            backgroundHeightPercent = livePreset.BackgroundHeightPercent;
            backgroundOpacityPercent = livePreset.BackgroundOpacityPercent;
            backgroundOffsetX = livePreset.BackgroundOffsetX;
            backgroundOffsetY = livePreset.BackgroundOffsetY;
            animationType = livePreset.AnimationType;
            animationDurationMs = livePreset.AnimationDurationMs;
            autoHideSeconds = livePreset.AutoHideSeconds;
            alwaysOn = livePreset.AlwaysOn;
        }
        else
        {
            allElements = new WadevoDesignerDocumentStore().Load().Elements.ToList();
            backgroundImagePath = "";
            backgroundScaleMode = "Fill";
            backgroundRoundedCorners = true;
            backgroundWidthPercent = 100;
            backgroundHeightPercent = 100;
            backgroundOpacityPercent = 100;
            backgroundOffsetX = 0;
            backgroundOffsetY = 0;
            animationType = "None";
            animationDurationMs = 500;
            autoHideSeconds = 0;
            alwaysOn = false;
        }

        bool hasCustomLayout = allElements.Any(element =>
            element.Kind != WadevoDesignerElementKind.PreviewSurface);

        if (!hasCustomLayout)
        {
            return CreateLegacyThemeCardOverlay();
        }

        List<WadevoDesignerElementState> elements = allElements
            .Where(element => element.IsVisible)
            .ToList();

        (string liveArtist, string liveSong) = SplitCurrentSong(_getCurrentSong());
        MusicMetadataModel? metadata = _getCurrentMetadata();

        string liveAlbum = metadata?.AlbumName ?? "";
        string liveReleaseDate = metadata?.ReleaseDate ?? "";
        string liveArtworkUrl = metadata?.ArtworkUrl ?? "";

        int canvasWidth = Math.Max(320, elements.Count == 0 ? 900 : Math.Max(900, elements.Max(e => e.X + e.Width) + 40));
        int canvasHeight = Math.Max(160, elements.Count == 0 ? 400 : Math.Max(400, elements.Max(e => e.Y + e.Height) + 40));

        string canvasBackgroundHtml = BuildCanvasBackgroundHtml(
            backgroundImagePath, backgroundScaleMode, backgroundRoundedCorners,
            backgroundWidthPercent, backgroundHeightPercent, backgroundOpacityPercent,
            backgroundOffsetX, backgroundOffsetY, canvasWidth, canvasHeight);

        string fontFaceCss = BuildFontFaceCss(elements);

        string animationClass = animationType switch
        {
            "SlideLeft" => "anim-slideleft",
            "SlideRight" => "anim-slideright",
            "Fade" => "anim-fade",
            _ => ""
        };

        string alwaysOnJs = alwaysOn ? "true" : "false";
        string autoHideSecondsJs = autoHideSeconds.ToString();
        string initialCanvasClass = alwaysOn ? "canvas" : $"canvas motion-enabled {animationClass}";

        StringBuilder elementsHtml = new();

        foreach (WadevoDesignerElementState element in elements)
        {
            elementsHtml.AppendLine(RenderElementHtml(
                element,
                liveArtist,
                liveSong,
                liveAlbum,
                liveReleaseDate,
                liveArtworkUrl));
        }

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Song ID</title>
            <style>
                {{fontFaceCss}}

                body {
                    margin: 0;
                    background: transparent;
                    overflow: hidden;
                    font-family: Segoe UI, Arial, sans-serif;
                }

                .canvas {
                    position: relative;
                    width: {{canvasWidth}}px;
                    height: {{canvasHeight}}px;
                    overflow: hidden;
                }

                .canvas.motion-enabled {
                    opacity: 0;
                    transition: opacity {{animationDurationMs}}ms ease, transform {{animationDurationMs}}ms ease;
                }

                .canvas.motion-enabled.anim-slideright {
                    transform: translateX(-40px);
                }

                .canvas.motion-enabled.anim-slideleft {
                    transform: translateX(40px);
                }

                .canvas.motion-enabled.anim-fade {
                    transform: none;
                }

                .canvas.motion-enabled.visible {
                    opacity: 1;
                    transform: translateX(0);
                }

                .element {
                    position: absolute;
                    box-sizing: border-box;
                }

                .element-text {
                    display: flex;
                    align-items: center;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                    text-shadow: 0 2px 10px rgba(0, 0, 0, 0.55);
                }

                .element-artwork img {
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                    border-radius: 14px;
                    box-shadow: 0 12px 30px rgba(0, 0, 0, 0.45);
                }

                .element-progress-track {
                    width: 100%;
                    height: 100%;
                    border-radius: 999px;
                    background: rgba(255, 255, 255, 0.16);
                    overflow: hidden;
                }

                .element-progress-fill {
                    width: 100%;
                    height: 100%;
                    border-radius: 999px;
                    background: #34d399;
                }
            </style>
            <script>
                let lastArtist = null;
                let lastSong = null;
                const alwaysOn = {{alwaysOnJs}};
                const autoHideSeconds = {{autoHideSecondsJs}};
                let hideTimer = null;
                let reshowTimer = null;

                async function ping() {
                    try {
                        await fetch("/ping?route=/nowplaying");
                    } catch {
                    }
                }

                function applyRoleValue(role, value) {
                    document.querySelectorAll("[data-role=\"" + role + "\"]").forEach(function (el) {
                        if (el.tagName === "IMG") {
                            if (value) {
                                el.src = value;
                                el.style.display = "block";
                            } else {
                                el.removeAttribute("src");
                                el.style.display = "none";
                            }
                        } else {
                            el.textContent = value;
                        }
                    });
                }

                function playEntranceAnimation() {
                    if (alwaysOn) {
                        return;
                    }

                    const canvas = document.querySelector(".canvas");

                    canvas.classList.remove("visible");
                    void canvas.offsetWidth;
                    canvas.classList.add("visible");

                    scheduleAutoHide();
                }

                function playExitAnimation() {
                    if (alwaysOn) {
                        return;
                    }

                    document.querySelector(".canvas").classList.remove("visible");
                }

                // One-shot: shows, stays for autoHideSeconds, then hides and stays hidden -
                // it does NOT reappear on its own timer. Reappearing only happens from a
                // genuine new trigger (playEntranceAnimation being called again, e.g. when
                // pollNowPlayingData below detects an actual song change) - a blind timer
                // loop meant this kept cycling the overlay back into view again and again
                // even while the exact same song was still playing, which wasn't tied to
                // anything actually happening.
                function scheduleAutoHide() {
                    if (hideTimer) clearTimeout(hideTimer);

                    if (alwaysOn || autoHideSeconds <= 0) {
                        return;
                    }

                    hideTimer = setTimeout(function () {
                        playExitAnimation();
                    }, autoHideSeconds * 1000);
                }

                async function pollNowPlayingData() {
                    try {
                        const response = await fetch("/nowplaying-data");
                        const data = await response.json();

                        const isFirstLoad = lastArtist === null;
                        const songChanged = data.Artist !== lastArtist || data.Song !== lastSong;

                        applyRoleValue("artist", data.Artist);
                        applyRoleValue("song", data.Song);
                        applyRoleValue("album", data.Album);
                        applyRoleValue("releasedate", data.ReleaseDate);
                        applyRoleValue("artwork", data.ArtworkUrl);

                        if (isFirstLoad || songChanged) {
                            playEntranceAnimation();
                        }

                        lastArtist = data.Artist;
                        lastSong = data.Song;
                    } catch {
                    }
                }

                setInterval(ping, 2000);
                setInterval(pollNowPlayingData, 1500);
                ping();
                pollNowPlayingData();
            </script>
        </head>
        <body>
            <div class="{{initialCanvasClass}}">
                {{canvasBackgroundHtml}}
                {{elementsHtml}}
            </div>
        </body>
        </html>
        """;
    }

    // Computes the background image's exact on-screen rectangle using the shared
    // BackgroundRectCalculator - the same one WadevoDesignerCanvas uses for the live
    // Designer preview and the Alert popup uses too. This used to be a completely separate
    // CSS background-image + transform:scale() implementation that drifted significantly
    // from what the Designer actually showed, especially once an image had been resized
    // away from 100%/100%. An absolutely-positioned <img> at an explicit pixel rect, computed
    // the same way everywhere, keeps what you see in the Designer and what OBS actually
    // displays in sync.
    private static string BuildCanvasBackgroundHtml(
        string backgroundImagePath,
        string backgroundScaleMode,
        bool roundedCorners,
        int widthPercent,
        int heightPercent,
        int opacityPercent,
        int offsetX,
        int offsetY,
        int canvasWidth,
        int canvasHeight)
    {
        if (string.IsNullOrWhiteSpace(backgroundImagePath) || !File.Exists(backgroundImagePath))
        {
            return "";
        }

        int imageWidth;
        int imageHeight;

        try
        {
            using System.Drawing.Image image = System.Drawing.Image.FromFile(backgroundImagePath);
            imageWidth = image.Width;
            imageHeight = image.Height;
        }
        catch
        {
            return "";
        }

        (int x, int y, int width, int height) = BackgroundRectCalculator.ComputeDestRect(
            0, 0, canvasWidth, canvasHeight, imageWidth, imageHeight, backgroundScaleMode, widthPercent, heightPercent, offsetX, offsetY);

        string encodedPath = Uri.EscapeDataString(backgroundImagePath);
        string cornerStyle = roundedCorners ? "border-radius: 16px;" : "";
        double opacity = Math.Clamp(opacityPercent, 0, 100) / 100.0;

        return $"""
        <img class="canvas-background" src="/media?path={encodedPath}" style="
            position: absolute;
            left: {x}px;
            top: {y}px;
            width: {width}px;
            height: {height}px;
            object-fit: fill;
            opacity: {opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)};
            {cornerStyle}
        " />
        """;
    }

    private static string BuildAllCustomFontFacesCss()
    {
        StringBuilder css = new();

        foreach (string fontName in Wadevo.Services.CustomFontService.GetCustomFontNames())
        {
            if (!Wadevo.Services.CustomFontService.TryGetCustomFontFilePath(fontName, out string? filePath) ||
                filePath is null)
            {
                continue;
            }

            string encodedPath = Uri.EscapeDataString(filePath);
            string safeFontName = fontName.Replace("\"", "");

            css.AppendLine($$"""
            @font-face {
                font-family: "{{safeFontName}}";
                src: url('/media?path={{encodedPath}}');
            }
            """);
        }

        return css.ToString();
    }

    private static string BuildFontFaceCss(IEnumerable<WadevoDesignerElementState> elements)
    {
        HashSet<string> uniqueFontNames = elements
            .Where(element => !string.IsNullOrWhiteSpace(element.FontFamily))
            .Select(element => element.FontFamily)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        StringBuilder css = new();

        foreach (string fontName in uniqueFontNames)
        {
            if (!Wadevo.Services.CustomFontService.TryGetCustomFontFilePath(fontName, out string? filePath) ||
                filePath is null)
            {
                // Not a custom font - it's a regular system font the browser already knows.
                continue;
            }

            string encodedPath = Uri.EscapeDataString(filePath);
            string safeFontName = fontName.Replace("\"", "");

            css.AppendLine($$"""
            @font-face {
                font-family: "{{safeFontName}}";
                src: url('/media?path={{encodedPath}}');
            }
            """);
        }

        return css.ToString();
    }

    private static string RenderElementHtml(
        WadevoDesignerElementState element,
        string liveArtist,
        string liveSong,
        string liveAlbum,
        string liveReleaseDate,
        string liveArtworkUrl)
    {
        string positionStyle =
            $"left:{element.X}px; top:{element.Y}px; width:{element.Width}px; height:{element.Height}px;";

        string dataRole = GetElementRole(element.Name);
        string roleAttribute = dataRole.Length > 0 ? $" data-role=\"{dataRole}\"" : "";

        switch (element.Kind)
        {
            case WadevoDesignerElementKind.Text:
            {
                string text = EmoteRenderHelper.RenderTrustedTextToHtml(ResolveLiveText(
                    element,
                    liveArtist,
                    liveSong,
                    liveAlbum,
                    liveReleaseDate));

                string fontFamily = WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(element.FontFamily) ? "Segoe UI" : element.FontFamily);

                string fontWeight = element.FontBold ? "700" : "400";

                System.Drawing.Color color = System.Drawing.Color.FromArgb(element.FontColorArgb);
                string colorCss = $"rgb({color.R}, {color.G}, {color.B})";

                string textStyle =
                    $"font-family: '{fontFamily}', Segoe UI, Arial, sans-serif; " +
                    $"font-size: {element.FontSize}px; " +
                    $"font-weight: {fontWeight}; " +
                    $"color: {colorCss};";

                return $"""<div class="element element-text"{roleAttribute} style="{positionStyle} {textStyle}">{text}</div>""";
            }

            case WadevoDesignerElementKind.Artwork:
            {
                string src = WebUtility.HtmlEncode(liveArtworkUrl);
                string displayStyle = string.IsNullOrWhiteSpace(liveArtworkUrl) ? "display:none;" : "";

                return $"""<div class="element element-artwork" style="{positionStyle}"><img data-role="artwork" src="{src}" style="{displayStyle}" alt="Album artwork"></div>""";
            }

            case WadevoDesignerElementKind.Image:
            {
                if (string.IsNullOrWhiteSpace(element.ImagePath))
                {
                    return "";
                }

                string src = WebUtility.HtmlEncode("/media?path=" + Uri.EscapeDataString(element.ImagePath));

                string extension = Path.GetExtension(element.ImagePath).ToLowerInvariant();
                bool isVideo = extension is ".mp4" or ".webm" or ".mov";

                if (isVideo)
                {
                    return $"""<div class="element" style="{positionStyle}"><video src="{src}" style="width:100%; height:100%; object-fit:contain;" autoplay muted loop playsinline></video></div>""";
                }

                return $"""<div class="element" style="{positionStyle}"><img src="{src}" style="width:100%; height:100%; object-fit:contain;" alt=""></div>""";
            }

            case WadevoDesignerElementKind.Clock:
            {
                string textStyle = BuildTextElementStyle(element);
                string elementId = "clock-" + element.Id;
                string clockFormat = WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(element.ClockFormat) ? "h:mm tt" : element.ClockFormat);

                return $$"""
                <div class="element element-text" id="{{elementId}}" style="{{positionStyle}} {{textStyle}}"></div>
                <script>
                    (function () {
                        var el = document.getElementById("{{elementId}}");
                        var fmt = "{{clockFormat}}";

                        function pad(n) { return n < 10 ? "0" + n : "" + n; }

                        function render() {
                            var now = new Date();
                            var hours24 = now.getHours();
                            var hours12 = hours24 % 12 === 0 ? 12 : hours24 % 12;
                            var ampm = hours24 >= 12 ? "PM" : "AM";
                            var text = fmt
                                .replace("HH", pad(hours24))
                                .replace("hh", pad(hours12))
                                .replace("h", "" + hours12)
                                .replace("mm", pad(now.getMinutes()))
                                .replace("ss", pad(now.getSeconds()))
                                .replace("tt", ampm);
                            el.textContent = text;
                        }

                        render();
                        setInterval(render, 1000);
                    })();
                </script>
                """;
            }

            case WadevoDesignerElementKind.Countdown:
            {
                string textStyle = BuildTextElementStyle(element);
                string elementId = "countdown-" + element.Id;
                string targetIso = element.CountdownTargetUtc.ToString("O");

                // JSON-encoding (rather than HTML-encoding) is what's needed here since
                // these values are embedded as JS string literals inside a <script> block,
                // not as HTML text - this keeps quotes/backslashes in a label or completed
                // message from breaking out of the string.
                string labelJs = JsonSerializer.Serialize(element.CountdownLabel ?? "");
                string completedJs = JsonSerializer.Serialize(
                    string.IsNullOrWhiteSpace(element.CountdownCompletedText)
                        ? "00:00:00"
                        : element.CountdownCompletedText);

                return $$"""
                <div class="element element-text" id="{{elementId}}" style="{{positionStyle}} {{textStyle}}"></div>
                <script>
                    (function () {
                        var el = document.getElementById("{{elementId}}");
                        var target = new Date("{{targetIso}}").getTime();
                        var label = {{labelJs}};
                        var completedText = {{completedJs}};

                        function pad(n) { return n < 10 ? "0" + n : "" + n; }

                        function render() {
                            var remainingMs = target - Date.now();

                            if (remainingMs <= 0) {
                                el.textContent = completedText;
                                return;
                            }

                            var totalSeconds = Math.floor(remainingMs / 1000);
                            var days = Math.floor(totalSeconds / 86400);
                            var hours = Math.floor((totalSeconds % 86400) / 3600);
                            var minutes = Math.floor((totalSeconds % 3600) / 60);
                            var seconds = totalSeconds % 60;

                            var timeText = (days > 0 ? days + "d " : "") +
                                pad(hours) + ":" + pad(minutes) + ":" + pad(seconds);

                            el.textContent = label ? (label + " " + timeText) : timeText;
                        }

                        render();
                        setInterval(render, 1000);
                    })();
                </script>
                """;
            }

            case WadevoDesignerElementKind.SongQueue:
            {
                string textStyle = BuildTextElementStyle(element);
                string elementId = "songqueue-" + element.Id;
                int maxVisible = Math.Max(1, element.SongQueueMaxVisible);

                return $$"""
                <div class="element" id="{{elementId}}" style="{{positionStyle}} {{textStyle}} overflow: hidden;">
                    <div id="{{elementId}}-list"></div>
                </div>
                <script>
                    (function () {
                        var list = document.getElementById("{{elementId}}-list");
                        var maxVisible = {{maxVisible}};

                        async function refresh() {
                            try {
                                var response = await fetch("/songqueue-data");
                                var data = await response.json();
                                var items = (data.Items || []).slice(0, maxVisible);

                                list.innerHTML = "";

                                if (items.length === 0) {
                                    var empty = document.createElement("div");
                                    empty.style.opacity = "0.6";
                                    empty.style.fontSize = "0.8em";
                                    empty.textContent = "No requests yet";
                                    list.appendChild(empty);
                                    return;
                                }

                                items.forEach(function (item) {
                                    var row = document.createElement("div");
                                    row.style.display = "flex";
                                    row.style.justifyContent = "space-between";
                                    row.style.alignItems = "baseline";
                                    row.style.padding = "4px 0";
                                    row.style.borderBottom = "1px solid rgba(255,255,255,0.12)";

                                    var song = document.createElement("div");
                                    song.style.overflow = "hidden";
                                    song.style.textOverflow = "ellipsis";
                                    song.style.whiteSpace = "nowrap";
                                    song.textContent = item.SongText;

                                    var requester = document.createElement("div");
                                    requester.style.opacity = "0.7";
                                    requester.style.fontSize = "0.75em";
                                    requester.style.marginLeft = "10px";
                                    requester.style.whiteSpace = "nowrap";
                                    requester.textContent = item.RequesterUsername;

                                    row.appendChild(song);
                                    row.appendChild(requester);
                                    list.appendChild(row);
                                });
                            } catch {
                            }
                        }

                        refresh();
                        setInterval(refresh, 4000);
                    })();
                </script>
                """;
            }

            case WadevoDesignerElementKind.ChatFeed:
            {
                ChatOverlaySettings chatSettings = new ChatOverlaySettingsStore().Load();

                string elementId = "chatfeed-" + element.Id;
                string bubbleBgRgba = HexToRgba(chatSettings.BubbleBackgroundHex, chatSettings.BubbleOpacityPercent / 100.0);
                string flexAlign = chatSettings.Alignment == "right" ? "row-reverse" : "row";
                string alignItems = chatSettings.Alignment == "right" ? "flex-end" : "flex-start";
                string platformLabelDisplay = chatSettings.ShowPlatformLabel ? "inline" : "none";
                int maxVisible = Math.Max(1, chatSettings.MaxVisibleMessages);

                return $$"""
                <div class="element" id="{{elementId}}" style="{{positionStyle}} overflow: hidden;">
                    <div id="{{elementId}}-feed" style="display:flex; flex-direction:column; justify-content:flex-end; align-items:{{alignItems}}; height:100%; gap:6px; box-sizing:border-box; font-family:{{chatSettings.FontFamily}}, Segoe UI, Arial, sans-serif;"></div>
                </div>
                <script>
                    (function () {
                        var feedEl = document.getElementById("{{elementId}}-feed");
                        var lastSeenId = 0;
                        var maxVisible = {{maxVisible}};

                        function escapeHtml(text) {
                            var div = document.createElement("div");
                            div.textContent = text || "";
                            return div.innerHTML;
                        }

                        function renderMessage(m) {
                            var row = document.createElement("div");
                            row.style.display = "flex";
                            row.style.flexDirection = "{{flexAlign}}";
                            row.style.alignItems = "flex-start";
                            row.style.gap = "8px";
                            row.style.padding = "8px 12px";
                            row.style.borderRadius = "10px";
                            row.style.background = "{{bubbleBgRgba}}";
                            row.style.borderLeft = "4px solid " + m.PlatformColorHex;
                            row.style.wordBreak = "break-word";
                            row.style.maxWidth = "100%";
                            row.style.fontSize = "{{chatSettings.FontSizePx}}px";

                            row.innerHTML =
                                '<div style="flex:0 0 auto;">' + m.PlatformGlyph + '</div>' +
                                '<div style="flex:1 1 auto; min-width:0;">' +
                                '<span style="font-weight:700; color:' + m.PlatformColorHex + '; margin-right:6px;">' + escapeHtml(m.Username) + '</span>' +
                                '<span style="color:{{chatSettings.TextColorHex}};">' + escapeHtml(m.Message) + '</span>' +
                                '<span style="display:{{platformLabelDisplay}}; font-size:0.75em; text-transform:uppercase; opacity:0.6; margin-left:6px;">' + escapeHtml(m.Platform) + '</span>' +
                                '</div>';

                            feedEl.appendChild(row);

                            while (feedEl.children.length > maxVisible) {
                                feedEl.removeChild(feedEl.firstChild);
                            }
                        }

                        async function refresh() {
                            try {
                                var response = await fetch("/chat-data");
                                var messages = await response.json();
                                var newMessages = messages.filter(function (m) { return m.Id > lastSeenId; });

                                newMessages.forEach(function (message) {
                                    renderMessage(message);
                                    lastSeenId = Math.max(lastSeenId, message.Id);
                                });
                            } catch {
                            }
                        }

                        refresh();
                        setInterval(refresh, 1000);
                    })();
                </script>
                """;
            }

            case WadevoDesignerElementKind.VoteTally:
            {
                string textStyle = BuildTextElementStyle(element);
                string elementId = "votetally-" + element.Id;
                string template = string.IsNullOrWhiteSpace(element.Text) ? "🗳️ {count} votes" : element.Text;
                string templateJs = JsonSerializer.Serialize(template);

                return $$"""
                <div class="element" id="{{elementId}}" style="{{positionStyle}} {{textStyle}}"></div>
                <script>
                    (function () {
                        var el = document.getElementById("{{elementId}}");
                        var template = {{templateJs}};

                        async function refresh() {
                            try {
                                var response = await fetch("/votes-data");
                                var data = await response.json();
                                var count = data.VoteCount || 0;

                                el.textContent = template.replace("{count}", count);
                            } catch {
                            }
                        }

                        refresh();
                        setInterval(refresh, 3000);
                    })();
                </script>
                """;
            }

            case WadevoDesignerElementKind.ProgressBar:
            {
                string textStyle = BuildTextElementStyle(element);
                string elementId = "goal-" + element.Id;
                string metric = WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(element.GoalMetric) ? "Followers" : element.GoalMetric);
                string platform = WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(element.GoalPlatform) ? "All" : element.GoalPlatform);
                int target = Math.Max(1, element.GoalTarget);

                System.Drawing.Color fillColor = System.Drawing.Color.FromArgb(element.ProgressFillColorArgb);
                System.Drawing.Color trackColor = System.Drawing.Color.FromArgb(element.ProgressTrackColorArgb);
                string fillCss = $"rgba({fillColor.R}, {fillColor.G}, {fillColor.B}, {fillColor.A / 255.0:0.###})";
                string trackCss = $"rgba({trackColor.R}, {trackColor.G}, {trackColor.B}, {trackColor.A / 255.0:0.###})";

                // A friendlier label than the raw metric key stored on the element.
                string metricLabelJs = JsonSerializer.Serialize(metric switch
                {
                    "Subscriptions" => "Subscriptions",
                    "GiftSubs" => "Gift Subs",
                    "Votes" => "Votes",
                    _ => "Followers"
                });

                return $$"""
                <div class="element" id="{{elementId}}" style="{{positionStyle}}">
                    <div style="width:100%; height:16px; border-radius:8px; overflow:hidden; background:{{trackCss}};">
                        <div id="{{elementId}}-fill" style="height:100%; width:0%; background:{{fillCss}}; transition: width 0.6s ease;"></div>
                    </div>
                    <div id="{{elementId}}-label" style="margin-top:6px; {{textStyle}}"></div>
                </div>
                <script>
                    (function () {
                        var fillEl = document.getElementById("{{elementId}}-fill");
                        var labelEl = document.getElementById("{{elementId}}-label");
                        var metric = "{{metric}}";
                        var platform = "{{platform}}";
                        var target = {{target}};
                        var metricLabel = {{metricLabelJs}};

                        async function refresh() {
                            try {
                                var response = await fetch("/goal-data?metric=" + encodeURIComponent(metric) + "&platform=" + encodeURIComponent(platform));
                                var data = await response.json();
                                var current = data.Current || 0;
                                var percent = Math.max(0, Math.min(100, (current / target) * 100));

                                fillEl.style.width = percent + "%";
                                labelEl.textContent = current + " / " + target + " " + metricLabel;
                            } catch {
                            }
                        }

                        refresh();
                        setInterval(refresh, 5000);
                    })();
                </script>
                """;
            }

            default:
                return "";
        }
    }

    private static string BuildTextElementStyle(WadevoDesignerElementState element)
    {
        string fontFamily = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(element.FontFamily) ? "Segoe UI" : element.FontFamily);

        string fontWeight = element.FontBold ? "700" : "400";

        System.Drawing.Color color = System.Drawing.Color.FromArgb(element.FontColorArgb);
        string colorCss = $"rgb({color.R}, {color.G}, {color.B})";

        return
            $"font-family: '{fontFamily}', Segoe UI, Arial, sans-serif; " +
            $"font-size: {element.FontSize}px; " +
            $"font-weight: {fontWeight}; " +
            $"color: {colorCss};";
    }

    private static string GetElementRole(string elementName)
    {
        return elementName switch
        {
            "Song ID Label" => "title",
            "Artist" => "artist",
            "Song" => "song",
            "Album" => "album",
            "Release Date" => "releasedate",
            _ => ""
        };
    }

    private static string ResolveLiveText(
        WadevoDesignerElementState element,
        string liveArtist,
        string liveSong,
        string liveAlbum,
        string liveReleaseDate)
    {
        return element.Name switch
        {
            "Artist" => string.IsNullOrWhiteSpace(liveArtist) ? element.Text : liveArtist,
            "Song" => string.IsNullOrWhiteSpace(liveSong) ? element.Text : liveSong,
            "Album" => string.IsNullOrWhiteSpace(liveAlbum) ? element.Text : liveAlbum,
            "Release Date" => string.IsNullOrWhiteSpace(liveReleaseDate) ? element.Text : liveReleaseDate,
            _ => element.Text
        };
    }

    private static (string Artist, string Song) SplitCurrentSong(string song)
    {
        if (string.IsNullOrWhiteSpace(song))
        {
            return ("", "");
        }

        string[] parts = song.Split(" - ", 2, StringSplitOptions.TrimEntries);

        return parts.Length == 2 ? (parts[0], parts[1]) : ("", song);
    }

    private string CreateLegacyThemeCardOverlay()
    {
        OverlayDesignerService designerService = new();
        OverlayThemeModel theme = designerService.GetSelectedTheme();
        OverlaySettingsModel settings = designerService.GetPreviewSettings();

        string currentSong = WebUtility.HtmlEncode(_getCurrentSong());
        string title = WebUtility.HtmlEncode(settings.NowPlayingTitle);

        string artworkHtml = theme.ShowArtwork
            ? """<div class="artwork">♪</div>"""
            : "";

        string progressHtml = theme.ShowProgressBar
            ? """<div class="progress"><div class="progress-fill"></div></div>"""
            : "";

        string glowClass = theme.ShowGlow ? " glow" : "";
        int textXGap = theme.ShowArtwork ? 16 : 0;

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta http-equiv="refresh" content="2">
            <title>Song ID</title>
            <style>
                body {
                    margin: 0;
                    background: transparent;
                    overflow: hidden;
                    font-family: Segoe UI, Arial, sans-serif;
                }

                .overlay {
                    display: inline-flex;
                    align-items: center;
                    gap: {{textXGap}}px;
                    margin: 20px;
                    padding: {{theme.Padding}}px;
                    border-radius: {{theme.BorderRadius}}px;
                    color: {{theme.TextHex}};
                    background: {{theme.BackgroundHex}};
                    border: 2px solid {{theme.AccentHex}};
                    box-shadow: 0 18px 55px rgba(0, 0, 0, 0.42);
                }

                .overlay.glow {
                    box-shadow:
                        0 0 24px {{theme.AccentHex}},
                        0 18px 55px rgba(0, 0, 0, 0.42);
                }

                .artwork {
                    width: 54px;
                    height: 54px;
                    border-radius: 18px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    background: {{theme.AccentHex}};
                    color: {{theme.BackgroundHex}};
                    font-size: 28px;
                    font-weight: 900;
                    flex: 0 0 auto;
                }

                .label {
                    font-size: 13px;
                    letter-spacing: 0.18em;
                    text-transform: uppercase;
                    color: {{theme.AccentHex}};
                    font-weight: 800;
                    display: {{(theme.ShowLabel ? "block" : "none")}};
                }

                .song {
                    margin-top: 4px;
                    font-size: {{theme.FontSize}}px;
                    font-weight: 800;
                    white-space: nowrap;
                    color: {{theme.TextHex}};
                }

                .progress {
                    margin-top: 10px;
                    height: 5px;
                    width: 210px;
                    border-radius: 999px;
                    background: rgba(255, 255, 255, 0.18);
                    overflow: hidden;
                }

                .progress-fill {
                    height: 100%;
                    width: 100%;
                    border-radius: 999px;
                    background: {{theme.AccentHex}};
                }
            </style>
            <script>
                async function ping() {
                    try {
                        await fetch("/ping?route=/nowplaying");
                    } catch {
                    }
                }

                setInterval(ping, 2000);
                ping();
            </script>
        </head>
        <body>
            <div class="overlay{{glowClass}}">
                {{artworkHtml}}
                <div>
                    <div class="label">{{title}}</div>
                    <div class="song">{{currentSong}}</div>
                    {{progressHtml}}
                </div>
            </div>
        </body>
        </html>
        """;
    }

    private static string CreateCommandsOverlay()
    {
        OverlaySettingsModel settings = new OverlayDesignerService().GetPreviewSettings();

        OverlayAnimationModel animation = new()
        {
            DisplayMilliseconds = settings.CommandDurationMilliseconds
        };

        return CreateEventOverlayPage(
            "command",
            "/commands",
            "#00ff88",
            "Wadevo Command",
            "Ready.",
            animation);
    }

    private static string CreateAlertsOverlay()
    {
        OverlaySettingsModel settings = new OverlayDesignerService().GetPreviewSettings();

        OverlayAnimationModel animation = new()
        {
            DisplayMilliseconds = settings.AlertDurationMilliseconds
        };

        return CreateEventOverlayPage(
            "alert",
            "/alerts",
            "#ffbf57",
            "Alerts Overlay",
            "Ready for follows, raids, subs, tips, and custom alerts.",
            animation);
    }

    private static string CreateGifsOverlay()
    {
        return """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Wadevo GIFs</title>
            <style>
                body {
                    margin: 0;
                    background: transparent;
                    overflow: hidden;
                    font-family: Segoe UI, Arial, sans-serif;
                }

                #gif {
                    position: fixed;
                    left: 50%;
                    bottom: 30px;
                    transform: translateX(-50%) translateY(24px) scale(.94);
                    max-width: 480px;
                    max-height: 480px;
                    border-radius: 18px;
                    opacity: 0;
                    filter: blur(4px);
                    will-change: opacity, transform, filter;
                    transition: opacity 260ms cubic-bezier(.2,.9,.2,1),
                                transform 260ms cubic-bezier(.2,.9,.2,1),
                                filter 260ms ease;
                }

                #gif.visible {
                    opacity: 1;
                    transform: translateX(-50%) translateY(0) scale(1);
                    filter: blur(0);
                }

                #gif.hiding {
                    opacity: 0;
                    transform: translateX(-50%) translateY(-14px) scale(.98);
                    filter: blur(3px);
                    transition: opacity 220ms ease, transform 220ms ease, filter 220ms ease;
                }
            </style>
        </head>
        <body>
            <img id="gif" alt="">

            <script>
                let lastEventId = 0;
                const route = "/gifs";
                const gif = document.getElementById("gif");
                let hideTimer = null;

                function showGif(event) {
                    if (!event.GifPath) {
                        return;
                    }

                    clearTimeout(hideTimer);

                    gif.classList.remove("visible");
                    gif.classList.remove("hiding");

                    const newSrc = "/media?path=" + encodeURIComponent(event.GifPath) + "&cache=" + Date.now();

                    const revealNewGif = () => {
                        gif.classList.add("visible");

                        const durationMs = event.DurationMs && event.DurationMs > 0 ? event.DurationMs : 5000;

                        hideTimer = setTimeout(() => {
                            gif.classList.remove("visible");
                            gif.classList.add("hiding");
                        }, durationMs);
                    };

                    // Changing src alone doesn't clear what's currently displayed - the
                    // browser keeps showing the old image until the new one finishes loading
                    // in the background. Waiting for the load event (instead of revealing
                    // immediately) is what actually prevents the previous GIF from briefly
                    // showing through during the transition.
                    gif.onload = () => {
                        gif.onload = null;
                        revealNewGif();
                    };

                    gif.src = newSrc;
                }

                async function ping() {
                    try {
                        await fetch("/ping?route=" + encodeURIComponent(route));
                    } catch {
                    }
                }

                async function pollEvents() {
                    try {
                        const response = await fetch("/events?route=" + encodeURIComponent(route) + "&after=" + lastEventId);
                        const events = await response.json();

                        for (const event of events) {
                            lastEventId = Math.max(lastEventId, event.Id);

                            if (event.Type === "gif") {
                                showGif(event);
                            }
                        }
                    } catch {
                    }
                }

                setInterval(ping, 2000);
                setInterval(pollEvents, 800);
                ping();
                pollEvents();
            </script>
        </body>
        </html>
        """;
    }

    private static string CreateEventOverlayPage(
        string eventType,
        string route,
        string accentColor,
        string waitingTitle,
        string waitingText,
        OverlayAnimationModel animation)
    {
        OverlayAnimationService animationService = new();

        string animationCss = animationService.BuildCss(animation);
        string animationJavaScript = animationService.BuildJavaScript(animation);
        string fontFaceCss = BuildAllCustomFontFacesCss();
        string emoteMapJson = EmoteRenderHelper.BuildEmoteMapJson();
        string emoteScriptBlock = EmoteRenderHelper.EmoteScriptBlock();

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>{{waitingTitle}}</title>
            <style>
                {{fontFaceCss}}

                body {
                    margin: 0;
                    background: transparent;
                    overflow: hidden;
                    font-family: Segoe UI, Arial, sans-serif;
                    color: white;
                }

               .overlay {
            margin: 30px;
            min-width: 360px;
            padding: 24px 32px;
            display: inline-block;
            border-radius: 26px;

            background:
                linear-gradient(
                    135deg,
                    rgba(3, 20, 15, .96),
                    rgba(0, 6, 5, .94)
                );

            border: 2px solid {{accentColor}};

            box-shadow:
                0 0 18px {{accentColor}},
                inset 0 0 25px rgba(0,255,136,.12),
                0 20px 60px rgba(0,0,0,.8);
        }
                }

               #title {
                    color: {{accentColor}};
                    font-weight: 900;
                    font-size: 24px;
                    text-shadow: 0 0 14px {{accentColor}};
        }

                #message {
                    margin-top: 12px;
                    color: #f5fff9;
                    font-size: 18px;
                    font-weight: 600;
        }
                }

                #media, #mediaVideo {
                    display: none;
                    margin-top: 14px;
                    max-width: 280px;
                    max-height: 190px;
                    border-radius: 18px;
                    object-fit: contain;
                }

                /* When an event is just an image/GIF, drop the card chrome entirely -
                   no border, no glow, no title/message text, just the media itself. */
                .overlay.media-only {
                    margin: 30px;
                    padding: 0;
                    min-width: 0;
                    background: none;
                    border: none;
                    box-shadow: none;
                }

                .overlay.media-only #title,
                .overlay.media-only #message {
                    display: none;
                }

                .overlay.media-only #media,
                .overlay.media-only #mediaVideo {
                    margin-top: 0;
                    max-width: 520px;
                    max-height: 520px;
                }

                {{animationCss}}

                /* Named alert entrance variants - applied only to alert events, layered on
                   top of the existing fade/slide-up transition above. */
                .overlay.anim-pop.visible {
                    transform: translateY(0) scale(1);
                }

                .overlay.anim-pop {
                    transform: scale(.7);
                }

                .overlay.anim-slide {
                    transform: translateX(-60px);
                }

                .overlay.anim-slide.visible {
                    transform: translateX(0);
                }

                .overlay.anim-fade {
                    transform: none;
                }

                .overlay.anim-fade.visible {
                    transform: none;
                }
            </style>
        </head>
        <body>
            <div id="overlay" class="overlay">
                <div id="title">{{waitingTitle}}</div>
                <div id="message">{{waitingText}}</div>
                <img id="media" alt="">
                <video id="mediaVideo" playsinline></video>
            </div>

            <!-- Designed alerts (built in Overlay Designer) render here instead of the
                 fixed card above - a completely separate, absolutely-positioned canvas
                 matching whatever layout the alert profile links to. -->
            <div id="designedAlertContainer" style="position:fixed; inset:0; display:none;"></div>

            <script>
                let lastEventId = 0;
                const eventType = "{{eventType}}";
                const route = "{{route}}";
                const overlay = document.getElementById("overlay");
                const title = document.getElementById("title");
                const message = document.getElementById("message");
                const media = document.getElementById("media");
                const mediaVideo = document.getElementById("mediaVideo");

                // Alert/Command text can include viewer-typed chat content via the
                // {message} token, so this uses safe DOM-node construction rather than
                // baking HTML server-side - see EmoteRenderHelper for why.
                const EMOTE_MAP = {{emoteMapJson}};
                {{emoteScriptBlock}}

                {{animationJavaScript}}

                async function pollEvents() {
                    try {
                        const response = await fetch("/events?route=" + encodeURIComponent(route) + "&after=" + lastEventId);
                        const events = await response.json();

                        for (const event of events) {
                            lastEventId = Math.max(lastEventId, event.Id);

                            if (event.Type === eventType) {
                                showEvent(event);
                            }
                        }
                    } catch {
                    }
                }

                function isVideoPath(path) {
                    const lower = path.toLowerCase();
                    return lower.endsWith(".mp4") || lower.endsWith(".webm") ||
                        lower.endsWith(".mov") || lower.endsWith(".ogg");
                }

                function showEvent(event) {
                    if (event.Type === "alert" && event.ElementsJson) {
                        showDesignedAlert(event);
                        return;
                    }

                    const hasMedia = event.GifPath && event.GifPath.length > 0;
                    const isVideo = hasMedia && isVideoPath(event.GifPath);

                    overlay.classList.toggle("media-only", hasMedia);

                    wadevoSetTextWithEmotes(title, event.Title, EMOTE_MAP);
                    wadevoSetTextWithEmotes(message, event.Message, EMOTE_MAP);

                    applyAlertStyle(event);

                    if (hasMedia) {
                        const activeElement = isVideo ? mediaVideo : media;
                        const inactiveElement = isVideo ? media : mediaVideo;

                        inactiveElement.removeAttribute("src");
                        inactiveElement.style.display = "none";

                        activeElement.src = "/media?path=" + encodeURIComponent(event.GifPath) + "&cache=" + Date.now();
                        activeElement.style.display = "block";

                        if (isVideo) {
                            mediaVideo.currentTime = 0;
                            mediaVideo.play().catch(() => {});
                        }

                        if (event.Type !== "alert" && event.Width > 0 && event.Height > 0) {
                            activeElement.style.width = event.Width + "px";
                            activeElement.style.height = event.Height + "px";
                            activeElement.style.maxWidth = "none";
                            activeElement.style.maxHeight = "none";
                        } else {
                            activeElement.style.width = "";
                            activeElement.style.height = "";
                            activeElement.style.maxWidth = "";
                            activeElement.style.maxHeight = "";
                        }
                    } else {
                        media.removeAttribute("src");
                        media.style.display = "none";
                        mediaVideo.pause();
                        mediaVideo.removeAttribute("src");
                        mediaVideo.style.display = "none";
                    }

                    showOverlay();
                }

                let designedAlertHideTimer = null;
                const designedAlertContainer = document.getElementById("designedAlertContainer");

                function showDesignedAlert(event) {
                    if (designedAlertHideTimer) {
                        clearTimeout(designedAlertHideTimer);
                    }

                    designedAlertContainer.innerHTML = "";

                    let payload;
                    try {
                        payload = JSON.parse(event.ElementsJson);
                    } catch {
                        payload = { background: null, elements: [] };
                    }

                    const elements = payload.elements || [];
                    const background = payload.background || null;

                    if (background && background.ImageUrl) {
                        const bgImg = document.createElement("img");
                        bgImg.src = background.ImageUrl;
                        bgImg.style.position = "absolute";
                        bgImg.style.left = background.X + "px";
                        bgImg.style.top = background.Y + "px";
                        bgImg.style.width = background.Width + "px";
                        bgImg.style.height = background.Height + "px";
                        bgImg.style.objectFit = "fill";
                        bgImg.style.opacity = (background.BackgroundOpacityPercent ?? 100) / 100;
                        bgImg.style.borderRadius = background.BackgroundRoundedCorners ? "16px" : "0";

                        designedAlertContainer.appendChild(bgImg);
                    }

                    elements.forEach(function (el) {
                        const wrapper = document.createElement("div");
                        wrapper.style.position = "absolute";
                        wrapper.style.left = el.X + "px";
                        wrapper.style.top = el.Y + "px";
                        wrapper.style.width = el.Width + "px";
                        wrapper.style.height = el.Height + "px";

                        if (el.Kind === "Text") {
                            wrapper.style.fontFamily = "'" + el.FontFamily + "', Segoe UI, Arial, sans-serif";
                            wrapper.style.fontSize = el.FontSize + "px";
                            wrapper.style.fontWeight = el.FontBold ? "700" : "400";
                            wrapper.style.color = el.TextColor;
                            // Text here was already token-substituted server-side ({username},
                            // {message}, etc.) - this only needs to additionally handle emotes,
                            // via the same safe DOM-construction helper used everywhere else,
                            // since the text can still contain viewer-typed chat content.
                            wadevoSetTextWithEmotes(wrapper, el.Text, EMOTE_MAP);
                        } else if (el.Kind === "Image") {
                            const mediaEl = document.createElement(el.IsVideo ? "video" : "img");
                            mediaEl.src = el.ImageUrl;
                            mediaEl.style.width = "100%";
                            mediaEl.style.height = "100%";
                            mediaEl.style.objectFit = "contain";

                            if (el.IsVideo) {
                                mediaEl.autoplay = true;
                                mediaEl.muted = true;
                                mediaEl.loop = true;
                                mediaEl.playsInline = true;
                            }

                            wrapper.appendChild(mediaEl);
                        }

                        designedAlertContainer.appendChild(wrapper);
                    });

                    designedAlertContainer.style.display = "block";

                    const durationMs = event.DurationMs > 0 ? event.DurationMs : 4200;

                    designedAlertHideTimer = setTimeout(function () {
                        designedAlertContainer.style.display = "none";
                        designedAlertContainer.innerHTML = "";
                    }, durationMs);
                }

                // Applies a custom alert's own colors, font, background image, size, and
                // animation. Only alert events carry this data - Commands events leave these
                // fields empty, so clearing back to CSS defaults (rather than leaving
                // whatever the previous alert set) is what keeps that route's own styling
                // completely unaffected.
                function applyAlertStyle(event) {
                    if (event.Type !== "alert") {
                        overlay.style.cssText = "";
                        title.style.cssText = "";
                        message.style.cssText = "";
                        return;
                    }

                    overlay.style.borderColor = event.AccentHex || "";
                    overlay.style.boxShadow = event.AccentHex
                        ? "0 0 18px " + event.AccentHex + ", 0 20px 60px rgba(0,0,0,.8)"
                        : "";
                    overlay.style.background = event.BackgroundHex || "";
                    overlay.style.fontFamily = event.FontFamily ? "'" + event.FontFamily + "', Segoe UI, Arial, sans-serif" : "";

                    if (event.BackgroundImagePath) {
                        overlay.style.backgroundImage =
                            "url('/media?path=" + encodeURIComponent(event.BackgroundImagePath) + "')";
                        overlay.style.backgroundSize = "cover";
                        overlay.style.backgroundPosition = "center";
                    } else {
                        overlay.style.backgroundImage = "";
                    }

                    if (event.Width > 0) {
                        overlay.style.minWidth = event.Width + "px";
                    }

                    if (event.Height > 0) {
                        overlay.style.minHeight = event.Height + "px";
                    }

                    title.style.color = event.AccentHex || "";
                    title.style.textShadow = event.AccentHex ? "0 0 14px " + event.AccentHex : "";
                    message.style.color = event.TextHex || "";

                    overlay.classList.remove("anim-pop", "anim-slide", "anim-fade");

                    if (event.AnimationName === "Slide") {
                        overlay.classList.add("anim-slide");
                    } else if (event.AnimationName === "Fade") {
                        overlay.classList.add("anim-fade");
                    } else if (event.AnimationName === "Pop") {
                        overlay.classList.add("anim-pop");
                    }
                }

                setInterval(pollEvents, 1000);
                pollEvents();
            </script>
        </body>
        </html>
        """;
    }

    private static string CreateDebugPage()
    {
        IReadOnlyDictionary<string, bool> statuses = GetRouteStatuses();
        StringBuilder rows = new();

        foreach (KeyValuePair<string, bool> route in statuses.OrderBy(x => x.Key))
        {
            string safeRoute = WebUtility.HtmlEncode(route.Key);
            string status = route.Value ? "Active" : "Idle";

            rows.AppendLine($"""
            <tr>
                <td>{safeRoute}</td>
                <td>{status}</td>
            </tr>
            """);
        }

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta http-equiv="refresh" content="2">
            <title>Overlay Engine Debug</title>
        </head>
        <body style="margin:0;padding:32px;background:#08111f;color:#e9f7ff;font-family:Segoe UI,Arial,sans-serif;">
            <h1 style="color:#45d9ff;">🚀 Overlay Engine Debug</h1>
            <p>Live route status.</p>
            <table style="width:100%;border-collapse:collapse;margin-top:24px;background:rgba(255,255,255,.06);">
                <thead>
                    <tr>
                        <th style="text-align:left;padding:14px;color:#45d9ff;">Route</th>
                        <th style="text-align:left;padding:14px;color:#45d9ff;">Status</th>
                    </tr>
                </thead>
                <tbody>
                    {{rows}}
                </tbody>
            </table>
        </body>
        </html>
        """;
    }

    private static string CreateNotFoundPage(string route)
    {
        string safeRoute = WebUtility.HtmlEncode(route);

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <title>Overlay Route Not Found</title>
        </head>
        <body style="margin:0;background:#08111f;color:#e9f7ff;font-family:Segoe UI,Arial,sans-serif;">
            <div style="padding:36px;">
                <h1 style="color:#ff6b6b;">Route Not Found</h1>
                <p>{{safeRoute}}</p>
            </div>
        </body>
        </html>
        """;
    }

    private sealed record OverlayEvent(
        int Id,
        string Type,
        string Title,
        string Message,
        string GifPath,
        int DurationMs,
        int Width,
        int Height,
        DateTime CreatedUtc,
        string AccentHex = "",
        string BackgroundHex = "",
        string TextHex = "",
        string FontFamily = "",
        string AnimationName = "",
        string BackgroundImagePath = "",
        // When set, this is a fully Designer-built alert - a JSON array of already
        // token-substituted widget data (see BuildAlertElementsJson) that the client
        // renders directly instead of using the fixed title/message/media template below.
        // Empty for every other event type (Commands' simple alert popup, GIF triggers,
        // etc.), which keeps using the original template unchanged.
        string ElementsJson = "");

    private sealed record ChatOverlayMessage(
        int Id,
        string Platform,
        string PlatformGlyph,
        string PlatformColorHex,
        string Username,
        string Message,
        DateTime CreatedUtc);
}