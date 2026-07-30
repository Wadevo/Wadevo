namespace Wadevo.Models;

public sealed class BlazeLiveStatsModel
{
    public bool IsLive { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public int NewFollowerCount { get; set; }

    public int NewSubscriberCount { get; set; }

    public int ViewerCount { get; set; }
}
