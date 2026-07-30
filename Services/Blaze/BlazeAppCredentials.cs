namespace Wadevo.Services.Blaze;

// These identify the Wadevo application itself to Blaze - the same values are used by
// every person who runs Wadevo. They are NOT per-user; each user still logs into their
// own Blaze account separately when they click Connect.
//
// You probably don't need to edit this file directly. Instead, run Wadevo, go to the
// Blaze page, and click "Blaze App Setup" - that saves your credentials to a file in
// %AppData% that survives every future code update, so you only ever do it once.
//
// These placeholders only matter as a fallback for a distributed build that has real
// values baked in before being shared with other people.
public static class BlazeAppCredentials
{
    public const string ClientId = "PASTE_YOUR_BLAZE_CLIENT_ID_HERE";

    public const string ClientSecret = "PASTE_YOUR_BLAZE_CLIENT_SECRET_HERE";
}
