namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public static class MusicMetadataService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string SearchUrl = "https://itunes.apple.com/search";

    public static async Task<MusicMetadataModel?> LookupAsync(
        string artist,
        string song,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artist) && string.IsNullOrWhiteSpace(song))
        {
            return null;
        }

        string term = $"{artist} {song}".Trim();

        string url =
            $"{SearchUrl}?term={Uri.EscapeDataString(term)}" +
            "&media=music&entity=song&limit=1";

        using HttpResponseMessage response = await HttpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("results", out JsonElement results) ||
            results.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement track = results[0];

        string artistName = ReadString(track, "artistName");
        string trackName = ReadString(track, "trackName");
        string albumName = ReadString(track, "collectionName");
        string releaseDateRaw = ReadString(track, "releaseDate");
        string artworkUrl = ReadString(track, "artworkUrl100");

        return new MusicMetadataModel
        {
            ArtistName = artistName,
            TrackName = trackName,
            AlbumName = albumName,
            ReleaseDate = FormatReleaseDate(releaseDateRaw),
            ArtworkUrl = UpscaleArtwork(artworkUrl)
        };
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement value)
            ? value.GetString() ?? ""
            : "";
    }

    private static string FormatReleaseDate(string releaseDateRaw)
    {
        if (string.IsNullOrWhiteSpace(releaseDateRaw))
        {
            return "";
        }

        return DateTime.TryParse(releaseDateRaw, out DateTime parsed)
            ? parsed.ToString("MMMM d, yyyy")
            : releaseDateRaw;
    }

    private static string UpscaleArtwork(string artworkUrl100)
    {
        if (string.IsNullOrWhiteSpace(artworkUrl100))
        {
            return "";
        }

        // The API returns a 100x100 thumbnail URL; Apple serves larger sizes
        // from the same path if you swap the dimensions in the filename.
        return artworkUrl100.Replace("100x100bb", "600x600bb");
    }
}
