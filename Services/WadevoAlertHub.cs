namespace Wadevo.Services;

using Wadevo.Models;
using Wadevo.Services.Blaze;

public static class WadevoAlertHub
{
    public static AlertProfileService ProfileService { get; } = new();

    public static void TriggerPreview(AlertProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!profile.IsEnabled)
            return;

        // Sample data standing in for what a real Blaze event would carry, so a preview
        // renders the alert's actual designed text tokens ({username}, {message}, etc.)
        // with something readable instead of leaving them blank or showing raw braces.
        BlazeEventCommandContext previewContext = new()
        {
            TriggerName = profile.EventTrigger,
            Username = "DemoUser",
            Message = "This is a Wadevo preview alert.",
            Data = new Dictionary<string, object?>
            {
                ["viewerCount"] = 42,
                ["giftCount"] = 5,
                ["voteAmount"] = 100
            }
        };

        OverlayServer.TriggerAlert(profile, previewContext);
    }
}
