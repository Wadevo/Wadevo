namespace Wadevo.Services.Twitch;

public interface ITwitchEventClient
{
    bool IsConnected { get; }

    event EventHandler<TwitchEvent>? EventReceived;

    Task ConnectAsync(
        string accessToken,
        string clientId,
        string broadcasterUserId,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(
        CancellationToken cancellationToken = default);
}
