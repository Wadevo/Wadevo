namespace Wadevo.Controls;

using System.Text.Json;

public sealed class WadevoDesignerCanvasStateStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "designer-canvas-state.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public WadevoDesignerCanvasStateStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    public WadevoDesignerCanvasState Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new WadevoDesignerCanvasState();
            }

            string json = File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize<WadevoDesignerCanvasState>(json, JsonOptions)
                ?? new WadevoDesignerCanvasState();
        }
        catch
        {
            return new WadevoDesignerCanvasState();
        }
    }

    public void Save(WadevoDesignerCanvasState state)
    {
        try
        {
            string json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never break the designer.
        }
    }
}