namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class WadevoAppSettingsStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "app-settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public WadevoAppSettingsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    public WadevoAppSettingsModel Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new WadevoAppSettingsModel();
            }

            string json = File.ReadAllText(_filePath);
            WadevoAppSettingsModel? settings = JsonSerializer.Deserialize<WadevoAppSettingsModel>(json, JsonOptions);

            return settings ?? new WadevoAppSettingsModel();
        }
        catch
        {
            return new WadevoAppSettingsModel();
        }
    }

    public void Save(WadevoAppSettingsModel settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never break the app.
        }
    }
}
