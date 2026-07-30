using Wadevo.Services.Blaze;

namespace Wadevo.Services;

public static class AlertProfileTextFormatter
{
    public static string Format(string template, BlazeEventCommandContext context)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        string result = template;

        result = result.Replace(
            "{username}",
            context.Username,
            StringComparison.OrdinalIgnoreCase);

        result = result.Replace(
            "{message}",
            context.Message,
            StringComparison.OrdinalIgnoreCase);

        if (context.Data.TryGetValue("viewerCount", out object? viewerCount) &&
            viewerCount is not null)
        {
            result = result.Replace(
                "{viewerCount}",
                viewerCount.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        if (context.Data.TryGetValue("giftCount", out object? giftCount) &&
            giftCount is not null)
        {
            result = result.Replace(
                "{giftCount}",
                giftCount.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        if (context.Data.TryGetValue("voteAmount", out object? voteAmount) &&
            voteAmount is not null)
        {
            result = result.Replace(
                "{voteAmount}",
                voteAmount.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}