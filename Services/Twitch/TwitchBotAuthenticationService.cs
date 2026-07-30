namespace Wadevo.Services.Twitch;

public sealed class TwitchBotAuthenticationService
{
    private static readonly Lazy<TwitchBotAuthenticationService> LazyShared =
        new(() => new TwitchBotAuthenticationService());

    public static TwitchBotAuthenticationService Shared => LazyShared.Value;

    private readonly TwitchOAuthCallbackListener _callbackListener = new();
    private readonly TwitchTokenExchangeService _tokenExchangeService = new();
    private readonly TwitchConnectionStore _connectionStore = new("twitch-bot-connection.dat");
    private readonly HttpClient _httpClient = new();

    private string _pendingState = "";

    public TwitchConnectionState Connection { get; } = new();

    public event EventHandler? ConnectionStateChanged;

    public bool IsConnected => Connection.IsAuthenticated;

    public string BotUserId => Connection.UserId;

    public string BotUsername => Connection.Username;

    public TwitchBotAuthenticationService()
    {
        _connectionStore.Load(Connection);
    }

    // Reuses the main account's app credentials (Client ID/Secret) - this is still "the
    // Wadevo app" identifying itself to Twitch, just with a second Twitch account (the bot)
    // completing the login instead of the streamer's own account.
    public string BeginLogin()
    {
        TwitchOAuthSettings settings = TwitchAuthenticationService.Shared.Settings;

        if (!settings.IsConfigured)
            throw new InvalidOperationException("Twitch OAuth settings are not configured.");

        _pendingState = Guid.NewGuid().ToString("N");

        string scope = string.Join(' ', settings.Scopes);

        string url =
            $"{settings.AuthorizationEndpoint}" +
            $"?response_type=code" +
            $"&client_id={Uri.EscapeDataString(settings.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(settings.RedirectUri)}" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            $"&state={_pendingState}";

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

        Connection.StatusMessage = "Waiting for bot account authorization...";
        RaiseConnectionStateChanged();

        return url;
    }

    public async Task<bool> CompleteLoginAsync(CancellationToken cancellationToken = default)
    {
        TwitchOAuthSettings settings = TwitchAuthenticationService.Shared.Settings;

        TwitchOAuthCallbackResult callback =
            await _callbackListener.WaitForCallbackAsync(settings.RedirectUri, cancellationToken);

        if (!callback.Success)
        {
            Connection.StatusMessage = string.IsNullOrWhiteSpace(callback.Error)
                ? "Bot account authorization failed."
                : callback.Error;

            RaiseConnectionStateChanged();
            return false;
        }

        if (!string.Equals(callback.State, _pendingState, StringComparison.Ordinal))
        {
            Connection.StatusMessage = "Twitch OAuth state mismatch.";
            RaiseConnectionStateChanged();
            return false;
        }

        TwitchTokenResponse token =
            await _tokenExchangeService.ExchangeCodeAsync(settings, callback.Code, cancellationToken);

        (string userId, string username) = await GetAuthenticatedUserAsync(token.AccessToken, settings.ClientId, cancellationToken);

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        Connection.AccessToken = token.AccessToken;
        Connection.RefreshToken = token.RefreshToken;
        Connection.TokenExpiresAt = expiresAt;
        Connection.IsAuthenticated = true;
        Connection.UserId = userId;
        Connection.Username = username;
        Connection.StatusMessage = "Bot connected";

        RaiseConnectionStateChanged();

        return true;
    }

    public void Reset()
    {
        Connection.Clear();
        RaiseConnectionStateChanged();
    }

    private async Task<(string UserId, string Username)> GetAuthenticatedUserAsync(
        string accessToken,
        string clientId,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Client-Id", clientId);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);

        System.Text.Json.JsonElement user = document.RootElement.GetProperty("data")[0];

        return (
            user.GetProperty("id").GetString() ?? "",
            user.GetProperty("display_name").GetString() ?? "");
    }

    private void RaiseConnectionStateChanged()
    {
        _connectionStore.Save(Connection);
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
