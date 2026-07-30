namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class OverlayThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _themeFilePath;

    public OverlayThemeService()
    {
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(appDataFolder);

        _themeFilePath = Path.Combine(appDataFolder, "overlay-themes.json");

        EnsureThemeFileExists();
    }

    public List<OverlayThemeModel> GetThemes()
    {
        try
        {
            string json = File.ReadAllText(_themeFilePath);

            List<OverlayThemeModel>? themes =
                JsonSerializer.Deserialize<List<OverlayThemeModel>>(json, JsonOptions);

            return themes is { Count: > 0 }
                ? themes
                : CreateDefaultThemes();
        }
        catch
        {
            return CreateDefaultThemes();
        }
    }

    public OverlayThemeModel GetTheme(string themeId)
    {
        return GetThemes().FirstOrDefault(theme => theme.Id == themeId)
            ?? OverlayThemeModel.Neon();
    }

    public void SaveThemes(List<OverlayThemeModel> themes)
    {
        string json = JsonSerializer.Serialize(themes, JsonOptions);
        File.WriteAllText(_themeFilePath, json);
    }

    public void SaveTheme(OverlayThemeModel theme)
    {
        List<OverlayThemeModel> themes = GetThemes();

        int existingIndex = themes.FindIndex(existingTheme => existingTheme.Id == theme.Id);

        if (existingIndex >= 0)
        {
            themes[existingIndex] = theme;
        }
        else
        {
            themes.Add(theme);
        }

        SaveThemes(themes);
    }

    private void EnsureThemeFileExists()
    {
        if (File.Exists(_themeFilePath))
        {
            return;
        }

        SaveThemes(CreateDefaultThemes());
    }

    private static List<OverlayThemeModel> CreateDefaultThemes()
    {
        return
        [
            OverlayThemeModel.Minimal(),
            OverlayThemeModel.Neon(),
            OverlayThemeModel.Glass(),
            OverlayThemeModel.Vinyl(),
            OverlayThemeModel.Retro(),
            OverlayThemeModel.Cyberpunk()
        ];
    }
}