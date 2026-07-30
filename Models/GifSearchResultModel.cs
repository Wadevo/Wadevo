namespace Wadevo.Models;

public sealed class GifSearchResultModel
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Source { get; set; } = "";

    public string PreviewUrl { get; set; } = "";

    public string DownloadUrl { get; set; } = "";

    public int Width { get; set; }

    public int Height { get; set; }
}
