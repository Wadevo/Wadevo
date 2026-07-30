namespace Wadevo.Services.Gifs;

using System.Net;
using System.Text.Json;
using Wadevo.Models;

public sealed class GiphyGifProvider : IGifProvider
{
    private static readonly HttpClient HttpClient = new(
        new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
    {
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestVersion = HttpVersion.Version20,
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
    };

    static GiphyGifProvider()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        HttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
        HttpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        HttpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
        HttpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("deflate");
        HttpClient.DefaultRequestHeaders.ConnectionClose = false;
    }

    private const string BaseUrl = "https://api.giphy.com/v1/gifs";

    public string Name => "Giphy";

    public Task<List<GifSearchResultModel>> SearchAsync(
        string query,
        string apiKey,
        int limit,
        CancellationToken cancellationToken)
    {
        string url =
            $"{BaseUrl}/search?api_key={Uri.EscapeDataString(apiKey)}" +
            $"&q={Uri.EscapeDataString(query)}" +
            $"&limit={limit}" +
            "&rating=pg-13";

        return FetchAsync(url, cancellationToken);
    }

    public Task<List<GifSearchResultModel>> GetTrendingAsync(
        string apiKey,
        int limit,
        CancellationToken cancellationToken)
    {
        string url =
            $"{BaseUrl}/trending?api_key={Uri.EscapeDataString(apiKey)}" +
            $"&limit={limit}" +
            "&rating=pg-13";

        return FetchAsync(url, cancellationToken);
    }

    private static async Task<List<GifSearchResultModel>> FetchAsync(
        string url,
        CancellationToken cancellationToken)
    {
        List<GifSearchResultModel> results = new();

        using HttpResponseMessage response = await HttpClient.GetAsync(url, cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            string detail = ExtractGiphyErrorMessage(body);
            string rawBody = string.IsNullOrWhiteSpace(body) ? "(empty response body)" : body;

            string friendlyPart = string.IsNullOrWhiteSpace(detail)
                ? "Giphy rejected the API key."
                : $"Giphy rejected the API key: {detail}";

            throw new InvalidOperationException(
                $"{friendlyPart}\r\nHTTP {(int)response.StatusCode} {response.ReasonPhrase}\r\nRaw response: {rawBody}");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Giphy search failed ({(int)response.StatusCode}).\r\n{body}");
        }

        using JsonDocument document = JsonDocument.Parse(body);

        if (!document.RootElement.TryGetProperty("data", out JsonElement dataElement))
        {
            return results;
        }

        foreach (JsonElement item in dataElement.EnumerateArray())
        {
            string id = item.TryGetProperty("id", out JsonElement idElement)
                ? idElement.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString();

            string title = item.TryGetProperty("title", out JsonElement titleElement)
                ? titleElement.GetString() ?? ""
                : "";

            if (!item.TryGetProperty("images", out JsonElement images))
            {
                continue;
            }

            (string downloadUrl, int width, int height) = ReadImage(images, "original");
            (string previewUrl, _, _) = ReadImage(images, "fixed_width");

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                continue;
            }

            results.Add(new GifSearchResultModel
            {
                Id = id,
                Title = string.IsNullOrWhiteSpace(title) ? "Giphy GIF" : title,
                Source = "Giphy",
                DownloadUrl = downloadUrl,
                PreviewUrl = string.IsNullOrWhiteSpace(previewUrl) ? downloadUrl : previewUrl,
                Width = width,
                Height = height
            });
        }

        return results;
    }

    private static string ExtractGiphyErrorMessage(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("meta", out JsonElement meta) &&
                meta.TryGetProperty("msg", out JsonElement msg))
            {
                return msg.GetString() ?? "";
            }
        }
        catch
        {
            // Not JSON, or not in the shape we expect - fall through to the generic message.
        }

        return "";
    }

    private static (string Url, int Width, int Height) ReadImage(JsonElement images, string imageName)
    {
        if (!images.TryGetProperty(imageName, out JsonElement image))
        {
            return ("", 0, 0);
        }

        string url = image.TryGetProperty("url", out JsonElement urlElement)
            ? urlElement.GetString() ?? ""
            : "";

        int width = image.TryGetProperty("width", out JsonElement widthElement) &&
                    int.TryParse(widthElement.GetString(), out int parsedWidth)
            ? parsedWidth
            : 0;

        int height = image.TryGetProperty("height", out JsonElement heightElement) &&
                     int.TryParse(heightElement.GetString(), out int parsedHeight)
            ? parsedHeight
            : 0;

        return (url, width, height);
    }
}
