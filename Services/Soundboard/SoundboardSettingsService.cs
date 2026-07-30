namespace Wadevo.Services.Soundboard;

using System.Text.Json;
using Wadevo.Models;

public sealed class SoundboardSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFile;

    public SoundboardSettingsService()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _settingsFile = Path.Combine(folder, "soundboard-settings.json");

        if (!File.Exists(_settingsFile))
        {
            Save(new SoundboardSettingsModel());
        }
    }

    public SoundboardSettingsModel Load()
    {
        try
        {
            string json = File.ReadAllText(_settingsFile);

            SoundboardSettingsModel? settings =
                JsonSerializer.Deserialize<SoundboardSettingsModel>(
                    json,
                    JsonOptions);

            return settings ?? new SoundboardSettingsModel();
        }
        catch
        {
            return new SoundboardSettingsModel();
        }
    }

    public void Save(SoundboardSettingsModel settings)
    {
        string json = JsonSerializer.Serialize(
            settings,
            JsonOptions);

        File.WriteAllText(_settingsFile, json);
    }
}
