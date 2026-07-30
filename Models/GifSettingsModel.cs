namespace Wadevo.Models;

public sealed class GifSettingsModel
{
    public string GiphyApiKey { get; set; } = "";

    public int DefaultDurationSeconds { get; set; } = 5;
}
