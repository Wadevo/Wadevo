namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class WorkspaceLayoutStore
{
    private readonly string _filePath;

    public WorkspaceLayoutStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "workspace-layout.json");
    }

    public WorkspaceLayoutModel Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new WorkspaceLayoutModel();
            }

            string json = File.ReadAllText(_filePath);
            WorkspaceLayoutModel? layout = JsonSerializer.Deserialize<WorkspaceLayoutModel>(json);

            return layout ?? new WorkspaceLayoutModel();
        }
        catch
        {
            return new WorkspaceLayoutModel();
        }
    }

    public void Save(WorkspaceLayoutModel layout)
    {
        try
        {
            string json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
