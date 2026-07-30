namespace Wadevo.Services;

public enum CommandSourcePlatform
{
    Unknown = 0,
    Blaze = 1,
    Twitch = 2,
    Kick = 3,
    TikTok = 5
}

public sealed class CommandSenderContext
{
    public string Username { get; init; } = "";

    public string ChatMessage { get; init; } = "";

    public bool IsSubscriber { get; init; }

    public bool IsFollower { get; init; }

    public bool IsModerator { get; init; }

    public bool IsOwner { get; init; }

    // Which platform this command execution originated from - determines where a "Chat
    // Message" reply action gets posted back to. Unknown/unset (e.g. a Timer-mode command
    // with no triggering platform at all) falls back to Blaze, preserving old behavior.
    public CommandSourcePlatform SourcePlatform { get; init; } = CommandSourcePlatform.Unknown;
}
