namespace Wadevo.Services.Platforms;

using Wadevo.Core;

public static class PlatformRegistry
{
    // Ordered deliberately: implemented platforms first (in the order they were added to
    // Wadevo), then planned-but-not-yet-connected ones. UI code that lists platforms
    // should iterate this in order rather than re-deciding an order of its own.
    public static IReadOnlyList<PlatformDescriptor> All { get; } = new List<PlatformDescriptor>
    {
        new()
        {
            Platform = CommandSourcePlatform.Blaze,
            Name = "Blaze",
            TriggerPrefix = "blaze",
            Glyph = "🔥",
            AccentColor = WadevoTheme.Colors.Warning,
            IsImplemented = true,
            AlertEvents = new List<PlatformAlertEvent>
            {
                new("follow", "👋", "Follower"),
                new("raid", "🎉", "Raid"),
                new("subscribe", "⭐", "Subscribe"),
                new("gift", "🎁", "Gift Subs"),
                new("vote", "🗳", "Vote"),
                new("vip", "👑", "VIP"),
                new("chat", "💬", "Chat")
            }
        },
        new()
        {
            Platform = CommandSourcePlatform.Twitch,
            Name = "Twitch",
            TriggerPrefix = "twitch",
            Glyph = "🟣",
            AccentColor = WadevoTheme.Colors.Purple,
            IsImplemented = true,
            AlertEvents = new List<PlatformAlertEvent>
            {
                new("follow", "👋", "Follower"),
                new("raid", "🎉", "Raid"),
                new("subscribe", "⭐", "Subscribe"),
                new("gift", "🎁", "Gift Subs"),
                new("cheer", "💎", "Cheer"),
                new("chat", "💬", "Chat")
            }
        },
        new()
        {
            Platform = CommandSourcePlatform.Kick,
            Name = "Kick",
            TriggerPrefix = "kick",
            Glyph = "🟢",
            AccentColor = WadevoTheme.Colors.Success,
            IsImplemented = false
        },
        new()
        {
            Platform = CommandSourcePlatform.TikTok,
            Name = "TikTok",
            TriggerPrefix = "tiktok",
            Glyph = "⬛",
            AccentColor = WadevoTheme.Colors.Text,
            IsImplemented = false
        }
    };

    public static IEnumerable<PlatformDescriptor> Implemented => All.Where(p => p.IsImplemented);

    public static PlatformDescriptor Get(CommandSourcePlatform platform)
    {
        return All.First(p => p.Platform == platform);
    }

    // Replaces the two hand-copied trigger lists that used to live in AlertsModule
    // (filter chips) and AlertStudioControl (the event trigger dropdown) - both now
    // generate their list from here, so a platform's triggers can never drift out of
    // sync between the two pages again.
    public static IEnumerable<(string Trigger, string Icon, string Label)> AllAlertTriggers()
    {
        foreach (PlatformDescriptor platform in Implemented)
        {
            foreach (PlatformAlertEvent alertEvent in platform.AlertEvents)
            {
                yield return (
                    platform.GetTriggerName(alertEvent.EventName),
                    alertEvent.Icon,
                    $"{platform.Name} {alertEvent.Label}");
            }
        }
    }

    // Trigger names are namespaced (e.g. "twitch.follow") - this is the one place that
    // knowledge gets turned into a PlatformDescriptor, replacing the various hand-rolled
    // "StartsWith(\"twitch.\")" checks scattered across Commands/Alerts/Overlay Designer.
    public static PlatformDescriptor? GetByTriggerName(string triggerName)
    {
        if (string.IsNullOrWhiteSpace(triggerName))
        {
            return null;
        }

        return All.FirstOrDefault(p => p.OwnsTriggerName(triggerName));
    }
}
