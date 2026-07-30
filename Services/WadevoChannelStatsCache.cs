namespace Wadevo.Services;

using Wadevo.Models;
using Wadevo.Services.Blaze;
using Wadevo.Services.Twitch;

// Goal widgets (e.g. "Follower Goal: 28 / 50") need the streamer's real, current total -
// not "how many new follows happened since Wadevo opened", which is what
// DashboardStatsService.FollowCount actually tracks (correct for its own purpose, wrong
// for this one). This fetches and caches the real totals from every connected platform,
// refreshed periodically in the background, so the /goal-data endpoint (a synchronous,
// per-request HTTP handler) can read an already-known value instantly instead of making a
// live API call on every single poll from an overlay.
public static class WadevoChannelStatsCache
{
    private static readonly TwitchChannelService TwitchChannelService = new();
    private static readonly BlazeChannelService BlazeChannelService = new();

    private static System.Threading.Timer? _refreshTimer;
    private static readonly object RefreshLock = new();

    public static int TotalFollowerCount { get; private set; }

    public static int TotalSubscriberCount { get; private set; }

    public static int TwitchFollowerCount { get; private set; }

    public static int TwitchSubscriberCount { get; private set; }

    public static int BlazeFollowerCount { get; private set; }

    public static int BlazeSubscriberCount { get; private set; }

    public static void EnsureStarted()
    {
        lock (RefreshLock)
        {
            if (_refreshTimer is not null)
            {
                return;
            }

            _refreshTimer = new System.Threading.Timer(
                _ => _ = RefreshAsync(),
                null,
                dueTime: TimeSpan.Zero,
                period: TimeSpan.FromSeconds(60));
        }
    }

    private static async Task RefreshAsync()
    {
        int followerTotal = 0;
        int subscriberTotal = 0;

        TwitchAuthenticationService twitchAuth = TwitchAuthenticationService.Shared;

        if (twitchAuth.IsAuthenticated && !string.IsNullOrWhiteSpace(twitchAuth.Connection.UserId))
        {
            try
            {
                TwitchChannelStatsModel twitchStats = await TwitchChannelService.GetStatsAsync(
                    twitchAuth.Connection.AccessToken,
                    twitchAuth.Settings.ClientId,
                    twitchAuth.Connection.UserId);

                TwitchFollowerCount = twitchStats.FollowerCount;
                TwitchSubscriberCount = twitchStats.SubscriberCount;

                followerTotal += twitchStats.FollowerCount;
                subscriberTotal += twitchStats.SubscriberCount;
            }
            catch (Exception ex)
            {
                WadevoLogger.Warning($"Goal widget: Twitch stats refresh failed: {ex.Message}");
            }
        }
        else
        {
            TwitchFollowerCount = 0;
            TwitchSubscriberCount = 0;
        }

        BlazeAuthenticationService blazeAuth = BlazeAuthenticationService.Shared;

        if (blazeAuth.IsAuthenticated)
        {
            try
            {
                BlazeChannelStatsModel blazeStats = await BlazeChannelService.GetStatsAsync(
                    blazeAuth.Connection.AccessToken,
                    blazeAuth.Settings.ClientId,
                    blazeAuth.Settings.ClientSecret);

                BlazeFollowerCount = blazeStats.FollowerCount;
                BlazeSubscriberCount = blazeStats.SubscriberCount;

                followerTotal += blazeStats.FollowerCount;
                subscriberTotal += blazeStats.SubscriberCount;
            }
            catch (Exception ex)
            {
                WadevoLogger.Warning($"Goal widget: Blaze stats refresh failed: {ex.Message}");
            }
        }
        else
        {
            BlazeFollowerCount = 0;
            BlazeSubscriberCount = 0;
        }

        TotalFollowerCount = followerTotal;
        TotalSubscriberCount = subscriberTotal;
    }
}
