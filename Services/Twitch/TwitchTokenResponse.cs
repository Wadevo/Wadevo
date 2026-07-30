namespace Wadevo.Services.Twitch;

using System.Text.Json.Serialization;

public sealed class TwitchTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string[] Scopes { get; set; } = [];

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";
}
