namespace Wadevo.Services.Blaze;

using System.Net.Http.Json;

public sealed class BlazeTokenExchangeService
{
    private const string TokenEndpoint =
        "https://blaze.stream/bapi/oauth2/token";

    private readonly HttpClient _httpClient = new();

    public async Task<BlazeTokenResponse> ExchangeAsync(
        BlazeTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(
                TokenEndpoint,
                request,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error =
                await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Blaze token exchange failed ({(int)response.StatusCode})\r\n{error}");
        }

        BlazeTokenResponse? token =
            await response.Content.ReadFromJsonAsync<BlazeTokenResponse>(
                cancellationToken);

        if (token == null)
            throw new InvalidOperationException(
                "Blaze returned an empty token response.");

        return token;
    }
}