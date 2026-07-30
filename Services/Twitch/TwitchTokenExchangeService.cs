namespace Wadevo.Services.Twitch;

using System.Net.Http.Json;

public sealed class TwitchTokenExchangeService
{
    private readonly HttpClient _httpClient = new();

    public async Task<TwitchTokenResponse> ExchangeCodeAsync(
        TwitchOAuthSettings settings,
        string code,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> form = new()
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = settings.RedirectUri
        };

        return await PostFormAsync(settings.TokenEndpoint, form, cancellationToken);
    }

    public async Task<TwitchTokenResponse> RefreshAsync(
        TwitchOAuthSettings settings,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> form = new()
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };

        return await PostFormAsync(settings.TokenEndpoint, form, cancellationToken);
    }

    private async Task<TwitchTokenResponse> PostFormAsync(
        string endpoint,
        Dictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent content = new(form);

        using HttpResponseMessage response =
            await _httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Twitch token exchange failed ({(int)response.StatusCode})\r\n{error}");
        }

        TwitchTokenResponse? token =
            await response.Content.ReadFromJsonAsync<TwitchTokenResponse>(cancellationToken);

        if (token == null)
            throw new InvalidOperationException("Twitch returned an empty token response.");

        return token;
    }
}
