namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class SongRequestSettingsStore
{
    private readonly string _filePath;

    public SongRequestSettingsStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "song-request-settings.json");
    }

    public SongRequestSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new SongRequestSettings();
            }

            string json = File.ReadAllText(_filePath);
            SongRequestSettings? settings = JsonSerializer.Deserialize<SongRequestSettings>(json);

            return settings ?? new SongRequestSettings();
        }
        catch
        {
            return new SongRequestSettings();
        }
    }

    public void Save(SongRequestSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
