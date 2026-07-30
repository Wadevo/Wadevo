namespace Wadevo.Services.Blaze;

using System.Net;
using System.Text;

public sealed class BlazeOAuthCallbackListener
{
    public async Task<BlazeOAuthCallbackResult> WaitForCallbackAsync(
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("Redirect URI is required.", nameof(redirectUri));

        Uri uri = new(redirectUri);

        string listenerPrefix =
            $"{uri.Scheme}://{uri.Host}:{uri.Port}/";

        using HttpListener listener = new();
        listener.Prefixes.Add(listenerPrefix);
        listener.Start();

        using CancellationTokenRegistration registration =
            cancellationToken.Register(() =>
            {
                try
                {
                    listener.Stop();
                }
                catch
                {
                    // Ignore cancellation shutdown errors.
                }
            });

        HttpListenerContext context = await listener.GetContextAsync();

        string code = context.Request.QueryString["code"] ?? "";
        string state = context.Request.QueryString["state"] ?? "";
        string error = context.Request.QueryString["error"] ?? "";

        BlazeOAuthCallbackResult result = string.IsNullOrWhiteSpace(error)
            ? BlazeOAuthCallbackResult.FromSuccess(code, state)
            : BlazeOAuthCallbackResult.FromError(error, state);

        await WriteBrowserResponseAsync(context.Response, result);

        return result;
    }

    private static async Task WriteBrowserResponseAsync(
        HttpListenerResponse response,
        BlazeOAuthCallbackResult result)
    {
        string message = result.Success
            ? "Blaze connected. You can return to Wadevo."
            : "Blaze connection failed. You can return to Wadevo.";

        string html =
            "<!doctype html>" +
            "<html><head><title>Wadevo Blaze Login</title></head>" +
            "<body style=\"font-family:Segoe UI,Arial,sans-serif;background:#111827;color:#f9fafb;padding:40px;\">" +
            $"<h1>{WebUtility.HtmlEncode(message)}</h1>" +
            "<p>This browser tab can be closed.</p>" +
            "</body></html>";

        byte[] buffer = Encoding.UTF8.GetBytes(html);

        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }
}