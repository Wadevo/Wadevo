namespace Wadevo.Services.Blaze;

using System.Text.Json;
using SocketIOClient;

public sealed class BlazeSocketIoEventClient : IBlazeEventClient
{
    private SocketIO? _socket;
    private string? _sessionId;

    public bool IsConnected => _socket?.Connected == true;

    public event EventHandler<BlazeEvent>? EventReceived;
    public event EventHandler<string>? SessionReady;

    public async Task ConnectAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Blaze access token is required.", nameof(accessToken));

        await DisconnectAsync(cancellationToken);
        _sessionId = null;

        _socket = new SocketIO(
            new Uri("https://blaze.stream"),
            new SocketIOOptions
            {
                Path = "/ws",
                Reconnection = true,
                ExtraHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {accessToken}"
                }
            });

        _socket.OnConnected += async (_, _) =>
        {
            RaiseEvent(new BlazeEvent
            {
                EventType = BlazeEventType.Connected,
                Message = "Connected to Blaze Socket.IO"
            });

            await Task.CompletedTask;
        };

        _socket.OnDisconnected += async (_, reason) =>
        {
            RaiseEvent(new BlazeEvent
            {
                EventType = BlazeEventType.Disconnected,
                Message = $"Disconnected from Blaze Socket.IO: {reason}"
            });

            await Task.CompletedTask;
        };

        _socket.OnError += async (_, error) =>
        {
            RaiseEvent(new BlazeEvent
            {
                EventType = BlazeEventType.Error,
                Message = $"Blaze Socket.IO error: {error}"
            });

            await Task.CompletedTask;
        };

        _socket.On("eventsub", async context =>
        {
            HandleEventSub(context);
            await Task.CompletedTask;
        });

        await _socket.ConnectAsync();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_socket == null)
            return;

        try
        {
            if (_socket.Connected)
                await _socket.DisconnectAsync();
        }
        finally
        {
            _socket.Dispose();
            _socket = null;
            _sessionId = null;
        }
    }

    private void HandleEventSub(IEventContext context)
    {
        string rawJson = GetRawJson(context);

        string messageType = TryReadString(rawJson, "metadata", "messageType") ?? "unknown";
        string subscriptionType = TryReadString(rawJson, "metadata", "subscriptionType") ?? "unknown";
        string? sessionId = TryReadString(rawJson, "payload", "sessionId");

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _sessionId = sessionId;
            SessionReady?.Invoke(this, sessionId);
        }

        BlazeEventType eventType = GetEventType(messageType, subscriptionType);
        BlazePayloadUser user = ExtractUser(rawJson);

        string? chatMessage = eventType == BlazeEventType.ChatMessage
            ? ExtractChatMessage(rawJson)
            : null;

        string? chatMessageId = eventType == BlazeEventType.ChatMessage
            ? TryReadString(rawJson, "payload", "messageId")
            : null;

        string? giftCount = eventType == BlazeEventType.GiftSub
            ? TryReadString(rawJson, "payload", "giftCount")
            : null;

        string? voteAmount = eventType == BlazeEventType.Vote
            ? TryReadString(rawJson, "payload", "amount")
            : null;

        string? message = eventType switch
        {
            BlazeEventType.ChatMessage =>
                chatMessage,

            BlazeEventType.Follow =>
                $"{user.DisplayName ?? user.Username ?? "Unknown User"} followed.",

            BlazeEventType.Raid =>
                $"{user.DisplayName ?? user.Username ?? "Unknown User"} raided.",

            BlazeEventType.Subscribe =>
                $"{user.DisplayName ?? user.Username ?? "Unknown User"} subscribed.",

            BlazeEventType.GiftSub =>
                $"{user.DisplayName ?? user.Username ?? "Unknown User"} gifted {giftCount ?? "some"} subs.",

            BlazeEventType.Vote =>
                $"{user.DisplayName ?? user.Username ?? "Unknown User"} voted {voteAmount ?? ""}.",

            BlazeEventType.VipAdd =>
                $"{user.DisplayName ?? user.Username ?? "Unknown User"} became a VIP.",

            BlazeEventType.StreamOnline =>
                "Stream went live.",

            BlazeEventType.StreamOffline =>
                "Stream went offline.",

            BlazeEventType.Connected when !string.IsNullOrWhiteSpace(sessionId) =>
                $"Blaze session ready: {sessionId}",

            _ => rawJson
        };

        RaiseEvent(new BlazeEvent
        {
            EventType = eventType,
            UserId = user.Id,
            Username = user.DisplayName ?? user.Username,
            MessageId = chatMessageId,
            Message = message,
            IsSubscriber = user.IsSubscriber,
            IsFollower = user.IsFollower,
            IsOwner = user.IsOwner,
            IsModerator = user.Roles.Contains("moderator", StringComparer.OrdinalIgnoreCase),
            RawJson = rawJson,
            Data = new Dictionary<string, object?>
            {
                ["source"] = "eventsub",
                ["messageType"] = messageType,
                ["subscriptionType"] = subscriptionType,
                ["sessionId"] = sessionId,
                ["userId"] = user.Id,
                ["username"] = user.Username,
                ["displayName"] = user.DisplayName,
                ["avatarUrl"] = user.AvatarUrl,
                ["chatMessage"] = chatMessage,
                ["giftCount"] = giftCount,
                ["voteAmount"] = voteAmount,
                ["raw"] = rawJson
            }
        });
    }

    private static BlazeEventType GetEventType(string messageType, string subscriptionType)
    {
        if (messageType == "session_welcome")
            return BlazeEventType.Connected;

        if (messageType != "notification")
            return BlazeEventType.Unknown;

        return subscriptionType switch
        {
            "channel.follow" => BlazeEventType.Follow,
            "channel.chat.message" => BlazeEventType.ChatMessage,
            "channel.chat_message" => BlazeEventType.ChatMessage,
            "chat.message" => BlazeEventType.ChatMessage,
            "channel.raid" => BlazeEventType.Raid,
            "channel.subscribe" => BlazeEventType.Subscribe,
            "channel.subscription.gift" => BlazeEventType.GiftSub,
            "channel.vote" => BlazeEventType.Vote,
            "channel.vip.add" => BlazeEventType.VipAdd,
            "stream.online" => BlazeEventType.StreamOnline,
            "stream.offline" => BlazeEventType.StreamOffline,
            _ => BlazeEventType.Unknown
        };
    }

    private static string? ExtractChatMessage(string rawJson)
    {
        return TryReadString(rawJson, "payload", "message")
            ?? TryReadString(rawJson, "payload", "text")
            ?? TryReadString(rawJson, "payload", "content")
            ?? TryReadNestedString(rawJson, "payload", "message", "text")
            ?? TryReadNestedString(rawJson, "payload", "message", "body")
            ?? TryReadNestedString(rawJson, "payload", "message", "content")
            ?? TryReadNestedString(rawJson, "payload", "chatMessage", "text")
            ?? TryReadNestedString(rawJson, "payload", "chatMessage", "body")
            ?? TryReadNestedString(rawJson, "payload", "chatMessage", "content");
    }

    private static BlazePayloadUser ExtractUser(string rawJson)
    {
        string[] userObjectNames =
        [
            "sender",
            "chatter",
            "user",
            "follower",
            "subscriber",
            "raider",
            "moderator",
            "targetUser",
            "vip",
            "og",
            "bannedUser",
            "unbannedUser",
            "voter"
        ];

        try
        {
            using JsonDocument document = JsonDocument.Parse(rawJson);

            if (!document.RootElement.TryGetProperty("payload", out JsonElement payload))
                return new BlazePayloadUser();

            foreach (string userObjectName in userObjectNames)
            {
                if (!payload.TryGetProperty(userObjectName, out JsonElement userElement))
                    continue;

                return new BlazePayloadUser
                {
                    Id = ReadString(userElement, "id"),
                    Username = ReadString(userElement, "username"),
                    DisplayName = ReadString(userElement, "displayName"),
                    AvatarUrl = ReadString(userElement, "avatarUrl"),
                    Roles = ReadStringArray(userElement, "roles"),
                    IsSubscriber = ReadBool(userElement, "isSubscriber"),
                    IsFollower = ReadBool(userElement, "isFollower"),
                    IsOwner = ReadBool(userElement, "isOwner")
                };
            }
        }
        catch
        {
            return new BlazePayloadUser();
        }

        return new BlazePayloadUser();
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return null;

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
            return false;

        return value.ValueKind == JsonValueKind.True;
    }

    private static List<string> ReadStringArray(JsonElement element, string propertyName)
    {
        List<string> results = new();

        if (!element.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                string? text = item.GetString();

                if (text is not null)
                {
                    results.Add(text);
                }
            }
        }

        return results;
    }

    private static string GetRawJson(IEventContext context)
    {
        try
        {
            JsonElement element = context.GetValue<JsonElement>(0);
            return element.GetRawText();
        }
        catch
        {
            return context.ToString() ?? "";
        }
    }

    private static string? TryReadString(string rawJson, string parentName, string childName)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(rawJson);

            if (!document.RootElement.TryGetProperty(parentName, out JsonElement parent))
                return null;

            if (!parent.TryGetProperty(childName, out JsonElement child))
                return null;

            return child.ValueKind == JsonValueKind.String
                ? child.GetString()
                : child.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadNestedString(
        string rawJson,
        string parentName,
        string objectName,
        string childName)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(rawJson);

            if (!document.RootElement.TryGetProperty(parentName, out JsonElement parent))
                return null;

            if (!parent.TryGetProperty(objectName, out JsonElement nestedObject))
                return null;

            if (!nestedObject.TryGetProperty(childName, out JsonElement child))
                return null;

            return child.ValueKind == JsonValueKind.String
                ? child.GetString()
                : child.ToString();
        }
        catch
        {
            return null;
        }
    }

    private void RaiseEvent(BlazeEvent blazeEvent)
    {
        EventReceived?.Invoke(this, blazeEvent);
    }

    private sealed class BlazePayloadUser
    {
        public string? Id { get; init; }
        public string? Username { get; init; }
        public string? DisplayName { get; init; }
        public string? AvatarUrl { get; init; }
        public List<string> Roles { get; init; } = new();
        public bool IsSubscriber { get; init; }
        public bool IsFollower { get; init; }
        public bool IsOwner { get; init; }
    }
}