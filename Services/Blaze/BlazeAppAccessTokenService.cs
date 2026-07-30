namespace Wadevo.Services.Blaze;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed class BlazeAppAccessTokenService
{
    private const string TokenEndpoint = "https://blaze.stream/bapi/oauth2/token";

    private readonly HttpClient _httpClient = new();

    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetTokenAsync(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        // Reuse the cached token as long as it's not close to expiring - App Access Tokens
        // are valid for 7 days, so there's no need to request a fresh one on every send.
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedTokenExpiresAt)
        {
            return _cachedToken;
        }

        AppTokenRequest request = new()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            GrantType = "client_credentials"
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            TokenEndpoint, request, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze app access token request failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }

        AppTokenResponse? token = await response.Content.ReadFromJsonAsync<AppTokenResponse>(cancellationToken);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Blaze returned an empty app access token response.");
        }

        // Refresh a little early rather than cutting it exactly at expiry.
        _cachedToken = token.AccessToken;
        _cachedTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 300));

        return _cachedToken;
    }

    private sealed class AppTokenRequest
    {
        [JsonPropertyName("clientId")]
        public string ClientId { get; init; } = "";

        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; init; } = "";

        [JsonPropertyName("grantType")]
        public string GrantType { get; init; } = "";
    }

    private sealed class AppTokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; init; } = "";

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; init; }
    }
}
