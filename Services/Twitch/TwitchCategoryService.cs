namespace Wadevo.Services.Twitch;

using System.Net.Http.Headers;
using System.Text.Json;
using Wadevo.Models;

public sealed class TwitchCategoryService
{
    private readonly HttpClient _httpClient = new();

    public async Task<IReadOnlyList<TwitchCategoryModel>> SearchCategoriesAsync(
        string accessToken,
        string clientId,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<TwitchCategoryModel>();
        }

        using HttpRequestMessage httpMessage = new(
            HttpMethod.Get,
            $"https://api.twitch.tv/helix/search/categories?query={Uri.EscapeDataString(query)}&first=20");

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.Add("Client-Id", clientId);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Twitch search categories failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }

        List<TwitchCategoryModel> results = new();

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement dataArray = document.RootElement.GetProperty("data");

        foreach (JsonElement item in dataArray.EnumerateArray())
        {
            results.Add(new TwitchCategoryModel
            {
                Id = item.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "",
                Name = item.TryGetProperty("name", out JsonElement name) ? name.GetString() ?? "" : "",
                BoxArtUrl = item.TryGetProperty("box_art_url", out JsonElement art) ? art.GetString() ?? "" : ""
            });
        }

        return results;
    }
}
