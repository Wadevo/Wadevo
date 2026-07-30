namespace Wadevo.Services.Obs;

using System.Text.Json;
using Wadevo.Models;

public sealed class ObsSceneHotkeyStore
{
    private readonly string _filePath;

    public ObsSceneHotkeyStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "obs-scene-hotkeys.json");
    }

    public List<ObsSceneHotkeyModel> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<ObsSceneHotkeyModel>();
            }

            string json = File.ReadAllText(_filePath);
            List<ObsSceneHotkeyModel>? bindings = JsonSerializer.Deserialize<List<ObsSceneHotkeyModel>>(json);

            return bindings ?? new List<ObsSceneHotkeyModel>();
        }
        catch
        {
            return new List<ObsSceneHotkeyModel>();
        }
    }

    public void Save(List<ObsSceneHotkeyModel> bindings)
    {
        try
        {
            string json = JsonSerializer.Serialize(bindings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
