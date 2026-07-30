namespace Wadevo.Services.Blaze;

using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Wadevo.Models;

/// <summary>
/// Wraps the Blaze Categories endpoint - used to populate a searchable category picker
/// for the Stream Info quick-edit panel, so switching category doesn't require leaving
/// Wadevo to look up the right category id on the website.
/// </summary>
public sealed class BlazeCategoryService
{
    private readonly HttpClient _httpClient = new();

    // Searches by name - this is what Blaze's own "Edit Stream Info" dialog actually uses
    // for its Game field (note it's a "Search..." box, not a pre-loaded dropdown, in the
    // screenshots this was built from). Also the far more reliable way to find a handful
    // of specifically-named directories than paginating through the whole catalog and
    // hoping they show up in whatever page happens to come back - two attempts at fetching
    // everything and filtering client-side both still only surfaced "Gambling & Casino" out
    // of a 100-row response, which points at something about how that catalog is structured
    // or paginated that browsing isn't reliably surfacing - searching by exact name
    // sidesteps that problem entirely.
    public async Task<IReadOnlyList<BlazeCategoryModel>> SearchCategoriesAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        string term,
        CancellationToken cancellationToken = default)
    {
        string url = $"https://api.blaze.stream/v1/categories?limit=50&term={Uri.EscapeDataString(term)}";

        using HttpRequestMessage httpMessage = new(HttpMethod.Get, url);

        httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpMessage.Headers.TryAddWithoutValidation("client-id", clientId);
        httpMessage.Headers.TryAddWithoutValidation("secret", clientSecret);

        using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blaze category search failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
        }

        BlazeCategoriesResponse? parsed =
            System.Text.Json.JsonSerializer.Deserialize<BlazeCategoriesResponse>(
                body,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        IReadOnlyList<BlazeCategoryRow> rows = parsed?.Data?.Rows ?? [];

        return rows
            .Select(row => new BlazeCategoryModel
            {
                Id = row.Id,
                ParentId = row.ParentId,
                Name = row.Name,
                Slug = row.Slug,
                ImageUrl = row.ImageUrl
            })
            .ToList();
    }

    public async Task<IReadOnlyList<BlazeCategoryModel>> GetCategoriesAsync(
        string accessToken,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        // There are only a handful of top-level directories (Game, Music, IRL, ...) but
        // potentially hundreds of individual games underneath "Game" alone. A single
        // 200-row page isn't guaranteed to include every directory - if the catalog is
        // sorted such that a run of games fills the page first, a directory can be pushed
        // past page 1 entirely and never appear at all. Paginating through every page (via
        // the cursor the API returns) is what actually guarantees the full set.
        List<BlazeCategoryModel> allCategories = new();
        string? cursor = null;

        // A hard cap, not an expected case - this is just to guarantee the loop can't spin
        // forever if the API ever returned a cursor that doesn't actually terminate.
        const int maxPages = 25;

        for (int page = 0; page < maxPages; page++)
        {
            string url = "https://api.blaze.stream/v1/categories?limit=200";

            if (!string.IsNullOrEmpty(cursor))
            {
                url += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            using HttpRequestMessage httpMessage = new(HttpMethod.Get, url);

            httpMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpMessage.Headers.TryAddWithoutValidation("client-id", clientId);
            httpMessage.Headers.TryAddWithoutValidation("secret", clientSecret);

            using HttpResponseMessage response = await _httpClient.SendAsync(httpMessage, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Blaze list categories failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
            }

            BlazeCategoriesResponse? parsed =
                System.Text.Json.JsonSerializer.Deserialize<BlazeCategoriesResponse>(
                    body,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            IReadOnlyList<BlazeCategoryRow> rows = parsed?.Data?.Rows ?? [];

            allCategories.AddRange(rows.Select(row => new BlazeCategoryModel
            {
                Id = row.Id,
                ParentId = row.ParentId,
                Name = row.Name,
                Slug = row.Slug,
                ImageUrl = row.ImageUrl
            }));

            cursor = parsed?.Data?.Pagination?.Cursor;

            if (string.IsNullOrEmpty(cursor) || rows.Count == 0)
            {
                break;
            }
        }

        return allCategories;
    }

    private sealed class BlazeCategoriesResponse
    {
        [JsonPropertyName("data")]
        public BlazeCategoriesData? Data { get; init; }
    }

    private sealed class BlazeCategoriesData
    {
        [JsonPropertyName("rows")]
        public List<BlazeCategoryRow> Rows { get; init; } = new();

        [JsonPropertyName("pagination")]
        public BlazeCategoriesPagination? Pagination { get; init; }
    }

    private sealed class BlazeCategoriesPagination
    {
        [JsonPropertyName("cursor")]
        public string? Cursor { get; init; }
    }

    private sealed class BlazeCategoryRow
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("parentId")]
        public int? ParentId { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("slug")]
        public string Slug { get; init; } = "";

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; init; } = "";
    }
}
