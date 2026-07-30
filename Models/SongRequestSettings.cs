namespace Wadevo.Models;

public sealed class SongRequestSettings
{
    public bool IsEnabled { get; set; } = true;

    public string TriggerWord { get; set; } = "!sr";

    public int MaxQueueSize { get; set; } = 25;

    public string ConfirmationMessage { get; set; } = "🎵 Added \"{song}\" to the queue, {username}!";

    public bool PostConfirmationToChat { get; set; } = true;
}
