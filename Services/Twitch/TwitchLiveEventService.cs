namespace Wadevo.Services.Twitch;

using Wadevo.Services;

// Owns the actual live Twitch connection so it keeps running in the background no matter
// which page of Wadevo is open - previously TwitchConnectionPopupForm connected directly,
// which meant events stopped the moment the popup was closed. This mirrors the role
// BlazeLiveEventService plays for Blaze: a single shared instance that auto-starts once
// authenticated and stays alive for the life of the app.
public sealed class TwitchLiveEventService
{
    private static readonly Lazy<TwitchLiveEventService> LazyShared = new(() => new TwitchLiveEventService());

    public static TwitchLiveEventService Shared => LazyShared.Value;

    public ITwitchEventClient EventClient { get; } = new TwitchEventSubWebSocketClient();

    public TwitchEventExecutionService ExecutionService { get; } = new();

    public bool IsListening => EventClient.IsConnected;

    public event EventHandler<TwitchEvent>? EventReceived;

    public event EventHandler<string>? LogMessage;

    public event EventHandler? StatusChanged;

    private TwitchLiveEventService()
    {
        EventClient.EventReceived += OnEventReceived;

        TwitchAuthenticationService.Shared.ConnectionStateChanged += (_, _) => AutoStartIfAuthenticated();

        // Covers the case where a saved login is already valid the moment the app starts,
        // so the person doesn't have to visit the Twitch page at all for events to flow.
        AutoStartIfAuthenticated();
    }

    private string? _connectedWithAccessToken;

    public async Task StartAsync()
    {
        if (!TwitchAuthenticationService.Shared.IsAuthenticated || IsListening)
        {
            return;
        }

        TwitchConnectionState connection = TwitchAuthenticationService.Shared.Connection;

        await EventClient.ConnectAsync(
            connection.AccessToken,
            TwitchAuthenticationService.Shared.Settings.ClientId,
            connection.UserId);

        _connectedWithAccessToken = connection.AccessToken;

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task StopAsync()
    {
        await EventClient.DisconnectAsync();
        _connectedWithAccessToken = null;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private async void AutoStartIfAuthenticated()
    {
        if (!TwitchAuthenticationService.Shared.IsAuthenticated)
        {
            return;
        }

        string currentAccessToken = TwitchAuthenticationService.Shared.Connection.AccessToken;

        // A reconnect (e.g. after adding new scopes) produces a new access token while the
        // old socket may still report itself as connected - that stale connection was
        // registered with the old token and never picks up new subscriptions on its own,
        // which is exactly why this previously required manually clicking Stop then Start.
        if (IsListening && currentAccessToken != _connectedWithAccessToken)
        {
            try
            {
                await StopAsync();
            }
            catch (Exception ex)
            {
                WadevoLogger.Warning($"Twitch stale-connection cleanup failed: {ex.Message}");
            }
        }

        if (!IsListening)
        {
            try
            {
                await StartAsync();
            }
            catch (Exception ex)
            {
                WadevoLogger.Warning($"Twitch auto-connect failed: {ex.Message}");
            }
        }
    }

    private void OnEventReceived(object? sender, TwitchEvent twitchEvent)
    {
        try
        {
            IReadOnlyList<CommandExecutionResult> results = ExecutionService.Execute(twitchEvent);

            foreach (CommandExecutionResult result in results)
            {
                LogMessage?.Invoke(this, $"Command executed: {result.Command.Name}");
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Twitch event handling failed", ex);
        }

        if (twitchEvent.EventType == TwitchEventType.Connected)
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (twitchEvent.EventType == TwitchEventType.Disconnected)
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (twitchEvent.EventType == TwitchEventType.Error && twitchEvent.Message is not null)
        {
            LogMessage?.Invoke(this, twitchEvent.Message);
        }

        EventReceived?.Invoke(this, twitchEvent);
    }
}
