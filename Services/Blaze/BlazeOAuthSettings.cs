namespace Wadevo.Services.Blaze;

public sealed class BlazeOAuthSettings
{
    public string ClientId { get; set; } = BlazeAppCredentials.ClientId;

    public string ClientSecret { get; set; } = BlazeAppCredentials.ClientSecret;

    public string RedirectUri { get; set; } = "http://localhost:5055/blaze/oauth/callback/";

    public string GenerateAuthorizationUrlEndpoint { get; set; } =
        "https://blaze.stream/bapi/oauth2/generate-auth-url";

    public string TokenEndpoint { get; set; } = "";

    public string[] Scopes { get; set; } =
    [
        "users.read",
        "offline.access",
        "channel.moderate",
        "users.bot"
    ];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(RedirectUri) &&
        !string.IsNullOrWhiteSpace(GenerateAuthorizationUrlEndpoint) &&
        ClientId != "PASTE_YOUR_BLAZE_CLIENT_ID_HERE" &&
        ClientSecret != "PASTE_YOUR_BLAZE_CLIENT_SECRET_HERE";
}