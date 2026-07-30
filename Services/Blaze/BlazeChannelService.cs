namespace Wadevo.Services.Blaze;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Wadevo.Models;

/// <summary>
/// Wraps the Blaze Channels endpoints Wadevo hadn't touched yet: point-in-time totals
/// (stats), current-session counters (live-stats), and updating the channel's own
/// title/category/language directly, instead of alt-tabbing to the Blaze website.
/// </summary>
public sealed class BlazeChannelService
{
    private readonly HttpClient _httpClient = new();

    public async Task<BlazeChannelStatsModel> GetStatsAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage httpMessage = new(HttpMethod.Get, "https://api.blaze.stream/v1/channels/stats");
        ApplyHeaders(httpMessage, accessToken, clientId, clientSecret);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze get channel stats failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }

        BlazeStatsResponse? parsed = System.Text.Json.JsonSerializer.Deserialize<BlazeStatsResponse>(body, JsonOptions);
        BlazeStatsData data = parsed?.Data ?? new BlazeStatsData();

        return new BlazeChannelStatsModel
        {
            FollowerCount = data.FollowerCount,
            SubscriberCount = data.SubscriberCount,
            ViewerCount = data.ViewerCount
        };
    }

    public async Task<BlazeLiveStatsModel> GetLiveStatsAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage httpMessage = new(HttpMethod.Get, "https://api.blaze.stream/v1/channels/live-stats");
        ApplyHeaders(httpMessage, accessToken, clientId, clientSecret);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze get live stats failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }

        BlazeLiveStatsResponse? parsed = System.Text.Json.JsonSerializer.Deserialize<BlazeLiveStatsResponse>(body, JsonOptions);
        BlazeLiveStatsData data = parsed?.Data ?? new BlazeLiveStatsData();

        return new BlazeLiveStatsModel
        {
            IsLive = data.IsLive,
            StartedAt = data.StartedAt,
            NewFollowerCount = data.NewFollowerCount,
            NewSubscriberCount = data.NewSubscriberCount,
            ViewerCount = data.ViewerCount
        };
    }

    public async Task UpdateChannelInfoAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        string title,
        string lang,
        int? categoryId,
        CancellationToken cancellationToken = default)
    {
        BlazeUpdateChannelInfoRequest request = new()
        {
            Title = title,
            Lang = lang,
            CategoryId = categoryId
        };

        using HttpRequestMessage httpMessage = new(HttpMethod.Post, "https://api.blaze.stream/v1/channels/info");
        ApplyHeaders(httpMessage, accessToken, clientId, clientSecret);
        httpMessage.Content = JsonContent.Create(request);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze update channel info failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    private static void ApplyHeaders(HttpRequestMessage httpMessage, string accessToken, string clientId, string clientSecret)
    {
        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.TryAddWithoutValidation("client-id", clientId);
        httpMessage.Headers.TryAddWithoutValidation("secret", clientSecret);
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class BlazeUpdateChannelInfoRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = "";

        [JsonPropertyName("lang")]
        public string Lang { get; init; } = "";

        [JsonPropertyName("categoryId")]
        public int? CategoryId { get; init; }
    }

    private sealed class BlazeStatsResponse
    {
        [JsonPropertyName("data")]
        public BlazeStatsData? Data { get; init; }
    }

    private sealed class BlazeStatsData
    {
        [JsonPropertyName("followerCount")]
        public int FollowerCount { get; init; }

        [JsonPropertyName("subscriberCount")]
        public int SubscriberCount { get; init; }

        [JsonPropertyName("viewerCount")]
        public int ViewerCount { get; init; }
    }

    private sealed class BlazeLiveStatsResponse
    {
        [JsonPropertyName("data")]
        public BlazeLiveStatsData? Data { get; init; }
    }

    private sealed class BlazeLiveStatsData
    {
        [JsonPropertyName("isLive")]
        public bool IsLive { get; init; }

        [JsonPropertyName("startedAt")]
        public DateTimeOffset? StartedAt { get; init; }

        [JsonPropertyName("newFollowerCount")]
        public int NewFollowerCount { get; init; }

        [JsonPropertyName("newSubscriberCount")]
        public int NewSubscriberCount { get; init; }

        [JsonPropertyName("viewerCount")]
        public int ViewerCount { get; init; }
    }
}
