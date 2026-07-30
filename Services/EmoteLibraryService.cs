namespace Wadevo.Services;

using Wadevo.Models;

/// <summary>
/// Combines BTTV's global emotes with the user's own custom uploads into one list, used by
/// both the emote picker UI and the text renderer. Custom emotes always take priority over
/// a BTTV emote with the same shortcode, since a streamer's own upload is a more deliberate
/// choice than whatever happens to be in BTTV's global set.
/// </summary>
public sealed class EmoteLibraryService
{
    private readonly BttvEmoteService _bttvService = new();
    private readonly CustomEmoteService _customEmoteService = new();

    public async Task<IReadOnlyList<EmoteModel>> GetAllEmotesAsync(CancellationToken cancellationToken = default)
    {
        List<EmoteModel> customEmotes = _customEmoteService.LoadAll()
            .Select(custom => new EmoteModel
            {
                Shortcode = custom.Shortcode,
                ImageUrl = $"/media?path={Uri.EscapeDataString(custom.FilePath)}",
                IsAnimated = custom.FilePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase),
                IsCustom = true
            })
            .ToList();

        HashSet<string> customShortcodes = customEmotes
            .Select(e => e.Shortcode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<EmoteModel> bttvEmotes;

        try
        {
            bttvEmotes = await _bttvService.GetGlobalEmotesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"BTTV emote list failed to load: {ex.Message}");
            bttvEmotes = Array.Empty<EmoteModel>();
        }

        IEnumerable<EmoteModel> nonConflictingBttvEmotes = bttvEmotes
            .Where(e => !customShortcodes.Contains(e.Shortcode));

        return customEmotes
            .Concat(nonConflictingBttvEmotes)
            .OrderBy(e => e.Shortcode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
