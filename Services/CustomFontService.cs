namespace Wadevo.Services;

using System.Drawing.Text;
using System.Text.Json;

public static class CustomFontService
{
    private const string FolderName = "Wadevo";
    private const string FontsSubfolder = "CustomFonts";
    private const string ManifestFileName = "custom-fonts.json";

    private static readonly PrivateFontCollection FontCollection = new();
    private static readonly List<string> CustomFontNames = new();
    private static readonly List<CustomFontEntry> _manifestEntries = new();
    private static bool _isLoaded;

    // Common system fonts that are safe to assume exist on Windows, offered alongside
    // whatever custom fonts the user has uploaded.
    private static readonly string[] BuiltInFonts =
    {
        "Segoe UI", "Arial", "Verdana", "Georgia", "Times New Roman",
        "Consolas", "Courier New", "Impact", "Comic Sans MS", "Trebuchet MS"
    };

    public static IReadOnlyList<string> GetAllFontNames()
    {
        EnsureLoaded();

        // The actual fonts installed on this Windows install, not just a small hardcoded
        // guess at what's "probably" there - this is the real answer to "can the font
        // list come from my Windows fonts folder."
        IEnumerable<string> installedFonts = System.Drawing.FontFamily.Families
            .Select(family => family.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name));

        return installedFonts
            .Concat(BuiltInFonts)
            .Concat(CustomFontNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> GetCustomFontNames()
    {
        EnsureLoaded();

        return CustomFontNames.ToList();
    }

    public static bool TryGetCustomFontFilePath(string fontName, out string? filePath)
    {
        EnsureLoaded();

        CustomFontEntry? entry = _manifestEntries.FirstOrDefault(e =>
            string.Equals(e.FontName, fontName, StringComparison.OrdinalIgnoreCase));

        filePath = entry?.FilePath;

        return filePath is not null;
    }

    // Returns the installed font's family name on success, or null if the file couldn't be
    // used as a font (wrong format, corrupted, etc).
    public static string? AddFontFromFile(string sourcePath)
    {
        EnsureLoaded();

        try
        {
            if (!File.Exists(sourcePath))
            {
                return null;
            }

            string extension = Path.GetExtension(sourcePath);
            string fontsFolder = GetFontsFolder();
            string destinationFileName = $"{Guid.NewGuid():N}{extension}";
            string destinationPath = Path.Combine(fontsFolder, destinationFileName);

            File.Copy(sourcePath, destinationPath, overwrite: true);

            int countBefore = FontCollection.Families.Length;
            FontCollection.AddFontFile(destinationPath);

            if (FontCollection.Families.Length <= countBefore)
            {
                // The file didn't actually add a usable font - clean up and bail.
                File.Delete(destinationPath);
                return null;
            }

            string fontName = FontCollection.Families[^1].Name;

            if (!CustomFontNames.Contains(fontName))
            {
                CustomFontNames.Add(fontName);
            }

            CustomFontEntry newEntry = new() { FilePath = destinationPath, FontName = fontName };
            _manifestEntries.RemoveAll(e => string.Equals(e.FontName, fontName, StringComparison.OrdinalIgnoreCase));
            _manifestEntries.Add(newEntry);

            SaveManifest(destinationPath, fontName);

            return fontName;
        }
        catch
        {
            return null;
        }
    }

    // Custom-uploaded fonts need to come from this collection specifically, not
    // FontFamily.Families - system font lookup won't know about them.
    public static bool TryGetCustomFontFamily(string fontName, out FontFamily? family)
    {
        EnsureLoaded();

        family = FontCollection.Families.FirstOrDefault(f =>
            string.Equals(f.Name, fontName, StringComparison.OrdinalIgnoreCase));

        return family is not null;
    }

    public static Font CreateFont(string fontName, float size, FontStyle style)
    {
        if (TryGetCustomFontFamily(fontName, out FontFamily? family) && family is not null)
        {
            return new Font(family, size, style);
        }

        return new Font(fontName, size, style);
    }

    private static void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;

        try
        {
            string manifestPath = GetManifestPath();

            if (!File.Exists(manifestPath))
            {
                return;
            }

            string json = File.ReadAllText(manifestPath);
            List<CustomFontEntry>? entries = JsonSerializer.Deserialize<List<CustomFontEntry>>(json);

            if (entries is null)
            {
                return;
            }

            foreach (CustomFontEntry entry in entries)
            {
                if (!File.Exists(entry.FilePath))
                {
                    continue;
                }

                try
                {
                    FontCollection.AddFontFile(entry.FilePath);

                    if (!CustomFontNames.Contains(entry.FontName))
                    {
                        CustomFontNames.Add(entry.FontName);
                        _manifestEntries.Add(entry);
                    }
                }
                catch
                {
                    // Skip fonts that fail to load rather than breaking the whole list.
                }
            }
        }
        catch
        {
            // A broken manifest should never crash the app - just start with no custom fonts.
        }
    }

    private static void SaveManifest(string filePath, string fontName)
    {
        try
        {
            string manifestPath = GetManifestPath();

            List<CustomFontEntry> entries = new();

            if (File.Exists(manifestPath))
            {
                string existingJson = File.ReadAllText(manifestPath);
                List<CustomFontEntry>? existing = JsonSerializer.Deserialize<List<CustomFontEntry>>(existingJson);

                if (existing is not null)
                {
                    entries = existing;
                }
            }

            entries.Add(new CustomFontEntry { FilePath = filePath, FontName = fontName });

            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, json);
        }
        catch
        {
            // Persistence failing shouldn't stop the font from working for this session.
        }
    }

    private static string GetFontsFolder()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName, FontsSubfolder);

        Directory.CreateDirectory(folderPath);

        return folderPath;
    }

    private static string GetManifestPath()
    {
        return Path.Combine(GetFontsFolder(), ManifestFileName);
    }

    private sealed class CustomFontEntry
    {
        public string FilePath { get; set; } = "";
        public string FontName { get; set; } = "";
    }
}
