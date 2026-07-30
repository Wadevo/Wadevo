namespace Wadevo.Services.Twitch;

public sealed class TwitchOAuthCallbackResult
{
    public bool Success { get; init; }

    public string Code { get; init; } = "";

    public string State { get; init; } = "";

    public string Error { get; init; } = "";

    public static TwitchOAuthCallbackResult FromSuccess(string code, string state)
    {
        return new TwitchOAuthCallbackResult { Success = true, Code = code, State = state };
    }

    public static TwitchOAuthCallbackResult FromError(string error, string state)
    {
        return new TwitchOAuthCallbackResult { Success = false, Error = error, State = state };
    }
}
