namespace Wadevo.Services.Blaze;

using System.Net.Http.Json;

public sealed class BlazeBotAuthenticationService
{
    private static readonly Lazy<BlazeBotAuthenticationService> LazyShared =
        new(() => new BlazeBotAuthenticationService());

    public static BlazeBotAuthenticationService Shared => LazyShared.Value;

    private readonly BlazeOAuthCallbackListener _callbackListener = new();
    private readonly BlazeTokenExchangeService _tokenExchangeService = new();
    private readonly BlazeConnectionStore _connectionStore = new("blaze-bot-connection.dat");

    public BlazeConnectionState Connection { get; } = new();

    public BlazeAuthorizationUrlResponse? PendingAuthorization { get; private set; }

    public event EventHandler? ConnectionStateChanged;

    public bool IsConnected => Connection.IsAuthenticated;

    public string BotUserId => Connection.UserId;

    // A friendly name for display purposes - Blaze's token response doesn't include a
    // display name directly, so this is set separately after connecting via the Users API.
    public string BotDisplayName { get; set; } = "";

    public BlazeBotAuthenticationService()
    {
        _connectionStore.Load(Connection);
    }

    public async Task<BlazeAuthorizationUrlResponse> GenerateAuthorizationUrlAsync(
        CancellationToken cancellationToken = default)
    {
        BlazeOAuthSettings settings = BlazeAuthenticationService.Shared.Settings;

        if (!settings.IsConfigured)
            throw new InvalidOperationException("Blaze OAuth settings are not configured.");

        BlazeAuthorizationUrlRequest request = new()
        {
            ClientId = settings.ClientId,
            ClientSecret = settings.ClientSecret,
            RedirectUri = settings.RedirectUri,
            Scopes = settings.Scopes
        };

        using HttpClient httpClient = new();

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            settings.GenerateAuthorizationUrlEndpoint,
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Blaze authorization URL request failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{error}");
        }

        BlazeAuthorizationUrlResponse? authorization =
            await response.Content.ReadFromJsonAsync<BlazeAuthorizationUrlResponse>(cancellationToken);

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
            return BlazeAuthenticationResult.Failed("No pending Blaze bot authorization request.");

        BlazeOAuthSettings settings = BlazeAuthenticationService.Shared.Settings;

        BlazeOAuthCallbackResult callback =
            await _callbackListener.WaitForCallbackAsync(settings.RedirectUri, cancellationToken);

        if (!callback.Success)
            return BlazeAuthenticationResult.Failed(
                string.IsNullOrWhiteSpace(callback.Error)
                    ? "Blaze bot authorization failed."
                    : callback.Error);

        if (!string.Equals(callback.State, PendingAuthorization.State, StringComparison.Ordinal))
        {
            return BlazeAuthenticationResult.Failed("Blaze OAuth state mismatch.");
        }

        BlazeTokenRequest request = new()
        {
            ClientId = settings.ClientId,
            ClientSecret = settings.ClientSecret,
            Code = callback.Code,
            CodeVerifier = PendingAuthorization.CodeVerifier,
            RedirectUri = settings.RedirectUri,
            GrantType = "authorization_code"
        };

        BlazeTokenResponse token = await _tokenExchangeService.ExchangeAsync(request, cancellationToken);

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        Connection.AccessToken = token.AccessToken;
        Connection.RefreshToken = token.RefreshToken;
        Connection.TokenExpiresAt = expiresAt;
        Connection.IsAuthenticated = true;
        Connection.StatusMessage = "Bot connected";

        if (!string.IsNullOrWhiteSpace(token.UserId))
            Connection.UserId = token.UserId;

        RaiseConnectionStateChanged();

        return BlazeAuthenticationResult.Successful(token);
    }

    public void Reset()
    {
        PendingAuthorization = null;
        BotDisplayName = "";
        Connection.Clear();
        RaiseConnectionStateChanged();
    }

    private void RaiseConnectionStateChanged()
    {
        _connectionStore.Save(Connection);
        ConnectionStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
