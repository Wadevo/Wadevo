namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class DashboardStatsStore
{
    private readonly string _filePath;

    public DashboardStatsStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "dashboard-stats.json");
    }

    public DashboardStatsModel Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new DashboardStatsModel();
            }

            string json = File.ReadAllText(_filePath);
            DashboardStatsModel? stats = JsonSerializer.Deserialize<DashboardStatsModel>(json);

            if (stats is null || stats.Date != DateOnly.FromDateTime(DateTime.Now))
            {
                // A new day - start fresh rather than showing yesterday's numbers.
                return new DashboardStatsModel();
            }

            return stats;
        }
        catch
        {
            return new DashboardStatsModel();
        }
    }

    public void Save(DashboardStatsModel stats)
    {
        try
        {
            string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
