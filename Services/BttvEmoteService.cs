namespace Wadevo.Services;

using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Wadevo.Models;

/// <summary>
/// Fetches BetterTTV's public global emote list (https://api.betterttv.net/3/cached/emotes/global) -
/// no auth or API key needed, a stable well-established REST endpoint (unlike 7TV, whose
/// real interface is an undocumented, community-reverse-engineered GraphQL API that broke
/// once already during a backend migration). Results are cached in memory since this list
/// changes rarely and there's no reason to refetch it every time the emote picker opens.
/// </summary>
public sealed class BttvEmoteService
{
    private const string GlobalEmotesUrl = "https://api.betterttv.net/3/cached/emotes/global";

    private static readonly HttpClient HttpClient = new();
    private static IReadOnlyList<EmoteModel>? _cachedEmotes;

    public async Task<IReadOnlyList<EmoteModel>> GetGlobalEmotesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedEmotes is not null)
        {
            return _cachedEmotes;
        }

        using HttpRequestMessage request = new(HttpMethod.Get, GlobalEmotesUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"BetterTTV global emotes request failed: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        List<BttvEmoteEntry>? entries = System.Text.Json.JsonSerializer.Deserialize<List<BttvEmoteEntry>>(
            body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        List<EmoteModel> emotes = (entries ?? new List<BttvEmoteEntry>())
            .Select(entry => new EmoteModel
            {
                Shortcode = entry.Code,
                // 2x is the size the vast majority of BTTV emotes have available and reads
                // clearly at typical overlay text sizes without being oversized.
                ImageUrl = $"https://cdn.betterttv.net/emote/{entry.Id}/2x",
                IsAnimated = entry.Animated,
                IsCustom = false
            })
            .ToList();

        _cachedEmotes = emotes;

        return emotes;
    }

    private sealed class BttvEmoteEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = "";

        [JsonPropertyName("code")]
        public string Code { get; init; } = "";

        [JsonPropertyName("animated")]
        public bool Animated { get; init; }
    }
}
