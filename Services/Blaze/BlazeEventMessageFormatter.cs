namespace Wadevo.Services.Blaze;

public static class BlazeEventMessageFormatter
{
    public static string Format(BlazeEvent blazeEvent)
    {
        ArgumentNullException.ThrowIfNull(blazeEvent);

        string username = string.IsNullOrWhiteSpace(blazeEvent.Username)
            ? "Unknown User"
            : blazeEvent.Username;

        return blazeEvent.EventType switch
        {
            BlazeEventType.Connected => blazeEvent.Message ?? "Blaze connected.",
            BlazeEventType.Disconnected => blazeEvent.Message ?? "Blaze disconnected.",
            BlazeEventType.ChatMessage => $"{username}: {blazeEvent.Message}",
            BlazeEventType.Follow => $"{username} followed.",
            BlazeEventType.Raid => FormatRaid(blazeEvent, username),
            BlazeEventType.Error => blazeEvent.Message ?? "Blaze error.",
            _ => blazeEvent.Message ?? "Unknown Blaze event."
        };
    }

    private static string FormatRaid(BlazeEvent blazeEvent, string username)
    {
        if (blazeEvent.Data.TryGetValue("viewerCount", out object? viewerCount) &&
            viewerCount is not null)
        {
            return $"{username} raided with {viewerCount} viewers.";
        }

        return $"{username} raided the channel.";
    }
}