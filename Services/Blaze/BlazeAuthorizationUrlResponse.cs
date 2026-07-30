namespace Wadevo.Services.Blaze;

using System.Text.Json.Serialization;

public sealed class BlazeAuthorizationUrlResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("codeVerifier")]
    public string CodeVerifier { get; set; } = "";
}