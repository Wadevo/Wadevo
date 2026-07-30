namespace Wadevo.Controls;

using System.Text.Json;

public sealed class WadevoDesignerDocumentStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "designer-document.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public WadevoDesignerDocumentStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    public WadevoDesignerDocument Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return CreateDefaultDocument();
            }

            string json = File.ReadAllText(_filePath);

            List<WadevoDesignerElementState>? elements =
                JsonSerializer.Deserialize<List<WadevoDesignerElementState>>(json, JsonOptions);

            WadevoDesignerDocument document = new();

            if (elements is null || elements.Count == 0)
            {
                return CreateDefaultDocument();
            }

            foreach (WadevoDesignerElementState element in elements)
            {
                document.Add(element);
            }

            return document;
        }
        catch
        {
            return CreateDefaultDocument();
        }
    }

    public void Save(WadevoDesignerDocument document)
    {
        try
        {
            string json = JsonSerializer.Serialize(document.Elements.ToList(), JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never break the designer.
        }
    }

    private static WadevoDesignerDocument CreateDefaultDocument()
    {
        WadevoDesignerDocument document = new();

        document.Add(new WadevoDesignerElementState
        {
            Name = "Preview Surface",
            Kind = WadevoDesignerElementKind.PreviewSurface,
            X = 90,
            Y = 90,
            Width = 420,
            Height = 120,
            Text = "Song ID"
        });

        return document;
    }
}