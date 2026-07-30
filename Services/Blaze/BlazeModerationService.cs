namespace Wadevo.Services.Blaze;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed class BlazeModerationService
{
    private readonly HttpClient _httpClient = new();

    public async Task DeleteMessageAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        string channelId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        BlazeDeleteMessageRequest request = new()
        {
            ChannelId = channelId,
            MessageId = messageId
        };

        using HttpRequestMessage httpMessage = new(HttpMethod.Delete, "https://api.blaze.stream/v1/chats/messages");

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.TryAddWithoutValidation("client-id", clientId);
        httpMessage.Headers.TryAddWithoutValidation("secret", clientSecret);

        httpMessage.Content = JsonContent.Create(request);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze delete chat message failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    public async Task MuteUserAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        string channelId,
        string mutedUserId,
        CancellationToken cancellationToken = default)
    {
        BlazeMuteUserRequest request = new()
        {
            ChannelId = channelId,
            MutedUserId = mutedUserId
        };

        using HttpRequestMessage httpMessage = new(HttpMethod.Post, "https://api.blaze.stream/v1/moderation/mute");

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.TryAddWithoutValidation("client-id", clientId);
        httpMessage.Headers.TryAddWithoutValidation("secret", clientSecret);

        httpMessage.Content = JsonContent.Create(request);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);

        // Blaze returns 409 if the user already has an active mute - that's not a failure
        // worth surfacing, the outcome (user is muted) is the same either way.
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze mute user failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    private sealed class BlazeDeleteMessageRequest
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; init; } = "";

        [JsonPropertyName("messageId")]
        public string MessageId { get; init; } = "";
    }

    private sealed class BlazeMuteUserRequest
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; init; } = "";

        [JsonPropertyName("mutedUserId")]
        public string MutedUserId { get; init; } = "";
    }
}
