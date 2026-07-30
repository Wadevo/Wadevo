namespace Wadevo.Services.Blaze;

public sealed class BlazeEventCommandContext
{
    public string TriggerName { get; init; } = string.Empty;

    public BlazeEventType EventType { get; init; } = BlazeEventType.Unknown;

    public string Username { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public IReadOnlyDictionary<string, object?> Data { get; init; }
        = new Dictionary<string, object?>();
}