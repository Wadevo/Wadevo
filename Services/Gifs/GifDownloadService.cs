namespace Wadevo.Services.Gifs;

using Wadevo.Models;

public sealed class GifDownloadService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly string _cacheFolder;

    public GifDownloadService()
    {
        _cacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo",
            "GifCache");

        Directory.CreateDirectory(_cacheFolder);
    }

    public string CacheFolder => _cacheFolder;

    public async Task<string> DownloadAsync(
        GifSearchResultModel gif,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gif);

        if (string.IsNullOrWhiteSpace(gif.DownloadUrl))
        {
            throw new InvalidOperationException("This GIF has no download URL.");
        }

        string extension = GetExtension(gif.DownloadUrl);
        string safeId = MakeSafeFileName(gif.Id);
        string fileName = $"{gif.Source.ToLowerInvariant()}-{safeId}{extension}";
        string filePath = Path.Combine(_cacheFolder, fileName);

        if (File.Exists(filePath))
        {
            return filePath;
        }

        byte[] bytes = await HttpClient.GetByteArrayAsync(gif.DownloadUrl, cancellationToken);
        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

        return filePath;
    }

    public void ClearCache()
    {
        try
        {
            foreach (string file in Directory.GetFiles(_cacheFolder))
            {
                File.Delete(file);
            }
        }
        catch
        {
        }
    }

    private static string GetExtension(string url)
    {
        int queryIndex = url.IndexOf('?');
        string cleanUrl = queryIndex >= 0 ? url[..queryIndex] : url;
        string extension = Path.GetExtension(cleanUrl);

        return string.IsNullOrWhiteSpace(extension) ? ".gif" : extension;
    }

    private static string MakeSafeFileName(string id)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        char[] safeChars = id.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray();

        return new string(safeChars);
    }
}
