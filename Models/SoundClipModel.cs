namespace Wadevo.Models;

public sealed class SoundClipModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    // Local path under the Wadevo sound library folder, not the original import location -
    // files are copied in on import so a clip keeps working even if the source file (e.g.
    // something on the desktop or a USB drive) moves or gets deleted later.
    public string FilePath { get; set; } = "";

    public int Volume { get; set; } = 100;

    // Stored as Windows Forms Keys names (e.g. "D1", "NumPad1", "F5") rather than raw
    // key codes, so the settings file stays human-readable and stable across .NET versions.
    public string HotkeyModifiers { get; set; } = "";

    public string HotkeyKey { get; set; } = "";

    public bool HasHotkey => !string.IsNullOrWhiteSpace(HotkeyKey);
}
