namespace Wadevo.Services.Gifs;

using System.Text.Json;
using Wadevo.Models;

public sealed class GifSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFile;

    public GifSettingsService()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _settingsFile = Path.Combine(folder, "gif-settings.json");

        if (!File.Exists(_settingsFile))
        {
            Save(new GifSettingsModel());
        }
    }

    public GifSettingsModel Load()
    {
        try
        {
            string json = File.ReadAllText(_settingsFile);

            GifSettingsModel? settings =
                JsonSerializer.Deserialize<GifSettingsModel>(
                    json,
                    JsonOptions);

            return settings ?? new GifSettingsModel();
        }
        catch
        {
            return new GifSettingsModel();
        }
    }

    public void Save(GifSettingsModel settings)
    {
        string json = JsonSerializer.Serialize(
            settings,
            JsonOptions);

        File.WriteAllText(_settingsFile, json);
    }
}
