namespace Wadevo.Services.Twitch;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed class TwitchModerationService
{
    private readonly HttpClient _httpClient = new();

    public async Task DeleteMessageAsync(
        string accessToken,
        string clientId,
        string broadcasterUserId,
        string moderatorUserId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        string url =
            $"https://api.twitch.tv/helix/moderation/chat" +
            $"?broadcaster_id={Uri.EscapeDataString(broadcasterUserId)}" +
            $"&moderator_id={Uri.EscapeDataString(moderatorUserId)}" +
            $"&message_id={Uri.EscapeDataString(messageId)}";

        using HttpRequestMessage httpMessage = new(HttpMethod.Delete, url);

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Add("Client-Id", clientId);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Twitch delete chat message failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    // Twitch has no separate "mute" concept for chat like Blaze does - a timeout (a ban
    // with a duration) is the equivalent. Ten minutes to match Blaze's own default.
    public async Task TimeoutUserAsync(
        string accessToken,
        string clientId,
        string broadcasterUserId,
        string moderatorUserId,
        string targetUserId,
        int durationSeconds = 600,
        string reason = "Blocked word",
        CancellationToken cancellationToken = default)
    {
        string url =
            $"https://api.twitch.tv/helix/moderation/bans" +
            $"?broadcaster_id={Uri.EscapeDataString(broadcasterUserId)}" +
            $"&moderator_id={Uri.EscapeDataString(moderatorUserId)}";

        TwitchBanRequest request = new()
        {
            Data = new TwitchBanRequestData
            {
                UserId = targetUserId,
                Duration = durationSeconds,
                Reason = reason
            }
        };

        using HttpRequestMessage httpMessage = new(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(request)
        };

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Add("Client-Id", clientId);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);

        // Twitch returns 400 if the user already has an active timeout - not worth
        // surfacing as a failure, the outcome (user is timed out) is the same either way.
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Twitch timeout user failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    private sealed class TwitchBanRequest
    {
        [JsonPropertyName("data")]
        public TwitchBanRequestData Data { get; init; } = new();
    }

    private sealed class TwitchBanRequestData
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; init; } = "";

        [JsonPropertyName("duration")]
        public int Duration { get; init; }

        [JsonPropertyName("reason")]
        public string Reason { get; init; } = "";
    }
}
