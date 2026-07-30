namespace Wadevo.Services.Blaze;

public sealed class BlazeAuthenticationResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = "";

    public BlazeTokenResponse? Token { get; init; }

    public static BlazeAuthenticationResult Successful(
        BlazeTokenResponse token)
    {
        return new BlazeAuthenticationResult
        {
            Success = true,
            Message = "Authentication successful.",
            Token = token
        };
    }

    public static BlazeAuthenticationResult Failed(
        string message)
    {
        return new BlazeAuthenticationResult
        {
            Success = false,
            Message = message
        };
    }
}