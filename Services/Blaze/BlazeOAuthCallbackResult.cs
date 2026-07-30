namespace Wadevo.Services.Blaze;

public sealed class BlazeOAuthCallbackResult
{
    public string Code { get; init; } = "";

    public string State { get; init; } = "";

    public string Error { get; init; } = "";

    public bool Success =>
        !string.IsNullOrWhiteSpace(Code) &&
        string.IsNullOrWhiteSpace(Error);

    public static BlazeOAuthCallbackResult FromSuccess(
        string code,
        string state)
    {
        return new BlazeOAuthCallbackResult
        {
            Code = code,
            State = state
        };
    }

    public static BlazeOAuthCallbackResult FromError(
        string error,
        string state)
    {
        return new BlazeOAuthCallbackResult
        {
            Error = error,
            State = state
        };
    }
}