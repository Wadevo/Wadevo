namespace Wadevo.Services.Blaze;

/// <summary>
/// Blaze's API doesn't expose a languages endpoint (unlike Categories), so there's no
/// official list to fetch - this is a reasonable, commonly-used set of ISO 639-1 codes to
/// pick from instead of a bare, unlabeled text box that only ever defaults to "en". The
/// dropdown stays typeable, so a code not in this list can still be entered by hand.
/// </summary>
public static class BlazeLanguageOptions
{
    public static readonly (string Code, string Label)[] Common =
    {
        ("en", "English"),
        ("es", "Spanish"),
        ("pt", "Portuguese"),
        ("fr", "French"),
        ("de", "German"),
        ("it", "Italian"),
        ("nl", "Dutch"),
        ("pl", "Polish"),
        ("ru", "Russian"),
        ("tr", "Turkish"),
        ("ar", "Arabic"),
        ("hi", "Hindi"),
        ("ja", "Japanese"),
        ("ko", "Korean"),
        ("zh", "Chinese")
    };
}
