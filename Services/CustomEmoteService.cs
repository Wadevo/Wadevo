namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

/// <summary>
/// Manages custom uploaded emotes - copies uploaded images into a dedicated Wadevo folder
/// (same reasoning as SoundLibraryService: a shortcode should keep working even if the
/// original source file gets moved or deleted) and persists the shortcode list as JSON.
/// </summary>
public sealed class CustomEmoteService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _libraryFolder;
    private readonly string _settingsFile;

    public CustomEmoteService()
    {
        string wadevoFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        _libraryFolder = Path.Combine(wadevoFolder, "Emotes");
        Directory.CreateDirectory(_libraryFolder);

        _settingsFile = Path.Combine(wadevoFolder, "custom-emotes.json");

        if (!File.Exists(_settingsFile))
        {
            Save(new List<CustomEmoteModel>());
        }
    }

    public List<CustomEmoteModel> LoadAll()
    {
        try
        {
            string json = File.ReadAllText(_settingsFile);
            List<CustomEmoteModel>? emotes = JsonSerializer.Deserialize<List<CustomEmoteModel>>(json, JsonOptions);
            return emotes ?? new List<CustomEmoteModel>();
        }
        catch
        {
            return new List<CustomEmoteModel>();
        }
    }

    public CustomEmoteModel Add(string shortcode, string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Emote image not found.", sourceFilePath);
        }

        string normalizedShortcode = NormalizeShortcode(shortcode);

        if (string.IsNullOrWhiteSpace(normalizedShortcode))
        {
            throw new ArgumentException("Shortcode can't be empty.", nameof(shortcode));
        }

        List<CustomEmoteModel> emotes = LoadAll();

        if (emotes.Any(e => string.Equals(e.Shortcode, normalizedShortcode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"An emote with the shortcode \"{normalizedShortcode}\" already exists.");
        }

        string extension = Path.GetExtension(sourceFilePath);
        string fileName = $"{Guid.NewGuid():N}{extension}";
        string destinationPath = Path.Combine(_libraryFolder, fileName);

        File.Copy(sourceFilePath, destinationPath, overwrite: false);

        CustomEmoteModel emote = new()
        {
            Shortcode = normalizedShortcode,
            FilePath = destinationPath
        };

        emotes.Insert(0, emote);
        Save(emotes);

        return emote;
    }

    public void Remove(string id)
    {
        List<CustomEmoteModel> emotes = LoadAll();
        CustomEmoteModel? emote = emotes.FirstOrDefault(e => e.Id == id);

        if (emote is null)
        {
            return;
        }

        emotes.Remove(emote);
        Save(emotes);

        try
        {
            if (File.Exists(emote.FilePath) && emote.FilePath.StartsWith(_libraryFolder, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(emote.FilePath);
            }
        }
        catch
        {
            // Best-effort - an orphaned file isn't worth surfacing an error for.
        }
    }

    // Shortcodes are matched against a strict pattern (letters/digits/underscore only) both
    // here and in the renderer, so a shortcode can never contain characters that would let
    // it interfere with the surrounding text or the :colon: delimiters themselves.
    public static string NormalizeShortcode(string shortcode)
    {
        string trimmed = (shortcode ?? "").Trim().Trim(':');
        return System.Text.RegularExpressions.Regex.IsMatch(trimmed, "^[a-zA-Z0-9_]+$") ? trimmed : "";
    }

    private void Save(List<CustomEmoteModel> emotes)
    {
        string json = JsonSerializer.Serialize(emotes, JsonOptions);
        File.WriteAllText(_settingsFile, json);
    }
}
