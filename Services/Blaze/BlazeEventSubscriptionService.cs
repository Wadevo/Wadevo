namespace Wadevo.Services.Blaze;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed class BlazeEventSubscriptionService
{
    private readonly HttpClient _httpClient = new();

    public async Task SubscribeAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        string sessionId,
        string channelId,
        string subscriptionType,
        CancellationToken cancellationToken = default)
    {
        BlazeEventSubscriptionRequest request = new()
        {
            Type = subscriptionType,
            Version = "1",
            SessionId = sessionId,
            Condition = new BlazeEventSubscriptionCondition
            {
                ChannelId = channelId
            }
        };

        using HttpRequestMessage message = CreateRequest(
            HttpMethod.Post,
            "https://api.blaze.stream/v1/events/subscriptions",
            accessToken,
            clientId,
            clientSecret);

        message.Content = JsonContent.Create(request);

        using HttpResponseMessage response =
            await _httpClient.SendAsync(message, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze event subscription failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }
    }

    public async Task<string> GetSubscriptionsRawAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage message = CreateRequest(
            HttpMethod.Get,
            $"https://api.blaze.stream/v1/events/{sessionId}/subscriptions",
            accessToken,
            clientId,
            clientSecret);

        using HttpResponseMessage response =
            await _httpClient.SendAsync(message, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze get subscriptions failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }

        return
            $"Blaze subscriptions response\r\n" +
            $"Status: {(int)response.StatusCode} {response.ReasonPhrase}\r\n" +
            $"Session ID: {sessionId}\r\n" +
            $"Body:\r\n{body}";
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        string accessToken,
        string clientId,
        string clientSecret)
    {
        HttpRequestMessage message = new(method, url);

        message.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        message.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        message.Headers.TryAddWithoutValidation("client-id", clientId);
        message.Headers.TryAddWithoutValidation("secret", clientSecret);

        return message;
    }

    private sealed class BlazeEventSubscriptionRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "";

        [JsonPropertyName("version")]
        public string Version { get; init; } = "1";

        [JsonPropertyName("sessionId")]
        public string SessionId { get; init; } = "";

        [JsonPropertyName("condition")]
        public BlazeEventSubscriptionCondition Condition { get; init; } = new();
    }

    private sealed class BlazeEventSubscriptionCondition
    {
        [JsonPropertyName("channelId")]
        public string ChannelId { get; init; } = "";
    }
}