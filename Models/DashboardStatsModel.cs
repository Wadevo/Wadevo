namespace Wadevo.Models;

public sealed class DashboardStatsModel
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    public int FollowCount { get; set; }

    public int SubscribeCount { get; set; }

    public int GiftSubCount { get; set; }

    public int RaidCount { get; set; }

    public int VoteCount { get; set; }

    public int VipCount { get; set; }

    public int CommandsExecutedCount { get; set; }

    public int SongRequestsCount { get; set; }

    public int ChatMessagesCount { get; set; }
}
