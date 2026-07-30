namespace Wadevo.Services.Twitch;

public sealed class TwitchEvent
{
    public TwitchEventType EventType { get; init; } = TwitchEventType.Unknown;

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public string? UserId { get; init; }

    public string? Username { get; init; }

    public string? MessageId { get; init; }

    public string? Message { get; init; }

    public bool IsSubscriber { get; init; }

    public bool IsModerator { get; init; }

    public bool IsBroadcaster { get; init; }

    public int? BitsCheered { get; init; }

    public int? ViewerCount { get; init; }

    public string? RawJson { get; init; }

    public IReadOnlyDictionary<string, object?> Data { get; init; }
        = new Dictionary<string, object?>();
}
