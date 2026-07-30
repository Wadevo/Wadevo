namespace Wadevo.Models;

public sealed class SoundboardSettingsModel
{
    public List<SoundClipModel> Clips { get; set; } = new();

    public int MasterVolume { get; set; } = 100;

    // Empty means "system default" - the same behavior Soundboard always had before this
    // setting existed.
    public string OutputDeviceName { get; set; } = "";
}
