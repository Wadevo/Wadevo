namespace Wadevo.Services.Twitch;

public sealed class TwitchOAuthSettings
{
    public string ClientId { get; set; } = TwitchAppCredentials.ClientId;

    public string ClientSecret { get; set; } = TwitchAppCredentials.ClientSecret;

    public string RedirectUri { get; set; } = "http://localhost:5056/twitch/oauth/callback/";

    public string AuthorizationEndpoint { get; set; } = "https://id.twitch.tv/oauth2/authorize";

    public string TokenEndpoint { get; set; } = "https://id.twitch.tv/oauth2/token";

    // user:read:chat / user:write:chat - read and send chat messages via EventSub + Helix
    // channel:read:subscriptions - subscription events
    // moderator:read:followers - follow events (requires the token owner to be a mod/broadcaster)
    // moderator:manage:chat_messages - delete a blocked-word message
    // moderator:manage:banned_users - timeout a user after repeated blocked words
    // channel:manage:broadcast - update stream title/category from the Dashboard
    public string[] Scopes { get; set; } =
    [
        "user:read:chat",
        "user:write:chat",
        "channel:read:subscriptions",
        "moderator:read:followers",
        "moderator:manage:chat_messages",
        "moderator:manage:banned_users",
        "channel:manage:broadcast",
        "bits:read"
    ];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(RedirectUri) &&
        ClientId != "PASTE_YOUR_TWITCH_CLIENT_ID_HERE" &&
        ClientSecret != "PASTE_YOUR_TWITCH_CLIENT_SECRET_HERE";
}
