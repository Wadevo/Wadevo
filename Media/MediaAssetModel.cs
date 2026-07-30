namespace Wadevo.Media.Models;

public sealed class MediaAssetModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty;

    public bool Favorite { get; set; }

    public DateTime ImportedUtc { get; set; }
        = DateTime.UtcNow;
}