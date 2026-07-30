namespace Wadevo.Services.Twitch;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed class TwitchChatService
{
    private readonly HttpClient _httpClient = new();

    public async Task SendMessageAsync(
        string accessToken,
        string clientId,
        string broadcasterUserId,
        string senderUserId,
        string message,
        CancellationToken cancellationToken = default)
    {
        // Twitch caps chat messages at 500 characters; trim rather than fail the whole command.
        if (message.Length > 500)
        {
            message = message[..500];
        }

        TwitchChatSendRequest request = new()
        {
            BroadcasterId = broadcasterUserId,
            SenderId = senderUserId,
            Message = message
        };

        using HttpRequestMessage httpMessage = new(HttpMethod.Post, "https://api.twitch.tv/helix/chat/messages");

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.Add("Client-Id", clientId);

        httpMessage.Content = JsonContent.Create(request);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Twitch send chat message failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    private sealed class TwitchChatSendRequest
    {
        [JsonPropertyName("broadcaster_id")]
        public string BroadcasterId { get; init; } = "";

        [JsonPropertyName("sender_id")]
        public string SenderId { get; init; } = "";

        [JsonPropertyName("message")]
        public string Message { get; init; } = "";
    }
}
