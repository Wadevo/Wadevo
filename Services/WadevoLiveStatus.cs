namespace Wadevo.Services;

// A small shared status board. MainForm updates these as it does its real work
// (reading Serato, running the Overlay Engine); other pages like the Connections Hub
// just read them, without needing a direct reference back to MainForm.
public static class WadevoLiveStatus
{
    public static bool IsSeratoConnected { get; set; }

    public static bool IsVirtualDjConnected { get; set; }

    public static string CurrentSong { get; set; } = "";

    public static bool IsOverlayEngineRunning { get; set; }
}
