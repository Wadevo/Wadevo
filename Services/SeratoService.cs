namespace Wadevo.Services;

using System.Net;
using System.Text.RegularExpressions;

public class SeratoService
{
    private readonly string _playlistUrl;
    private readonly HttpClient _http;

    public SeratoService(string playlistUrl)
    {
        _playlistUrl = playlistUrl;

        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 Wadevo/1.0");
    }

    public async Task<string> GetCurrentSongAsync()
    {
        string html = await _http.GetStringAsync(_playlistUrl);
        return ExtractLatestSong(html);
    }

    private static string ExtractLatestSong(string html)
    {
        Match match = Regex.Match(
            html,
            "<div class=\"playlist-trackname\">\\s*(.*?)\\s*</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
            return "";

        return CleanHtml(match.Groups[1].Value);
    }

    private static string CleanHtml(string html)
    {
        string text = Regex.Replace(html, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }
}