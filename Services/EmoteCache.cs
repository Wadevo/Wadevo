namespace Wadevo.Services;

using Wadevo.Models;

/// <summary>
/// The overlay HTTP handler (OverlayServer) is synchronous - it can't await a network call
/// mid-request without a larger restructuring. This keeps a background-refreshed snapshot
/// of the combined emote list (BTTV + custom) so rendering can read it synchronously, same
/// idea as how other cached lookups work elsewhere in the app.
/// </summary>
public static class EmoteCache
{
    private static readonly EmoteLibraryService LibraryService = new();
    private static IReadOnlyDictionary<string, EmoteModel> _emotesByShortcode =
        new Dictionary<string, EmoteModel>(StringComparer.OrdinalIgnoreCase);

    public static async Task RefreshAsync()
    {
        try
        {
            IReadOnlyList<EmoteModel> emotes = await LibraryService.GetAllEmotesAsync();

            _emotesByShortcode = emotes.ToDictionary(
                e => e.Shortcode,
                e => e,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Emote cache refresh failed: {ex.Message}");
        }
    }

    public static IReadOnlyDictionary<string, EmoteModel> GetSnapshot()
    {
        return _emotesByShortcode;
    }
}
