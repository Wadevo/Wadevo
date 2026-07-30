namespace Wadevo.Services.Blaze;

using System.Net.Http.Json;

public sealed class BlazeAuthenticationService
{
    private static readonly Lazy<BlazeAuthenticationService> LazyShared = new(() => new BlazeAuthenticationService());

    // A single shared instance so any part of the app (Blaze module, Connections Hub, etc.)
    // sees the same live connection state instead of each one starting disconnected.
    public static BlazeAuthenticationService Shared => LazyShared.Value;

    private readonly HttpClient _httpClient = new();
    private readonly BlazeOAuthCallbackListener _callbackListener = new();
    private readonly BlazeTokenExchangeService _tokenExchangeService = new();
    private readonly BlazeConnectionStore _connectionStore = new();

    public BlazeOAuthSettings Settings { get; } = new();

    public BlazeConnectionState Connection { get; } = new();

    public BlazeAuthorizationUrlResponse? PendingAuthorization { get; private set; }

    public event EventHandler? ConnectionStateChanged;

    public bool IsConfigured => Settings.IsConfigured;

    public bool IsAuthenticated => Connection.IsAuthenticated;

    public BlazeAuthenticationService()
    {
        _connectionStore.Load(Connection);

        // If a local dev-credentials file exists, it always wins over BlazeAppCredentials.cs.
        // That file lives in %AppData%, completely outside the project, so unlike the source
        // file, it survives every future code update untouched.
        (string ClientId, string ClientSecret)? devCredentials = new BlazeDevCredentialsStore().Load();

        if (devCredentials is not null)
        {
            Settings.ClientId = devCredentials.Value.ClientId;
            Settings.ClientSecret = devCredentials.Value.ClientSecret;
        }
    }

    public async Task<BlazeAuthorizationUrlResponse> GenerateAuthorizationUrlAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Settings.IsConfigured)
            throw new InvalidOperationException("Blaze OAuth settings are not configured.");

        BlazeAuthorizationUrlRequest request = new()
        {
            ClientId = Settings.ClientId,
            ClientSecret = Settings.ClientSecret,
            RedirectUri = Settings.RedirectUri,
            Scopes = Settings.Scopes
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            Settings.GenerateAuthorizationUrlEndpoint,
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Blaze authorization URL request failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{error}");
        }

        BlazeAuthorizationUrlResponse? authorization =
            await response.Content.ReadFromJsonAsync<BlazeAuthorizationUrlResponse>(
                cancellationToken);

        if (authorization == null || string.IsNullOrWhiteSpace(authorization.Url))
            throw new InvalidOperationException("Blaze did not return an authorization URL.");

        PendingAuthorization = authorization;
        Connection.StatusMessage = "Authorization URL generated";

        RaiseConnectionStateChanged();

        return authorization;
    }

    public async Task<BlazeAuthenticationResult> CompleteLoginAsync(
        CancellationToken cancellationToken = default)
    {
        if (PendingAuthorization == null)
            return BlazeAuthenticationResult.Failed("No pending Blaze authorization request.");

        BlazeOAuthCallbackResult callback =
            await _callbackListener.WaitForCallbackAsync(
                Settings.RedirectUri,
                cancellationToken);

        if (!callback.Success)
            return BlazeAuthenticationResult.Failed(
                string.IsNullOrWhiteSpace(callback.Error)
                    ? "Blaze authorization failed."
                    : callback.Error);

        if (!string.Equals(
                callback.State,
                PendingAuthorization.State,
                StringComparison.Ordinal))
        {
            return BlazeAuthenticationResult.Failed("Blaze OAuth state mismatch.");
        }

        BlazeTokenRequest request = new()
        {
            ClientId = Settings.ClientId,
            ClientSecret = Settings.ClientSecret,
            Code = callback.Code,
            CodeVerifier = PendingAuthorization.CodeVerifier,
            RedirectUri = Settings.RedirectUri,
            GrantType = "authorization_code"
        };

        BlazeTokenResponse token =
            await _tokenExchangeService.ExchangeAsync(
                request,
                cancellationToken);

        DateTimeOffset expiresAt =
            DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        SetAuthenticated(
            token.AccessToken,
            token.RefreshToken,
            expiresAt,
            token.UserId);

        return BlazeAuthenticationResult.Successful(token);
    }

    public void Reset()
    {
        PendingAuthorization = null;
        Connection.Clear();
        RaiseConnectionStateChanged();
    }

    public void SetAuthenticated(
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAt,
        string userId = "")
    {
        Connection.AccessToken = accessToken;
        Connection.RefreshToken = refreshToken;
        Connection.TokenExpiresAt = expiresAt;
        Connection.IsAuthenticated = true;
        Connection.StatusMessage = "Authenticated";

        if (!string.IsNullOrWhiteSpace(userId))
            Connection.UserId = userId;

        RaiseConnectionStateChanged();
    }

    public void SetEventStreamConnected(bool connected)
    {
        Connection.IsEventStreamConnected = connected;

        Connection.StatusMessage = connected
            ? "Connected"
            : Connection.IsAuthenticated
                ? "Authenticated"
                : "Not connected";

        RaiseConnectionStateChanged();
    }

    private void RaiseConnectionStateChanged()
    {
        _connectionStore.Save(Connection);
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
    }
}