namespace Wadevo.Models;

public sealed class EmoteModel
{
    public string Shortcode { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public bool IsAnimated { get; set; }

    public bool IsCustom { get; set; }
}
