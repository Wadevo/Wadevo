namespace Wadevo.Services.Twitch;

public enum TwitchEventType
{
    Unknown = 0,
    Connected = 1,
    Disconnected = 2,
    ChatMessage = 3,
    Follow = 4,
    Raid = 5,
    Error = 6,
    Subscribe = 7,
    GiftSub = 8,
    Cheer = 9,
    StreamOnline = 10,
    StreamOffline = 11
}
