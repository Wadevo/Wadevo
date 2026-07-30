namespace Wadevo.Controls;

public sealed class WadevoDesignerElementState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "Element";

    public WadevoDesignerElementKind Kind { get; set; } = WadevoDesignerElementKind.Text;

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; } = 160;

    public int Height { get; set; } = 40;

    public string Text { get; set; } = "";

    public string FontFamily { get; set; } = "Segoe UI";

    public float FontSize { get; set; } = 14f;

    public bool FontBold { get; set; } = true;

    public int FontColorArgb { get; set; } = unchecked((int)0xFFFFFFFF);

    public string ArtworkUrl { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public DateTime CountdownTargetUtc { get; set; } = DateTime.UtcNow.AddHours(1);

    public string CountdownLabel { get; set; } = "";

    public string CountdownCompletedText { get; set; } = "🎉 It's time!";

    public string ClockFormat { get; set; } = "h:mm tt";

    // Each widget instance controls its own display cap now, rather than a single global
    // setting - so someone could, in principle, have a small "next up" widget and a larger
    // full-queue widget elsewhere on the same overlay if they wanted.
    public int SongQueueMaxVisible { get; set; } = 5;

    // Goal bar (reuses the ProgressBar kind, which previously existed but was never
    // actually wired up to anything - a static empty track with no real functionality).
    public string GoalMetric { get; set; } = "Followers";

    // "All" (sum every connected platform, the original/default behavior), "Blaze", or
    // "Twitch" - lets a goal widget target one specific platform's count instead of always
    // combining everything connected.
    public string GoalPlatform { get; set; } = "All";

    public int GoalTarget { get; set; } = 100;

    public int ProgressFillColorArgb { get; set; } = unchecked((int)0xFF45D9FF);

    public int ProgressTrackColorArgb { get; set; } = unchecked((int)0x33FFFFFF);

    public bool IsVisible { get; set; } = true;

    public bool IsLocked { get; set; }

    public Rectangle Bounds
    {
        get => new(X, Y, Width, Height);
        set
        {
            X = value.X;
            Y = value.Y;
            Width = value.Width;
            Height = value.Height;
        }
    }
}