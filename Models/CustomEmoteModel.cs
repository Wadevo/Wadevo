namespace Wadevo.Models;

public sealed class CustomEmoteModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Shortcode { get; set; } = "";

    // Local path under the Wadevo emote library folder, not the original upload location -
    // same reasoning as the Soundboard's local file library: a shortcode should keep
    // working even if the original source image moves or gets deleted.
    public string FilePath { get; set; } = "";

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}
