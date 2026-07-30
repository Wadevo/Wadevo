namespace Wadevo.Services;

using Wadevo.Models;

public sealed class DashboardStatsService
{
    private const int MaxActivityEntries = 50;

    private readonly DashboardStatsStore _store = new();
    private readonly List<string> _recentActivity = new();

    public event EventHandler? StatsChanged;

    public DashboardStatsModel GetStats()
    {
        return _store.Load();
    }

    public IReadOnlyList<string> GetRecentActivity()
    {
        return _recentActivity.ToList();
    }

    public void RecordFollow() => Increment(s => s.FollowCount++, "New follower");

    public void RecordSubscribe() => Increment(s => s.SubscribeCount++, "New subscription");

    public void RecordGiftSub() => Increment(s => s.GiftSubCount++, "Gift subs");

    public void RecordRaid() => Increment(s => s.RaidCount++, "Raid received");

    public void RecordVote() => Increment(s => s.VoteCount++, "Vote cast");

    public void RecordVip() => Increment(s => s.VipCount++, "New VIP");

    public void RecordCommandExecuted(string commandName) =>
        Increment(s => s.CommandsExecutedCount++, $"Command used: {commandName}");

    public void RecordSongRequest(string songText) =>
        Increment(s => s.SongRequestsCount++, $"Song requested: {songText}");

    public void RecordChatMessage() => Increment(s => s.ChatMessagesCount++, null);

    private void Increment(Action<DashboardStatsModel> apply, string? activityText)
    {
        DashboardStatsModel stats = _store.Load();

        apply(stats);

        _store.Save(stats);

        if (activityText is not null)
        {
            _recentActivity.Insert(0, $"{DateTime.Now:HH:mm} - {activityText}");

            while (_recentActivity.Count > MaxActivityEntries)
            {
                _recentActivity.RemoveAt(_recentActivity.Count - 1);
            }
        }

        StatsChanged?.Invoke(this, EventArgs.Empty);
    }
}
