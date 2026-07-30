namespace Wadevo.Models;

public class CommandModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string Trigger { get; set; } = "";

    // "Chat Trigger" (default) fires from a typed chat command like everything already
    // did. "Timer" fires automatically on a repeating interval instead - no chat trigger
    // word is used or matched against in that mode.
    public string TriggerMode { get; set; } = "Chat Trigger";

    public int IntervalMinutes { get; set; } = 30;

    // Null until the first time a Timer-mode command actually fires. Persisted (not just
    // tracked in memory) so a restart doesn't immediately re-fire everything that was
    // close to due, and so it doesn't forget progress toward the next interval.
    public DateTime? LastFiredAt { get; set; }

    public bool RequireExclamation { get; set; } = true;

    public string CommandKind { get; set; } = "Chat Message";

    public string Response { get; set; } = "";

    public string MediaFilePath { get; set; } = "";

    public int Width { get; set; } = 400;

    public int Height { get; set; } = 300;

    public int DurationSeconds { get; set; } = 5;

    public bool FadeIn { get; set; } = true;

    public bool FadeOut { get; set; } = true;

    public bool IsEnabled { get; set; } = true;

    // Lets a command be curated into the Workspace Studio's Quick Commands panel instead
    // of that panel always showing every single enabled command regardless of how many
    // there are - without this, the panel gave no way to choose what actually shows up.
    public bool ShowInQuickPanel { get; set; } = false;

    public int CooldownSeconds { get; set; }

    public string MinimumRole { get; set; } = "Everyone";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}