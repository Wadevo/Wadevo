namespace Wadevo.Services.Blaze;

using Wadevo.Models;
using Wadevo.Services;

public sealed class BlazeLiveEventService
{
    private static readonly Lazy<BlazeLiveEventService> LazyShared = new(() => new BlazeLiveEventService());

    // A single shared instance that keeps listening for chat/follow/raid events and
    // executing matching commands no matter which page of Wadevo is currently open.
    public static BlazeLiveEventService Shared => LazyShared.Value;

    public IBlazeEventClient EventClient { get; } = new BlazeSocketIoEventClient();

    public BlazeEventExecutionService ExecutionService { get; } = new();

    public BlazeEventSubscriptionService SubscriptionService { get; } = new();

    public bool IsListening => EventClient.IsConnected;

    public event EventHandler<BlazeEvent>? EventReceived;

    public event EventHandler<string>? LogMessage;

    public event EventHandler? StatusChanged;

    private BlazeLiveEventService()
    {
        EventClient.EventReceived += OnEventReceived;

        if (EventClient is BlazeSocketIoEventClient socketClient)
        {
            socketClient.SessionReady += OnSessionReady;
        }

        BlazeAuthenticationService.Shared.ConnectionStateChanged += (_, _) => AutoStartIfAuthenticated();

        // Covers the case where a saved login is already valid the moment the app starts,
        // so the person doesn't have to visit the Blaze page at all for commands to work.
        AutoStartIfAuthenticated();
    }

    private string? _connectedWithAccessToken;

    public async Task StartAsync()
    {
        if (!BlazeAuthenticationService.Shared.IsAuthenticated || IsListening)
        {
            return;
        }

        string accessToken = BlazeAuthenticationService.Shared.Connection.AccessToken;

        await EventClient.ConnectAsync(accessToken);
        _connectedWithAccessToken = accessToken;

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
        if (!BlazeAuthenticationService.Shared.IsAuthenticated)
        {
            return;
        }

        string currentAccessToken = BlazeAuthenticationService.Shared.Connection.AccessToken;

        // A reconnect (or a token refresh) produces a new access token while the old
        // connection may still report itself as connected - that stale connection was
        // established with the old token and never picks up again on its own, which is
        // exactly why this previously required manually reconnecting to get events flowing.
        if (IsListening && currentAccessToken != _connectedWithAccessToken)
        {
            try
            {
                await StopAsync();
            }
            catch (Exception ex)
            {
                WadevoLogger.Warning($"Blaze stale-connection cleanup failed: {ex.Message}");
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
                WadevoLogger.Warning($"Blaze auto-connect failed: {ex.Message}");
            }
        }
    }

    private void OnEventReceived(object? sender, BlazeEvent blazeEvent)
    {
        try
        {
            IReadOnlyList<CommandExecutionResult> results = ExecutionService.Execute(blazeEvent);

            foreach (CommandExecutionResult result in results)
            {
                LogMessage?.Invoke(this, $"Command executed: {result.Command.Name}");
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Blaze event handling failed", ex);
        }

        EventReceived?.Invoke(this, blazeEvent);
    }

    private async void OnSessionReady(object? sender, string sessionId)
    {
        string userId = BlazeAuthenticationService.Shared.Connection.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            LogMessage?.Invoke(this, "Missing Blaze user ID. Reconnect to Blaze.");
            return;
        }

        try
        {
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.Follow);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.Raid);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.ChatMessage);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.Subscribe);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.GiftSubscription);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.Vote);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.VipAdd);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.StreamOnline);
            await SubscribeToEventAsync(sessionId, userId, BlazeEventSubscriptions.StreamOffline);

            LogMessage?.Invoke(this, "✅ Subscribed to Blaze events");
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Blaze event subscription failed", ex);
            LogMessage?.Invoke(this, ex.Message);
        }
    }

    private async Task SubscribeToEventAsync(string sessionId, string userId, string subscriptionType)
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        await SubscriptionService.SubscribeAsync(
            auth.Connection.AccessToken,
            auth.Settings.ClientId,
            auth.Settings.ClientSecret,
            sessionId,
            userId,
            subscriptionType);
    }
}
