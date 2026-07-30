namespace Wadevo.Services.Blaze;

public static class BlazeEventSubscriptions
{
    public const string Follow = "channel.follow";
    public const string Raid = "channel.raid";
    public const string ChatMessage = "channel.chat.message";

    public const string Subscribe = "channel.subscribe";
    public const string GiftSubscription = "channel.subscription.gift";
    public const string Thanks = "channel.thanks";
    public const string Vote = "channel.vote";

    public const string StreamOnline = "stream.online";
    public const string StreamOffline = "stream.offline";

    public const string ChatClear = "channel.chat.clear";
    public const string ChatMessageDelete = "channel.chat.message_delete";

    public const string Moderate = "channel.moderate";
    public const string Ban = "channel.ban";
    public const string Unban = "channel.unban";

    public const string ModeratorAdd = "channel.moderator.add";
    public const string ModeratorRemove = "channel.moderator.remove";

    public const string VipAdd = "channel.vip.add";
    public const string VipRemove = "channel.vip.remove";

    public const string OgAdd = "channel.og.add";
    public const string OgRemove = "channel.og.remove";

    public const string ChannelUpdate = "channel.update";
    public const string UserUpdate = "user.update";
}