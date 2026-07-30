namespace Wadevo.Models;

public sealed class WorkspacePanelInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string PanelType { get; set; } = "";

    public int X { get; set; } = 20;

    public int Y { get; set; } = 20;

    public int Width { get; set; } = 320;

    public int Height { get; set; } = 260;
}

public sealed class WorkspaceLayoutModel
{
    public bool AlwaysOnTop { get; set; } = true;

    public List<WorkspacePanelInstance> Panels { get; set; } = new();
}
