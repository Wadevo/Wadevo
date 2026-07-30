namespace Wadevo.Services.Twitch;

// These identify the Wadevo application itself to Twitch - the same values are used by
// every person who runs Wadevo. They are NOT per-user; each user still logs into their
// own Twitch account separately when they click Connect.
//
// You probably don't need to edit this file directly. Instead, run Wadevo, go to the
// Twitch page, and click "Twitch App Setup" - that saves your credentials to a file in
// %AppData% that survives every future update, so you only ever do it once, and it never
// ends up committed to source control (important since a Client Secret is meant to stay
// private, especially in a public repo).
//
// These placeholders only matter as a fallback for a distributed build that has real
// values baked in before being shared with other people.
public static class TwitchAppCredentials
{
    public const string ClientId = "PASTE_YOUR_TWITCH_CLIENT_ID_HERE";

    public const string ClientSecret = "PASTE_YOUR_TWITCH_CLIENT_SECRET_HERE";
}
