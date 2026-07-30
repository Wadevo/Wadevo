namespace Wadevo.Models;

public sealed class AlertProfileModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string EventTrigger { get; set; } = "";

    public string Name { get; set; } = "";

    // Appearance (background image/video/GIF, text, position, size, colors, fonts) is now
    // designed on a real canvas in the Overlay Designer instead of being a handful of
    // disconnected fields here with no way to actually size or position anything - this
    // just points at that design. Null/empty means "not designed yet".
    public string? LinkedOverlayPresetId { get; set; }

    public int CooldownSeconds { get; set; }

    public int DurationMilliseconds { get; set; } = 4200;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
