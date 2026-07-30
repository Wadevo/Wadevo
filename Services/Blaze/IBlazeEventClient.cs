namespace Wadevo.Services.Blaze;

public interface IBlazeEventClient
{
    bool IsConnected { get; }

    event EventHandler<BlazeEvent>? EventReceived;

    Task ConnectAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(
        CancellationToken cancellationToken = default);
}