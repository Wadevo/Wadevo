namespace Wadevo.Services.Blaze;

using System.Text.Json.Serialization;

public sealed class BlazeAuthorizationUrlRequest
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = "";

    [JsonPropertyName("redirectUri")]
    public string RedirectUri { get; set; } = "";

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; } = [];
}