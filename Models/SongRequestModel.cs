namespace Wadevo.Models;

public sealed class SongRequestModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RequesterUsername { get; set; } = "";

    public string SongText { get; set; } = "";

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsPlayed { get; set; }
}
