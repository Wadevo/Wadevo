namespace Wadevo.Models;

public sealed class TwitchChannelStatsModel
{
    public bool IsLive { get; set; }

    public int ViewerCount { get; set; }

    public int FollowerCount { get; set; }

    public int SubscriberCount { get; set; }

    public string Title { get; set; } = "";

    public string CategoryName { get; set; } = "";
}
