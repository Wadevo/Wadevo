using Wadevo.Models;

namespace Wadevo.Services;

public sealed class AlertProfileService
{
    private readonly AlertProfileStore _store = new();
    private readonly Dictionary<string, AlertProfileModel> _profiles =
        new(StringComparer.OrdinalIgnoreCase);

    // In-memory only, not persisted - a cooldown resetting on app restart is a reasonable
    // trade-off and matches how cooldowns already work elsewhere (e.g. Commands).
    private readonly Dictionary<string, DateTime> _lastFiredUtc =
        new(StringComparer.OrdinalIgnoreCase);

    public AlertProfileService()
    {
        Reload();
    }

    public IEnumerable<AlertProfileModel> Profiles => _profiles.Values;

    public void Reload()
    {
        _profiles.Clear();

        foreach (AlertProfileModel profile in _store.LoadAll())
        {
            // Keyed by EventTrigger for fast lookup during live Blaze events, but custom
            // alerts with no trigger still need a unique key to avoid colliding with each
            // other in this dictionary - fall back to Id for those.
            string key = string.IsNullOrWhiteSpace(profile.EventTrigger) ? profile.Id : profile.EventTrigger;
            _profiles[key] = profile;
        }
    }

    public AlertProfileModel GetProfile(string eventTrigger)
    {
        if (_profiles.TryGetValue(eventTrigger, out AlertProfileModel? profile))
            return profile;

        profile = new AlertProfileModel
        {
            EventTrigger = eventTrigger,
            Name = eventTrigger
        };

        _profiles[eventTrigger] = profile;

        return profile;
    }

    public bool IsOffCooldown(AlertProfileModel profile)
    {
        if (profile.CooldownSeconds <= 0)
        {
            return true;
        }

        if (!_lastFiredUtc.TryGetValue(profile.Id, out DateTime lastFired))
        {
            return true;
        }

        return (DateTime.UtcNow - lastFired).TotalSeconds >= profile.CooldownSeconds;
    }

    public void RecordFired(AlertProfileModel profile)
    {
        _lastFiredUtc[profile.Id] = DateTime.UtcNow;
    }

    public void SaveProfile(AlertProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(profile.EventTrigger))
            return;

        _profiles[profile.EventTrigger] = profile;

        if (!_store.Update(profile))
        {
            _store.SaveNew(profile);
        }
    }
}
