namespace Wadevo.Services.Blaze;

public static class BlazeEventCommandContextFactory
{
    public static BlazeEventCommandContext Create(BlazeEvent blazeEvent)
    {
        ArgumentNullException.ThrowIfNull(blazeEvent);

        return new BlazeEventCommandContext
        {
            TriggerName = BlazeEventCommandTriggerMapper.GetTriggerName(blazeEvent),
            EventType = blazeEvent.EventType,
            Username = blazeEvent.Username ?? string.Empty,
            Message = blazeEvent.Message ?? string.Empty,
            TimestampUtc = blazeEvent.TimestampUtc,
            Data = blazeEvent.Data
        };
    }
}