namespace Wadevo.Services.Twitch;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// Implements Twitch's EventSub-over-WebSocket flow: connect to Twitch's WebSocket
// server, receive a session id in the "session_welcome" message, then use that session
// id to register Helix EventSub subscriptions (chat messages, follows, subs, raids,
// cheers, stream online/offline). Twitch then pushes matching events down the same
// socket as "notification" messages - no public webhook server required, which is what
// makes this workable from a desktop app running on the streamer's own machine.
public sealed class TwitchEventSubWebSocketClient : ITwitchEventClient, IAsyncDisposable
{
    private const string DefaultWebSocketUrl = "wss://eventsub.wss.twitch.tv/ws";
    private const string HelixSubscriptionsEndpoint = "https://api.twitch.tv/helix/eventsub/subscriptions";

    private static readonly (string Type, string Version)[] SubscriptionTypes =
    [
        ("channel.chat.message", "1"),
        ("channel.follow", "2"),
        ("channel.subscribe", "1"),
        ("channel.subscription.gift", "1"),
        ("channel.cheer", "1"),
        ("channel.raid", "1"),
        ("stream.online", "1"),
        ("stream.offline", "1")
    ];

    private readonly HttpClient _httpClient = new();

    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveLoopCancellation;
    private Task? _receiveLoopTask;

    private string _accessToken = "";
    private string _clientId = "";
    private string _broadcasterUserId = "";

    public bool IsConnected { get; private set; }

    public event EventHandler<TwitchEvent>? EventReceived;

    public async Task ConnectAsync(
        string accessToken,
        string clientId,
        string broadcasterUserId,
        CancellationToken cancellationToken = default)
    {
        _accessToken = accessToken;
        _clientId = clientId;
        _broadcasterUserId = broadcasterUserId;

        await ConnectSocketAsync(DefaultWebSocketUrl, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;

        _receiveLoopCancellation?.Cancel();

        if (_socket is { State: WebSocketState.Open })
        {
            try
            {
                await _socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Disconnecting",
                    cancellationToken);
            }
            catch
            {
                // Best-effort close - the socket may already be in a bad state.
            }
        }

        if (_receiveLoopTask is not null)
        {
            try
            {
                await _receiveLoopTask;
            }
            catch
            {
                // Loop exit is expected once cancelled.
            }
        }

        _socket?.Dispose();
        _socket = null;

        RaiseEvent(TwitchEventType.Disconnected, "Disconnected from Twitch.");
    }

    private async Task ConnectSocketAsync(string url, CancellationToken cancellationToken)
    {
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(new Uri(url), cancellationToken);

        _receiveLoopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveLoopTask = ReceiveLoopAsync(_receiveLoopCancellation.Token);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[16 * 1024];

        try
        {
            while (_socket is { State: WebSocketState.Open } && !cancellationToken.IsCancellationRequested)
            {
                using MemoryStream messageStream = new();
                WebSocketReceiveResult result;

                do
                {
                    result = await _socket.ReceiveAsync(buffer, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    messageStream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                string json = Encoding.UTF8.GetString(messageStream.ToArray());
                await HandleMessageAsync(json, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect.
        }
        catch (Exception ex)
        {
            RaiseEvent(TwitchEventType.Error, ex.Message);
        }
    }

    private async Task HandleMessageAsync(string json, CancellationToken cancellationToken)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        string messageType = root
            .GetProperty("metadata")
            .GetProperty("message_type")
            .GetString() ?? "";

        switch (messageType)
        {
            case "session_welcome":
                await HandleWelcomeAsync(root, cancellationToken);
                break;

            case "notification":
                HandleNotification(root, json);
                break;

            case "session_reconnect":
                await HandleReconnectAsync(root, cancellationToken);
                break;

            case "revocation":
                RaiseEvent(TwitchEventType.Error, "A Twitch EventSub subscription was revoked.");
                break;

            // "session_keepalive" needs no action - receiving it at all confirms the
            // connection is still alive.
        }
    }

    private async Task HandleWelcomeAsync(JsonElement root, CancellationToken cancellationToken)
    {
        string sessionId = root
            .GetProperty("payload")
            .GetProperty("session")
            .GetProperty("id")
            .GetString() ?? "";

        await SubscribeToAllAsync(sessionId, cancellationToken);

        IsConnected = true;
        RaiseEvent(TwitchEventType.Connected, "Connected to Twitch.");
    }

    private async Task HandleReconnectAsync(JsonElement root, CancellationToken cancellationToken)
    {
        string? reconnectUrl = root
            .GetProperty("payload")
            .GetProperty("session")
            .GetProperty("reconnect_url")
            .GetString();

        // Twitch keeps the old connection alive briefly, but the simplest reliable
        // approach for a desktop client is to just drop and reconnect fresh - the new
        // session_welcome re-registers every subscription anyway.
        if (_socket is { State: WebSocketState.Open })
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", cancellationToken);
            }
            catch
            {
                // Ignore - proceeding to reconnect regardless.
            }
        }

        await ConnectSocketAsync(reconnectUrl ?? DefaultWebSocketUrl, cancellationToken);
    }

    private async Task SubscribeToAllAsync(string sessionId, CancellationToken cancellationToken)
    {
        foreach ((string type, string version) in SubscriptionTypes)
        {
            try
            {
                await SubscribeAsync(type, version, sessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                // One subscription type failing (e.g. missing scope) shouldn't stop the
                // others from being registered.
                RaiseEvent(TwitchEventType.Error, $"Failed to subscribe to {type}: {ex.Message}");
            }
        }
    }

    private async Task SubscribeAsync(
        string type,
        string version,
        string sessionId,
        CancellationToken cancellationToken)
    {
        object condition = type switch
        {
            "channel.chat.message" => new { broadcaster_user_id = _broadcasterUserId, user_id = _broadcasterUserId },
            "channel.follow" => new { broadcaster_user_id = _broadcasterUserId, moderator_user_id = _broadcasterUserId },
            "channel.raid" => new { to_broadcaster_user_id = _broadcasterUserId },
            _ => new { broadcaster_user_id = _broadcasterUserId }
        };

        var body = new
        {
            type,
            version,
            condition,
            transport = new { method = "websocket", session_id = sessionId }
        };

        using HttpRequestMessage request = new(HttpMethod.Post, HelixSubscriptionsEndpoint)
        {
            Content = JsonContent.Create(body)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.Add("Client-Id", _clientId);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"{(int)response.StatusCode}: {error}");
        }
    }

    private void HandleNotification(JsonElement root, string rawJson)
    {
        string subscriptionType = root
            .GetProperty("payload")
            .GetProperty("subscription")
            .GetProperty("type")
            .GetString() ?? "";

        JsonElement eventElement = root.GetProperty("payload").GetProperty("event");

        TwitchEvent? twitchEvent = subscriptionType switch
        {
            "channel.chat.message" => ParseChatMessage(eventElement, rawJson),
            "channel.follow" => ParseSimpleUserEvent(eventElement, TwitchEventType.Follow, rawJson),
            "channel.subscribe" => ParseSimpleUserEvent(eventElement, TwitchEventType.Subscribe, rawJson),
            "channel.subscription.gift" => ParseSimpleUserEvent(eventElement, TwitchEventType.GiftSub, rawJson),
            "channel.cheer" => ParseCheer(eventElement, rawJson),
            "channel.raid" => ParseRaid(eventElement, rawJson),
            "stream.online" => ParseStreamState(TwitchEventType.StreamOnline, rawJson),
            "stream.offline" => ParseStreamState(TwitchEventType.StreamOffline, rawJson),
            _ => null
        };

        if (twitchEvent is not null)
        {
            EventReceived?.Invoke(this, twitchEvent);
        }
    }

    private static TwitchEvent ParseChatMessage(JsonElement e, string rawJson)
    {
        HashSet<string> badgeSetIds = new();

        if (e.TryGetProperty("badges", out JsonElement badges) && badges.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement badge in badges.EnumerateArray())
            {
                string? setId = GetString(badge, "set_id");

                if (setId is not null)
                {
                    badgeSetIds.Add(setId);
                }
            }
        }

        return new TwitchEvent
        {
            EventType = TwitchEventType.ChatMessage,
            UserId = GetString(e, "chatter_user_id"),
            Username = GetString(e, "chatter_user_name"),
            MessageId = GetString(e, "message_id"),
            Message = e.TryGetProperty("message", out JsonElement msg) ? GetString(msg, "text") : null,
            IsSubscriber = badgeSetIds.Contains("subscriber"),
            IsModerator = badgeSetIds.Contains("moderator"),
            IsBroadcaster = badgeSetIds.Contains("broadcaster"),
            RawJson = rawJson
        };
    }

    private static TwitchEvent ParseSimpleUserEvent(JsonElement e, TwitchEventType type, string rawJson)
    {
        return new TwitchEvent
        {
            EventType = type,
            UserId = GetString(e, "user_id"),
            Username = GetString(e, "user_name"),
            RawJson = rawJson
        };
    }

    private static TwitchEvent ParseCheer(JsonElement e, string rawJson)
    {
        return new TwitchEvent
        {
            EventType = TwitchEventType.Cheer,
            UserId = GetString(e, "user_id"),
            Username = GetString(e, "user_name"),
            Message = GetString(e, "message"),
            BitsCheered = e.TryGetProperty("bits", out JsonElement bits) ? bits.GetInt32() : null,
            RawJson = rawJson
        };
    }

    private static TwitchEvent ParseRaid(JsonElement e, string rawJson)
    {
        return new TwitchEvent
        {
            EventType = TwitchEventType.Raid,
            UserId = GetString(e, "from_broadcaster_user_id"),
            Username = GetString(e, "from_broadcaster_user_name"),
            ViewerCount = e.TryGetProperty("viewers", out JsonElement viewers) ? viewers.GetInt32() : null,
            RawJson = rawJson
        };
    }

    private static TwitchEvent ParseStreamState(TwitchEventType type, string rawJson)
    {
        return new TwitchEvent
        {
            EventType = type,
            RawJson = rawJson
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value) ? value.GetString() : null;
    }

    private void RaiseEvent(TwitchEventType type, string message)
    {
        EventReceived?.Invoke(this, new TwitchEvent { EventType = type, Message = message });
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _httpClient.Dispose();
    }
}
