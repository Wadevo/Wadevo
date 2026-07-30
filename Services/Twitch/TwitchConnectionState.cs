namespace Wadevo.Services.Twitch;

public sealed class TwitchConnectionState
{
    public bool IsAuthenticated { get; set; }

    public bool IsEventStreamConnected { get; set; }

    public string StatusMessage { get; set; } = "Not connected";

    public string UserId { get; set; } = "";

    public string Username { get; set; } = "";

    public string AccessToken { get; set; } = "";

    public string RefreshToken { get; set; } = "";

    public DateTimeOffset? TokenExpiresAt { get; set; }

    public bool HasAccessToken =>
        !string.IsNullOrWhiteSpace(AccessToken);

    public bool HasRefreshToken =>
        !string.IsNullOrWhiteSpace(RefreshToken);

    public bool IsAccessTokenExpired =>
        TokenExpiresAt.HasValue &&
        TokenExpiresAt.Value <= DateTimeOffset.UtcNow;

    public void Clear()
    {
        IsAuthenticated = false;
        IsEventStreamConnected = false;
        StatusMessage = "Not connected";
        UserId = "";
        Username = "";
        AccessToken = "";
        RefreshToken = "";
        TokenExpiresAt = null;
    }
}
