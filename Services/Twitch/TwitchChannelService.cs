namespace Wadevo.Services.Twitch;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Wadevo.Models;

public sealed class TwitchChannelService
{
    private readonly HttpClient _httpClient = new();

    public async Task<TwitchChannelStatsModel> GetStatsAsync(
        string accessToken,
        string clientId,
        string broadcasterUserId,
        CancellationToken cancellationToken = default)
    {
        TwitchChannelStatsModel stats = new();

        // Each of these can fail independently (e.g. subscriber count needs Affiliate/
        // Partner status) without the whole dashboard card failing - best effort per call.
        await TryFillLiveAndViewerCountAsync(stats, accessToken, clientId, broadcasterUserId, cancellationToken);
        await TryFillFollowerCountAsync(stats, accessToken, clientId, broadcasterUserId, cancellationToken);
        await TryFillSubscriberCountAsync(stats, accessToken, clientId, broadcasterUserId, cancellationToken);
        await TryFillChannelInfoAsync(stats, accessToken, clientId, broadcasterUserId, cancellationToken);

        return stats;
    }

    public async Task UpdateChannelInfoAsync(
        string accessToken,
        string clientId,
        string broadcasterUserId,
        string title,
        string? categoryId,
        CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?> { ["title"] = title };

        if (categoryId is not null)
        {
            body["game_id"] = categoryId;
        }

        using HttpRequestMessage httpMessage = new(
            HttpMethod.Patch,
            $"https://api.twitch.tv/helix/channels?broadcaster_id={Uri.EscapeDataString(broadcasterUserId)}")
        {
            Content = JsonContent.Create(body)
        };

        ApplyHeaders(httpMessage, accessToken, clientId);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body2 = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"Twitch update channel info failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body2}");
    }

    private async Task TryFillLiveAndViewerCountAsync(
        TwitchChannelStatsModel stats,
        string accessToken,
        string clientId,
        string broadcasterUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage httpMessage = new(
                HttpMethod.Get,
                $"https://api.twitch.tv/helix/streams?user_id={Uri.EscapeDataString(broadcasterUserId)}");

            ApplyHeaders(httpMessage, accessToken, clientId);

            using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);

            JsonElement dataArray = document.RootElement.GetProperty("data");

            if (dataArray.GetArrayLength() > 0)
            {
                JsonElement stream = dataArray[0];
                stats.IsLive = true;
                stats.ViewerCount = stream.TryGetProperty("viewer_count", out JsonElement vc) ? vc.GetInt32() : 0;
            }
        }
        catch
        {
            // Leave IsLive/ViewerCount at their defaults on failure.
        }
    }

    private async Task TryFillFollowerCountAsync(
        TwitchChannelStatsModel stats,
        string accessToken,
        string clientId,
        string broadcasterUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage httpMessage = new(
                HttpMethod.Get,
                $"https://api.twitch.tv/helix/channels/followers?broadcaster_id={Uri.EscapeDataString(broadcasterUserId)}&first=1");

            ApplyHeaders(httpMessage, accessToken, clientId);

            using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty("total", out JsonElement total))
            {
                stats.FollowerCount = total.GetInt32();
            }
        }
        catch
        {
            // Leave FollowerCount at 0 on failure (e.g. missing moderator:read:followers scope).
        }
    }

    private async Task TryFillSubscriberCountAsync(
        TwitchChannelStatsModel stats,
        string accessToken,
        string clientId,
        string broadcasterUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage httpMessage = new(
                HttpMethod.Get,
                $"https://api.twitch.tv/helix/subscriptions?broadcaster_id={Uri.EscapeDataString(broadcasterUserId)}&first=1");

            ApplyHeaders(httpMessage, accessToken, clientId);

            using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty("total", out JsonElement total))
            {
                stats.SubscriberCount = total.GetInt32();
            }
        }
        catch
        {
            // Non-Affiliate/Partner channels get a 403 here - leave SubscriberCount at 0.
        }
    }

    private async Task TryFillChannelInfoAsync(
        TwitchChannelStatsModel stats,
        string accessToken,
        string clientId,
        string broadcasterUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage httpMessage = new(
                HttpMethod.Get,
                $"https://api.twitch.tv/helix/channels?broadcaster_id={Uri.EscapeDataString(broadcasterUserId)}");

            ApplyHeaders(httpMessage, accessToken, clientId);

            using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);

            JsonElement dataArray = document.RootElement.GetProperty("data");

            if (dataArray.GetArrayLength() > 0)
            {
                JsonElement channel = dataArray[0];
                stats.Title = channel.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? "" : "";
                stats.CategoryName = channel.TryGetProperty("game_name", out JsonElement g) ? g.GetString() ?? "" : "";
            }
        }
        catch
        {
            // Leave Title/CategoryName blank on failure.
        }
    }

    private static void ApplyHeaders(HttpRequestMessage httpMessage, string accessToken, string clientId)
    {
        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.Add("Client-Id", clientId);
    }
}
