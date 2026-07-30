namespace Wadevo.Services.Twitch;

using System.Net.Http.Headers;
using System.Text.Json;

public sealed class TwitchAuthenticationService
{
    private const string GetUsersEndpoint = "https://api.twitch.tv/helix/users";

    private static readonly Lazy<TwitchAuthenticationService> LazyShared = new(() => new TwitchAuthenticationService());

    // A single shared instance so any part of the app (Twitch module, Connections Hub,
    // etc.) sees the same live connection state instead of each one starting disconnected.
    public static TwitchAuthenticationService Shared => LazyShared.Value;

    private readonly HttpClient _httpClient = new();
    private readonly TwitchOAuthCallbackListener _callbackListener = new();
    private readonly TwitchTokenExchangeService _tokenExchangeService = new();
    private readonly TwitchConnectionStore _connectionStore = new();

    private string _pendingState = "";

    public TwitchOAuthSettings Settings { get; } = new();

    public TwitchConnectionState Connection { get; } = new();

    public event EventHandler? ConnectionStateChanged;

    public bool IsConfigured => Settings.IsConfigured;

    public bool IsAuthenticated => Connection.IsAuthenticated;

    public TwitchAuthenticationService()
    {
        _connectionStore.Load(Connection);

        // If a local dev-credentials file exists, it always wins over TwitchAppCredentials.cs.
        // That file lives in %AppData%, completely outside the project, so unlike the source
        // file, it survives every future code update untouched - and it's never at risk of
        // being committed to source control.
        (string ClientId, string ClientSecret)? devCredentials = new TwitchDevCredentialsStore().Load();

        if (devCredentials is not null)
        {
            Settings.ClientId = devCredentials.Value.ClientId;
            Settings.ClientSecret = devCredentials.Value.ClientSecret;
        }
    }

    // Builds the Twitch authorize URL and opens it in the user's default browser.
    // Unlike Blaze, Twitch's authorize endpoint is called directly - there's no
    // separate "generate auth url" API step.
    public string BeginLogin()
    {
        if (!Settings.IsConfigured)
            throw new InvalidOperationException("Twitch OAuth settings are not configured.");

        _pendingState = Guid.NewGuid().ToString("N");

        string scope = string.Join(' ', Settings.Scopes);

        string url =
            $"{Settings.AuthorizationEndpoint}" +
            $"?response_type=code" +
            $"&client_id={Uri.EscapeDataString(Settings.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(Settings.RedirectUri)}" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            $"&state={_pendingState}";

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

        Connection.StatusMessage = "Waiting for Twitch authorization...";
        RaiseConnectionStateChanged();

        return url;
    }

    public async Task<bool> CompleteLoginAsync(CancellationToken cancellationToken = default)
    {
        TwitchOAuthCallbackResult callback =
            await _callbackListener.WaitForCallbackAsync(Settings.RedirectUri, cancellationToken);

        if (!callback.Success)
        {
            Connection.StatusMessage = string.IsNullOrWhiteSpace(callback.Error)
                ? "Twitch authorization failed."
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
            await _tokenExchangeService.ExchangeCodeAsync(Settings, callback.Code, cancellationToken);

        (string userId, string username) = await GetAuthenticatedUserAsync(token.AccessToken, cancellationToken);

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        Connection.AccessToken = token.AccessToken;
        Connection.RefreshToken = token.RefreshToken;
        Connection.TokenExpiresAt = expiresAt;
        Connection.IsAuthenticated = true;
        Connection.UserId = userId;
        Connection.Username = username;
        Connection.StatusMessage = "Authenticated";

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
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, GetUsersEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Client-Id", Settings.ClientId);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement user = document.RootElement.GetProperty("data")[0];

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
