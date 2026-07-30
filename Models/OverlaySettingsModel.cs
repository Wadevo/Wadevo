namespace Wadevo.Models;

public sealed class OverlaySettingsModel
{
    public string SelectedThemeId { get; set; } = "neon";
    public string NowPlayingTitle { get; set; } = "Song ID";
    public string SampleArtist { get; set; } = "Wadevo";
    public string SampleSong { get; set; } = "Builder Challenge Mode";

    public string AlertTitle { get; set; } = "🚨 Test Alert";
    public string AlertMessage { get; set; } = "This is a Wadevo test alert.";
    public string CommandTitle { get; set; } = "💬 Test Command";
    public string CommandMessage { get; set; } = "This is a Wadevo test command overlay event.";

    public int AlertDurationMilliseconds { get; set; } = 4200;
    public int CommandDurationMilliseconds { get; set; } = 4200;
}