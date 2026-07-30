namespace Wadevo.Controls;

public sealed class WadevoDesignerPresetModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "Untitled Layout";

    public string OverlayType { get; set; } = "Song ID";

    public string BackgroundImagePath { get; set; } = "";

    public string BackgroundScaleMode { get; set; } = "Fill";

    public bool BackgroundRoundedCorners { get; set; } = true;

    public int BackgroundWidthPercent { get; set; } = 100;

    public int BackgroundHeightPercent { get; set; } = 100;

    public int BackgroundOpacityPercent { get; set; } = 100;

    public int BackgroundOffsetX { get; set; }

    public int BackgroundOffsetY { get; set; }

    public string AnimationType { get; set; } = "None";

    public int AnimationDurationMs { get; set; } = 500;

    public int AutoHideSeconds { get; set; } = 0;

    public bool AlwaysOn { get; set; } = false;

    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

    public List<WadevoDesignerElementState> Elements { get; set; } = new();
}
