namespace Wadevo.Services;

using Wadevo.Models;

public static class CommandPermissionChecker
{
    public static bool MeetsMinimumRole(CommandModel command, CommandSenderContext? sender)
    {
        if (string.IsNullOrWhiteSpace(command.MinimumRole) ||
            command.MinimumRole.Equals("Everyone", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // No sender context means the command was triggered a non-chat way (a Blaze event,
        // a manual test click) - minimum role only makes sense to enforce against an actual
        // chatter, so anything else is allowed through.
        if (sender is null)
        {
            return true;
        }

        // "Owner" is deliberately excluded from the moderator bypass below - it's the one
        // tier that means "only me, the streamer," not "me and my mods." Every other tier
        // treats mods as automatically trusted; this one doesn't.
        if (command.MinimumRole.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            return sender.IsOwner;
        }

        // Moderators and the owner can use any other command regardless of its minimum role.
        if (sender.IsModerator || sender.IsOwner)
        {
            return true;
        }

        return command.MinimumRole switch
        {
            "Follower" => sender.IsFollower || sender.IsSubscriber,
            "Subscriber" => sender.IsSubscriber,
            "Moderator" => false,
            _ => true
        };
    }
}
