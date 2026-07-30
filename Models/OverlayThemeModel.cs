namespace Wadevo.Models;

public sealed class OverlayThemeModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Overlay Theme";
    public string Description { get; set; } = "";
    public string AccentHex { get; set; } = "#45D9FF";
    public string BackgroundHex { get; set; } = "#071222";
    public string TextHex { get; set; } = "#E9F7FF";
    public string MutedTextHex { get; set; } = "#A9BAC8";
    public int BorderRadius { get; set; } = 24;
    public int Padding { get; set; } = 24;
    public int FontSize { get; set; } = 24;
    public bool ShowArtwork { get; set; } = true;
    public bool ShowLabel { get; set; } = true;
    public bool ShowProgressBar { get; set; } = false;
    public bool ShowGlow { get; set; } = true;

    public static OverlayThemeModel Minimal()
    {
        return new OverlayThemeModel
        {
            Id = "minimal",
            Name = "Minimal",
            Description = "Clean, compact, and readable.",
            AccentHex = "#45D9FF",
            BackgroundHex = "#071222",
            TextHex = "#E9F7FF",
            MutedTextHex = "#A9BAC8",
            BorderRadius = 20,
            Padding = 20,
            FontSize = 22,
            ShowArtwork = false,
            ShowLabel = true,
            ShowProgressBar = false,
            ShowGlow = false
        };
    }

    public static OverlayThemeModel Neon()
    {
        return new OverlayThemeModel
        {
            Id = "neon",
            Name = "Neon",
            Description = "Bright, glowing stream energy.",
            AccentHex = "#45D9FF",
            BackgroundHex = "#061222",
            TextHex = "#E9F7FF",
            MutedTextHex = "#A9BAC8",
            BorderRadius = 26,
            Padding = 24,
            FontSize = 24,
            ShowArtwork = true,
            ShowLabel = true,
            ShowProgressBar = true,
            ShowGlow = true
        };
    }

    public static OverlayThemeModel Glass()
    {
        return new OverlayThemeModel
        {
            Id = "glass",
            Name = "Glass",
            Description = "Soft transparent glass panel.",
            AccentHex = "#A78BFA",
            BackgroundHex = "#101827",
            TextHex = "#F5F3FF",
            MutedTextHex = "#C4B5FD",
            BorderRadius = 28,
            Padding = 24,
            FontSize = 24,
            ShowArtwork = true,
            ShowLabel = true,
            ShowProgressBar = false,
            ShowGlow = true
        };
    }

    public static OverlayThemeModel Vinyl()
    {
        return new OverlayThemeModel
        {
            Id = "vinyl",
            Name = "Vinyl",
            Description = "Warm music-focused overlay style.",
            AccentHex = "#FFBF57",
            BackgroundHex = "#1A1110",
            TextHex = "#FFF3DF",
            MutedTextHex = "#E9C891",
            BorderRadius = 24,
            Padding = 24,
            FontSize = 24,
            ShowArtwork = true,
            ShowLabel = true,
            ShowProgressBar = true,
            ShowGlow = true
        };
    }

    public static OverlayThemeModel Retro()
    {
        return new OverlayThemeModel
        {
            Id = "retro",
            Name = "Retro",
            Description = "Arcade-inspired streamer look.",
            AccentHex = "#FF5CCD",
            BackgroundHex = "#140A24",
            TextHex = "#FFEAFE",
            MutedTextHex = "#D9B4F4",
            BorderRadius = 18,
            Padding = 22,
            FontSize = 24,
            ShowArtwork = true,
            ShowLabel = true,
            ShowProgressBar = true,
            ShowGlow = true
        };
    }

    public static OverlayThemeModel Cyberpunk()
    {
        return new OverlayThemeModel
        {
            Id = "cyberpunk",
            Name = "Cyberpunk",
            Description = "Sharp, bold, high-contrast overlay.",
            AccentHex = "#00F5D4",
            BackgroundHex = "#050816",
            TextHex = "#F8FFFF",
            MutedTextHex = "#9FFFEF",
            BorderRadius = 16,
            Padding = 22,
            FontSize = 24,
            ShowArtwork = true,
            ShowLabel = true,
            ShowProgressBar = true,
            ShowGlow = true
        };
    }
}