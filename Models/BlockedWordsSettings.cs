namespace Wadevo.Models;

public sealed class BlockedWordsSettings
{
    public List<string> Words { get; set; } = new();

    public bool MuteOnBlock { get; set; }

    public bool IsEnabled { get; set; } = true;
}
