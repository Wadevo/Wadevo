namespace Wadevo.Services.Blaze;

public sealed class BlazeEventLogEntry
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public BlazeEventType EventType { get; init; } = BlazeEventType.Unknown;

    public string Message { get; init; } = string.Empty;

    public string DisplayText => $"[{TimestampUtc:HH:mm:ss}] {Message}";
}