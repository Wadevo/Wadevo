namespace Wadevo.Models;

public sealed class ObsSceneHotkeyModel
{
    public string SceneName { get; set; } = "";

    public string HotkeyModifiers { get; set; } = "";

    public string HotkeyKey { get; set; } = "";

    public bool HasHotkey => !string.IsNullOrWhiteSpace(HotkeyKey);
}
