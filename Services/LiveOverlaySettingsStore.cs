namespace Wadevo.Services;

using System.Text.Json;

public sealed class LiveOverlaySettingsStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "live-overlay-settings.json";

    private readonly string _filePath;

    public LiveOverlaySettingsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    public string? GetLivePresetId()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string json = File.ReadAllText(_filePath);
            LiveOverlaySettings? settings = JsonSerializer.Deserialize<LiveOverlaySettings>(json);

            return string.IsNullOrWhiteSpace(settings?.LivePresetId) ? null : settings.LivePresetId;
        }
        catch
        {
            return null;
        }
    }

    public void SetLivePresetId(string? presetId)
    {
        try
        {
            LiveOverlaySettings settings = new() { LivePresetId = presetId ?? "" };
            string json = JsonSerializer.Serialize(settings);

            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never break the app - the selection just won't stick.
        }
    }

    private sealed class LiveOverlaySettings
    {
        public string LivePresetId { get; set; } = "";
    }
}
