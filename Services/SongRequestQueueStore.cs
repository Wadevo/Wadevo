namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class SongRequestQueueStore
{
    private readonly string _filePath;

    public SongRequestQueueStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "song-request-queue.json");
    }

    public List<SongRequestModel> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<SongRequestModel>();
            }

            string json = File.ReadAllText(_filePath);
            List<SongRequestModel>? queue = JsonSerializer.Deserialize<List<SongRequestModel>>(json);

            return queue ?? new List<SongRequestModel>();
        }
        catch
        {
            return new List<SongRequestModel>();
        }
    }

    public void Save(List<SongRequestModel> queue)
    {
        try
        {
            string json = JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
