namespace Wadevo.Controls;

public sealed class WadevoOverlayStyleSettings
{
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
}
