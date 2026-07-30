namespace Wadevo.Services.Soundboard;

/// <summary>
/// Converts between the stored string form of a hotkey (used in SoundClipModel/JSON) and
/// the strongly-typed Keys/HotkeyModifiers used by GlobalHotkeyService, plus a friendly
/// display string like "Ctrl+Alt+F1" for the UI.
/// </summary>
public static class HotkeyFormatting
{
    public static string FormatModifiers(GlobalHotkeyService.HotkeyModifiers modifiers)
    {
        List<string> parts = new();

        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Control)) parts.Add("Control");
        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Win)) parts.Add("Win");

        return string.Join(",", parts);
    }

    public static GlobalHotkeyService.HotkeyModifiers ParseModifiers(string stored)
    {
        GlobalHotkeyService.HotkeyModifiers result = GlobalHotkeyService.HotkeyModifiers.None;

        if (string.IsNullOrWhiteSpace(stored))
        {
            return result;
        }

        foreach (string part in stored.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.Trim())
            {
                case "Control": result |= GlobalHotkeyService.HotkeyModifiers.Control; break;
                case "Alt": result |= GlobalHotkeyService.HotkeyModifiers.Alt; break;
                case "Shift": result |= GlobalHotkeyService.HotkeyModifiers.Shift; break;
                case "Win": result |= GlobalHotkeyService.HotkeyModifiers.Win; break;
            }
        }

        return result;
    }

    /// <summary>
    /// Friendly display text, e.g. "Ctrl+Alt+F1" or "No hotkey set".
    /// </summary>
    public static string DisplayText(string storedModifiers, string storedKey)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
        {
            return "No hotkey set";
        }

        GlobalHotkeyService.HotkeyModifiers modifiers = ParseModifiers(storedModifiers);
        List<string> parts = new();

        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(GlobalHotkeyService.HotkeyModifiers.Win)) parts.Add("Win");

        if (Enum.TryParse(storedKey, out Keys key))
        {
            parts.Add(KeyDisplayName(key));
        }
        else
        {
            parts.Add(storedKey);
        }

        return string.Join("+", parts);
    }

    private static string KeyDisplayName(Keys key)
    {
        return key switch
        {
            >= Keys.NumPad0 and <= Keys.NumPad9 => $"Num{key - Keys.NumPad0}",
            >= Keys.D0 and <= Keys.D9 => $"{key - Keys.D0}",
            _ => key.ToString()
        };
    }
}
