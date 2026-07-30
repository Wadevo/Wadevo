namespace Wadevo.Services.Blaze;

using System.Text.Json.Serialization;

public sealed class BlazeTokenRequest
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("codeVerifier")]
    public string CodeVerifier { get; set; } = "";

    [JsonPropertyName("redirectUri")]
    public string RedirectUri { get; set; } = "";

    [JsonPropertyName("grantType")]
    public string GrantType { get; set; } = "authorization_code";
}