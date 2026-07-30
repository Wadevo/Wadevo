namespace Wadevo.Models;

public sealed class ChatOverlaySettings
{
    public bool ShowBlaze { get; set; } = true;

    public bool ShowTwitch { get; set; } = true;

    public int MaxVisibleMessages { get; set; } = 12;

    public string FontFamily { get; set; } = "Segoe UI";

    public int FontSizePx { get; set; } = 14;

    public string TextColorHex { get; set; } = "#F2F7FB";

    public string BubbleBackgroundHex { get; set; } = "#060A10";

    public int BubbleOpacityPercent { get; set; } = 72;

    public bool ShowPlatformLabel { get; set; } = true;

    // "left" or "right" - which side new messages enter from and stack toward.
    public string Alignment { get; set; } = "left";
}
