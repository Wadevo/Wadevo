namespace Wadevo.Services.Blaze;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed class BlazeChatService
{
    private readonly HttpClient _httpClient = new();

    public async Task SendMessageAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        string channelId,
        string message,
        CancellationToken cancellationToken = default)
    {
        // Blaze caps chat messages at 500 characters; trim rather than fail the whole command.
        if (message.Length > 500)
        {
            message = message[..500];
        }

        BlazeChatSendRequest request = new()
        {
            ChannelId = channelId,
            Message = message
        };

        using HttpRequestMessage httpMessage = new(HttpMethod.Post, "https://api.blaze.stream/v1/chats/messages");

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
                $"Blaze send chat message failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    // Sends as a separate bot identity rather than the streamer's own account. Uses an App
    // Access Token (no user login involved) plus the bot account's own user ID - that bot
    // account must have separately authorized this app with the users.bot scope.
    public async Task SendMessageAsBotAsync(
        string appAccessToken,
        string clientId,
        string clientSecret,
        string channelId,
        string botSenderId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (message.Length > 500)
        {
            message = message[..500];
        }

        BlazeChatSendAsBotRequest request = new()
        {
            ChannelId = channelId,
            Message = message,
            SenderId = botSenderId
        };

        using HttpRequestMessage httpMessage = new(HttpMethod.Post, "https://api.blaze.stream/v1/chats/messages");

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appAccessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.TryAddWithoutValidation("client-id", clientId);
        httpMessage.Headers.TryAddWithoutValidation("secret", clientSecret);

        httpMessage.Content = JsonContent.Create(request);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze send chat message as bot failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    private sealed class BlazeChatSendAsBotRequest
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; init; } = "";

        [JsonPropertyName("message")]
        public string Message { get; init; } = "";

        [JsonPropertyName("senderId")]
        public string SenderId { get; init; } = "";
    }

    private sealed class BlazeChatSendRequest
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; init; } = "";

        [JsonPropertyName("message")]
        public string Message { get; init; } = "";
    }
}
