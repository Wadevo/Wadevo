namespace Wadevo.Services.Platforms;

// Identifies a streaming platform Wadevo can integrate with. This is intentionally just
// identity/metadata - not behavior. Auth, event handling, chat sending, etc. still live in
// each platform's own services (BlazeAuthenticationService, TwitchAuthenticationService,
// and so on) - this is Stage 1 of consolidating the platform-specific duplication, not a
// full behavioral abstraction yet. See CommandSourcePlatform for the matching enum used
// throughout command/alert execution.
public sealed class PlatformDescriptor
{
    // Matches CommandSourcePlatform - kept as a separate concept for now since that enum
    // is baked into a lot of existing command-routing code; this links the two worlds.
    public required CommandSourcePlatform Platform { get; init; }

    // Display name, e.g. "Blaze", "Twitch".
    public required string Name { get; init; }

    // The lowercase prefix used in trigger names, e.g. "blaze", "twitch" (as in
    // "twitch.follow"). Every trigger-name-based lookup should go through this instead of
    // a hardcoded string literal.
    public required string TriggerPrefix { get; init; }

    // A short emoji/glyph used consistently across Connections, Alerts, and the Dashboard.
    public required string Glyph { get; init; }

    public required Color AccentColor { get; init; }

    // Whether this platform has a real, working connection/integration built - false for
    // placeholder/planned platforms that exist in the registry but aren't wired up yet.
    public bool IsImplemented { get; init; } = true;

    public string GetTriggerName(string eventName)
    {
        return $"{TriggerPrefix}.{eventName}";
    }

    public bool OwnsTriggerName(string triggerName)
    {
        return triggerName.StartsWith(TriggerPrefix + ".", StringComparison.OrdinalIgnoreCase);
    }

    // The alert-capable events this platform actually supports, in display order. Not
    // every platform supports every event (Blaze has Vote/VIP; Twitch has Cheer instead) -
    // this is what replaces the two hand-copied, easy-to-desync trigger lists that used to
    // live in AlertsModule and AlertStudioControl.
    public IReadOnlyList<PlatformAlertEvent> AlertEvents { get; init; } = Array.Empty<PlatformAlertEvent>();
}

public sealed record PlatformAlertEvent(string EventName, string Icon, string Label);
