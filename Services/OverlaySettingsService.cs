namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class OverlaySettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFile;

    public OverlaySettingsService()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _settingsFile = Path.Combine(folder, "overlay-settings.json");

        if (!File.Exists(_settingsFile))
        {
            Save(new OverlaySettingsModel());
        }
    }

    public OverlaySettingsModel Load()
    {
        try
        {
            string json = File.ReadAllText(_settingsFile);

            OverlaySettingsModel? settings =
                JsonSerializer.Deserialize<OverlaySettingsModel>(
                    json,
                    JsonOptions);

            return settings ?? new OverlaySettingsModel();
        }
        catch
        {
            return new OverlaySettingsModel();
        }
    }

    public void Save(OverlaySettingsModel settings)
    {
        string json = JsonSerializer.Serialize(
            settings,
            JsonOptions);

        File.WriteAllText(_settingsFile, json);
    }

    public void Reset()
    {
        Save(new OverlaySettingsModel());
    }
}