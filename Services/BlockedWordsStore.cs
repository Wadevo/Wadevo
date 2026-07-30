namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class BlockedWordsStore
{
    private readonly string _filePath;

    public BlockedWordsStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "blocked-words.json");
    }

    public BlockedWordsSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new BlockedWordsSettings();
            }

            string json = File.ReadAllText(_filePath);
            BlockedWordsSettings? settings = JsonSerializer.Deserialize<BlockedWordsSettings>(json);

            return settings ?? new BlockedWordsSettings();
        }
        catch
        {
            return new BlockedWordsSettings();
        }
    }

    public void Save(BlockedWordsSettings settings)
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
