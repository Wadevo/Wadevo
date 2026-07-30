namespace Wadevo.Services.Twitch;

using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Blaze;

public sealed class TwitchEventExecutionService
{
    public IReadOnlyList<CommandExecutionResult> Execute(TwitchEvent twitchEvent)
    {
        ArgumentNullException.ThrowIfNull(twitchEvent);

        List<CommandExecutionResult> executedCommands = new();

        string triggerName = GetTriggerName(twitchEvent);

        BlazeEventCommandContext context = new()
        {
            TriggerName = triggerName,
            EventType = BlazeEventType.Unknown, // Twitch events don't map onto Blaze's enum - not read by the formatter/alert renderer, only Username/Message/Data are.
            Username = twitchEvent.Username ?? "",
            Message = twitchEvent.Message ?? "",
            TimestampUtc = twitchEvent.TimestampUtc,
            Data = BuildDataDictionary(twitchEvent)
        };

        if (twitchEvent.EventType == TwitchEventType.ChatMessage)
        {
            WadevoDashboardHub.StatsService.RecordChatMessage();

            if (TryHandleBlockedWord(twitchEvent))
            {
                return executedCommands;
            }

            OverlayServer.AddChatMessage(CommandSourcePlatform.Twitch, twitchEvent.Username ?? "Someone", twitchEvent.Message ?? "");

            if (TryHandleSongRequest(twitchEvent))
            {
                return executedCommands;
            }

            IReadOnlyList<CommandExecutionResult> chatCommandResults = ExecuteChatCommand(twitchEvent);

            foreach (CommandExecutionResult result in chatCommandResults)
            {
                WadevoDashboardHub.StatsService.RecordCommandExecuted(result.Command.Name);
            }

            executedCommands.AddRange(chatCommandResults);

            return executedCommands;
        }

        if (ShouldExecuteCommand(triggerName))
        {
            IReadOnlyList<CommandExecutionResult> triggeredCommandResults =
                WadevoCommandHub.ExecutionService.ExecuteTrigger(triggerName);

            foreach (CommandExecutionResult result in triggeredCommandResults)
            {
                WadevoDashboardHub.StatsService.RecordCommandExecuted(result.Command.Name);
            }

            executedCommands.AddRange(triggeredCommandResults);
        }

        AlertProfileModel profile = WadevoAlertHub.ProfileService.GetProfile(triggerName);

        if (!profile.IsEnabled)
            return executedCommands;

        if (!WadevoAlertHub.ProfileService.IsOffCooldown(profile))
            return executedCommands;

        switch (twitchEvent.EventType)
        {
            case TwitchEventType.Follow:
                WadevoDashboardHub.StatsService.RecordFollow();
                TriggerDesignedAlert(profile, context);
                break;

            case TwitchEventType.Raid:
                WadevoDashboardHub.StatsService.RecordRaid();
                TriggerDesignedAlert(profile, context);
                break;

            case TwitchEventType.Subscribe:
                WadevoDashboardHub.StatsService.RecordSubscribe();
                TriggerDesignedAlert(profile, context);
                break;

            case TwitchEventType.GiftSub:
                WadevoDashboardHub.StatsService.RecordGiftSub();
                TriggerDesignedAlert(profile, context);
                break;

            case TwitchEventType.Cheer:
                TriggerDesignedAlert(profile, context);
                break;

            case TwitchEventType.StreamOnline:
            case TwitchEventType.StreamOffline:
                TriggerDesignedAlert(profile, context);
                break;
        }

        return executedCommands;
    }

    private static void TriggerDesignedAlert(AlertProfileModel profile, BlazeEventCommandContext context)
    {
        OverlayServer.TriggerAlert(profile, context);
        WadevoAlertHub.ProfileService.RecordFired(profile);
    }

    private static IReadOnlyList<CommandExecutionResult> ExecuteChatCommand(TwitchEvent twitchEvent)
    {
        if (string.IsNullOrWhiteSpace(twitchEvent.Message))
            return Array.Empty<CommandExecutionResult>();

        CommandSenderContext sender = new()
        {
            Username = twitchEvent.Username ?? "",
            ChatMessage = twitchEvent.Message,
            IsSubscriber = twitchEvent.IsSubscriber,
            IsFollower = false, // Twitch's chat-message event doesn't include follow status.
            IsModerator = twitchEvent.IsModerator,
            IsOwner = twitchEvent.IsBroadcaster,
            SourcePlatform = CommandSourcePlatform.Twitch
        };

        return WadevoCommandHub.ExecutionService.ExecuteChatCommand(twitchEvent.Message, sender);
    }

    // Returns true if the message was a song request (and therefore should not go on to
    // match a regular command too) - same logic as Blaze's version, minus the "post
    // confirmation to chat" step, since there's no TwitchChatService to send with yet.
    private static bool TryHandleSongRequest(TwitchEvent twitchEvent)
    {
        if (string.IsNullOrWhiteSpace(twitchEvent.Message))
        {
            return false;
        }

        SongRequestSettings settings = WadevoSongRequestHub.SongRequestService.GetSettings();

        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.TriggerWord))
        {
            return false;
        }

        string message = twitchEvent.Message.Trim();
        string trigger = settings.TriggerWord.Trim();

        if (!message.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (message.Length > trigger.Length && !char.IsWhiteSpace(message[trigger.Length]))
        {
            return false;
        }

        string songText = message.Length > trigger.Length
            ? message[trigger.Length..].Trim()
            : "";

        if (string.IsNullOrWhiteSpace(songText))
        {
            return true;
        }

        string requesterUsername = twitchEvent.Username ?? "Someone";

        SongRequestModel? added = WadevoSongRequestHub.SongRequestService.TryAddRequest(
            requesterUsername, songText);

        if (added is not null)
        {
            WadevoDashboardHub.StatsService.RecordSongRequest(added.SongText);

            if (settings.PostConfirmationToChat)
            {
                string confirmation = settings.ConfirmationMessage
                    .Replace("{song}", added.SongText, StringComparison.OrdinalIgnoreCase)
                    .Replace("{username}", requesterUsername, StringComparison.OrdinalIgnoreCase);

                _ = PostChatMessageAsync(confirmation);
            }
        }

        return true;
    }

    private static bool TryHandleBlockedWord(TwitchEvent twitchEvent)
    {
        if (string.IsNullOrWhiteSpace(twitchEvent.Message) ||
            string.IsNullOrWhiteSpace(twitchEvent.MessageId))
        {
            return false;
        }

        BlockedWordsSettings settings = new BlockedWordsStore().Load();

        if (!settings.IsEnabled || settings.Words.Count == 0)
        {
            return false;
        }

        if (!BlockedWordsMatcher.ContainsBlockedWord(twitchEvent.Message, settings.Words))
        {
            return false;
        }

        _ = HandleBlockedWordAsync(twitchEvent, settings.MuteOnBlock);

        return true;
    }

    private static async Task HandleBlockedWordAsync(TwitchEvent twitchEvent, bool alsoTimeout)
    {
        TwitchAuthenticationService auth = TwitchAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            WadevoLogger.Warning("Blocked word detected but Twitch isn't connected - couldn't remove the message.");
            return;
        }

        string channelId = auth.Connection.UserId;
        TwitchModerationService moderationService = new();

        try
        {
            await moderationService.DeleteMessageAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                channelId,
                channelId,
                twitchEvent.MessageId!);

            WadevoLogger.Info($"Removed a Twitch chat message from {twitchEvent.Username ?? "a viewer"} for containing a blocked word.");
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to delete a blocked-word Twitch chat message", ex);
        }

        if (!alsoTimeout || string.IsNullOrWhiteSpace(twitchEvent.UserId))
        {
            return;
        }

        try
        {
            await moderationService.TimeoutUserAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                channelId,
                channelId,
                twitchEvent.UserId);

            WadevoLogger.Info($"Timed out {twitchEvent.Username ?? "a viewer"} for 10 minutes after a blocked word.");
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to time out a user after a blocked word", ex);
        }
    }

    private static async Task PostChatMessageAsync(string message)
    {
        TwitchAuthenticationService auth = TwitchAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            WadevoLogger.Warning("Couldn't post to Twitch chat - Twitch isn't connected.");
            return;
        }

        try
        {
            await new TwitchChatService().SendMessageAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Connection.UserId,
                auth.Connection.UserId,
                message);
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to send a Twitch chat message", ex);
        }
    }

    private static Dictionary<string, object?> BuildDataDictionary(TwitchEvent twitchEvent)
    {
        Dictionary<string, object?> data = new();

        if (twitchEvent.ViewerCount is not null)
        {
            data["viewerCount"] = twitchEvent.ViewerCount;
        }

        if (twitchEvent.BitsCheered is not null)
        {
            data["bitsCheered"] = twitchEvent.BitsCheered;
        }

        return data;
    }

    private static string GetTriggerName(TwitchEvent twitchEvent)
    {
        return twitchEvent.EventType switch
        {
            TwitchEventType.ChatMessage => "twitch.chat",
            TwitchEventType.Follow => "twitch.follow",
            TwitchEventType.Raid => "twitch.raid",
            TwitchEventType.Subscribe => "twitch.subscribe",
            TwitchEventType.GiftSub => "twitch.gift",
            TwitchEventType.Cheer => "twitch.cheer",
            TwitchEventType.StreamOnline => "twitch.online",
            TwitchEventType.StreamOffline => "twitch.offline",
            TwitchEventType.Connected => "twitch.connected",
            TwitchEventType.Disconnected => "twitch.disconnected",
            TwitchEventType.Error => "twitch.error",
            _ => "twitch.unknown"
        };
    }

    private static bool ShouldExecuteCommand(string triggerName)
    {
        return triggerName is not "twitch.connected"
            and not "twitch.disconnected"
            and not "twitch.error"
            and not "twitch.unknown";
    }
}
