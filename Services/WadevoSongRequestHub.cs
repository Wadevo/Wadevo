namespace Wadevo.Services;

public static class WadevoSongRequestHub
{
    public static SongRequestService SongRequestService { get; } = new();
}
