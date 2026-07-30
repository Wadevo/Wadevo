namespace Wadevo.Services.Gifs;

using Wadevo.Models;

public interface IGifProvider
{
    string Name { get; }

    Task<List<GifSearchResultModel>> SearchAsync(
        string query,
        string apiKey,
        int limit,
        CancellationToken cancellationToken);

    Task<List<GifSearchResultModel>> GetTrendingAsync(
        string apiKey,
        int limit,
        CancellationToken cancellationToken);
}
