namespace Wadevo.Services.Blaze;

public sealed class BlazeEventDispatcher
{
    public event EventHandler<BlazeEvent>? EventReceived;

    public void Dispatch(BlazeEvent blazeEvent)
    {
        ArgumentNullException.ThrowIfNull(blazeEvent);

        EventReceived?.Invoke(this, blazeEvent);
    }
}