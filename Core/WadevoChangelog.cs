namespace Wadevo.Core;

public static class WadevoChangelog
{
    public static readonly ChangelogEntry[] Entries =
    {
        new("v0.3.0", new[]
        {
            "Overlay Designer: full redesign - saved overlay list, background image upload, per-element font/color styling, real Motion settings (entrance animation, duration, always-on)",
            "Alerts: rebuilt as its own tab with real persistence - create, save, and search unlimited custom alerts (previously only 3 fixed types existed and nothing was saved between restarts)",
            "The Song ID page (formerly its own separate page) is now a live control center: pick which saved overlay is broadcasting, see connection status, jump straight to editing",
            "The Song ID overlay switched from full-page reloads to live polling - Motion animations now actually play on the real OBS overlay when a song changes",
            "Custom font uploads - available in Overlay Designer and Alerts, rendered correctly on the live overlay",
            "Commands: type filters, disabled-only filter, and alphabetical sorting for large command lists",
            "Backup & Restore - export everything you've built to a single file, restore it on this or another machine",
            "Numerous bug fixes: GIF sizing, GIF-to-GIF transition bleed, double-click editing, Overlay Engine button clipping, and more"
        }),
        new("v0.2.0", new[]
        {
            "Blaze OAuth integration, chat, commands, and live event handling",
            "Command Studio with GIF/Image, Video Clip, Sound Effect, and Multi Action support",
            "GIF Studio with Giphy search",
            "Overlay Engine for managing OBS browser source URLs",
            "Getting Started walkthrough"
        })
    };
}

public sealed record ChangelogEntry(string Version, string[] Changes);
