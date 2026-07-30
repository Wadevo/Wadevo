namespace Wadevo.Services.Blaze;

public sealed class BlazeEvent
{
    public BlazeEventType EventType { get; init; } = BlazeEventType.Unknown;

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public string? UserId { get; init; }

    public string? Username { get; init; }

    public string? MessageId { get; init; }

    public string? Message { get; init; }

    public bool IsSubscriber { get; init; }

    public bool IsFollower { get; init; }

    public bool IsOwner { get; init; }

    public bool IsModerator { get; init; }

    public string? RawJson { get; init; }

    public IReadOnlyDictionary<string, object?> Data { get; init; }
        = new Dictionary<string, object?>();
}