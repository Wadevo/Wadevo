namespace Wadevo.Services.Blaze;

public static class BlazeEventCommandTriggerMapper
{
    public static string GetTriggerName(BlazeEvent blazeEvent)
    {
        ArgumentNullException.ThrowIfNull(blazeEvent);

        return blazeEvent.EventType switch
        {
            BlazeEventType.ChatMessage => "blaze.chat",
            BlazeEventType.Follow => "blaze.follow",
            BlazeEventType.Raid => "blaze.raid",
            BlazeEventType.Subscribe => "blaze.subscribe",
            BlazeEventType.GiftSub => "blaze.gift",
            BlazeEventType.Vote => "blaze.vote",
            BlazeEventType.VipAdd => "blaze.vip",
            BlazeEventType.StreamOnline => "blaze.online",
            BlazeEventType.StreamOffline => "blaze.offline",
            BlazeEventType.Connected => "blaze.connected",
            BlazeEventType.Disconnected => "blaze.disconnected",
            BlazeEventType.Error => "blaze.error",
            _ => "blaze.unknown"
        };
    }
}