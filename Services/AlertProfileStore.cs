namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class AlertProfileStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "alert-profiles.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public AlertProfileStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    public List<AlertProfileModel> LoadAll()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<AlertProfileModel>();
            }

            string json = File.ReadAllText(_filePath);
            List<AlertProfileModel>? profiles = JsonSerializer.Deserialize<List<AlertProfileModel>>(json, JsonOptions);

            return profiles ?? new List<AlertProfileModel>();
        }
        catch
        {
            return new List<AlertProfileModel>();
        }
    }

    public AlertProfileModel SaveNew(AlertProfileModel profile)
    {
        List<AlertProfileModel> profiles = LoadAll();
        profiles.Add(profile);
        SaveAll(profiles);

        return profile;
    }

    public bool Update(AlertProfileModel profile)
    {
        List<AlertProfileModel> profiles = LoadAll();
        int index = profiles.FindIndex(item => item.Id == profile.Id);

        if (index < 0)
        {
            return false;
        }

        profiles[index] = profile;
        SaveAll(profiles);

        return true;
    }

    public bool Delete(string id)
    {
        List<AlertProfileModel> profiles = LoadAll();
        int removed = profiles.RemoveAll(item => item.Id == id);

        if (removed > 0)
        {
            SaveAll(profiles);
        }

        return removed > 0;
    }

    private void SaveAll(List<AlertProfileModel> profiles)
    {
        try
        {
            string json = JsonSerializer.Serialize(profiles, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
