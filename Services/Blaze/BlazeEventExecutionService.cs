namespace Wadevo.Services.Blaze;

using Wadevo.Models;
using Wadevo.Services;

public sealed class BlazeEventExecutionService
{
    public IReadOnlyList<CommandExecutionResult> Execute(BlazeEvent blazeEvent)
    {
        ArgumentNullException.ThrowIfNull(blazeEvent);

        List<CommandExecutionResult> executedCommands = new();

        BlazeEventCommandContext context =
            BlazeEventCommandContextFactory.Create(blazeEvent);

        if (blazeEvent.EventType == BlazeEventType.ChatMessage)
        {
            WadevoDashboardHub.StatsService.RecordChatMessage();

            if (TryHandleBlockedWord(blazeEvent))
            {
                return executedCommands;
            }

            OverlayServer.AddChatMessage(CommandSourcePlatform.Blaze, blazeEvent.Username ?? "Someone", blazeEvent.Message ?? "");

            if (TryHandleSongRequest(blazeEvent))
            {
                return executedCommands;
            }

            IReadOnlyList<CommandExecutionResult> chatCommandResults = ExecuteChatCommand(blazeEvent);

            foreach (CommandExecutionResult result in chatCommandResults)
            {
                WadevoDashboardHub.StatsService.RecordCommandExecuted(result.Command.Name);
            }

            executedCommands.AddRange(chatCommandResults);

            return executedCommands;
        }

        if (ShouldExecuteCommand(context.TriggerName))
        {
            IReadOnlyList<CommandExecutionResult> triggeredCommandResults =
                WadevoCommandHub.ExecutionService.ExecuteTrigger(context.TriggerName);

            foreach (CommandExecutionResult result in triggeredCommandResults)
            {
                WadevoDashboardHub.StatsService.RecordCommandExecuted(result.Command.Name);
            }

            executedCommands.AddRange(triggeredCommandResults);
        }

        AlertProfileModel profile =
            WadevoAlertHub.ProfileService.GetProfile(
                context.TriggerName);

        if (!profile.IsEnabled)
            return executedCommands;

        if (!WadevoAlertHub.ProfileService.IsOffCooldown(profile))
            return executedCommands;

        switch (blazeEvent.EventType)
        {
            case BlazeEventType.Follow:
                WadevoDashboardHub.StatsService.RecordFollow();
                TriggerDesignedAlert(profile, context);
                break;

            case BlazeEventType.Raid:
                WadevoDashboardHub.StatsService.RecordRaid();
                TriggerDesignedAlert(profile, context);
                break;

            case BlazeEventType.Subscribe:
                WadevoDashboardHub.StatsService.RecordSubscribe();
                TriggerDesignedAlert(profile, context);
                break;

            case BlazeEventType.GiftSub:
                WadevoDashboardHub.StatsService.RecordGiftSub();
                TriggerDesignedAlert(profile, context);
                break;

            case BlazeEventType.Vote:
                WadevoDashboardHub.StatsService.RecordVote();
                TriggerDesignedAlert(profile, context);
                break;

            case BlazeEventType.VipAdd:
                WadevoDashboardHub.StatsService.RecordVip();
                TriggerDesignedAlert(profile, context);
                break;

            case BlazeEventType.StreamOnline:
            case BlazeEventType.StreamOffline:
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

    private static IReadOnlyList<CommandExecutionResult> ExecuteChatCommand(
        BlazeEvent blazeEvent)
    {
        if (string.IsNullOrWhiteSpace(blazeEvent.Message))
            return Array.Empty<CommandExecutionResult>();

        CommandSenderContext sender = new()
        {
            Username = blazeEvent.Username ?? "",
            ChatMessage = blazeEvent.Message,
            IsSubscriber = blazeEvent.IsSubscriber,
            IsFollower = blazeEvent.IsFollower,
            IsModerator = blazeEvent.IsModerator,
            IsOwner = blazeEvent.IsOwner,
            SourcePlatform = CommandSourcePlatform.Blaze
        };

        return WadevoCommandHub.ExecutionService.ExecuteChatCommand(
            blazeEvent.Message,
            sender);
    }

    // Returns true if the message was blocked (and therefore should not go on to trigger
    // any command). Deletion (and mute, if enabled) happens async - this stays synchronous
    // since the whole event pipeline is synchronous.
    // Returns true if the message was a song request (and therefore should not go on to
    // match a regular command too - avoids ambiguity if a command happens to share the
    // same trigger word).
    private static bool TryHandleSongRequest(BlazeEvent blazeEvent)
    {
        if (string.IsNullOrWhiteSpace(blazeEvent.Message))
        {
            return false;
        }

        SongRequestSettings settings = WadevoSongRequestHub.SongRequestService.GetSettings();

        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.TriggerWord))
        {
            return false;
        }

        string message = blazeEvent.Message.Trim();
        string trigger = settings.TriggerWord.Trim();

        if (!message.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Require a word boundary after the trigger, so "!srtest" doesn't match "!sr".
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

        string requesterUsername = blazeEvent.Username ?? "Someone";

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

    private static async Task PostChatMessageAsync(string message)
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            return;
        }

        try
        {
            await new BlazeChatService().SendMessageAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret,
                auth.Connection.UserId,
                message);
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to post song request confirmation to chat", ex);
        }
    }

    private static bool TryHandleBlockedWord(BlazeEvent blazeEvent)
    {
        if (string.IsNullOrWhiteSpace(blazeEvent.Message) ||
            string.IsNullOrWhiteSpace(blazeEvent.MessageId))
        {
            return false;
        }

        BlockedWordsSettings settings = new BlockedWordsStore().Load();

        if (!settings.IsEnabled || settings.Words.Count == 0)
        {
            return false;
        }

        if (!BlockedWordsMatcher.ContainsBlockedWord(blazeEvent.Message, settings.Words))
        {
            return false;
        }

        _ = HandleBlockedWordAsync(blazeEvent, settings.MuteOnBlock);

        return true;
    }

    private static async Task HandleBlockedWordAsync(BlazeEvent blazeEvent, bool alsoMute)
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            WadevoLogger.Warning("Blocked word detected but Blaze isn't connected - couldn't remove the message.");
            return;
        }

        string channelId = auth.Connection.UserId;
        BlazeModerationService moderationService = new();

        try
        {
            await moderationService.DeleteMessageAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret,
                channelId,
                blazeEvent.MessageId!);

            WadevoLogger.Info($"Removed a chat message from {blazeEvent.Username ?? "a viewer"} for containing a blocked word.");
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to delete a blocked-word chat message", ex);
        }

        if (!alsoMute || string.IsNullOrWhiteSpace(blazeEvent.UserId))
        {
            return;
        }

        try
        {
            await moderationService.MuteUserAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret,
                channelId,
                blazeEvent.UserId);

            WadevoLogger.Info($"Muted {blazeEvent.Username ?? "a viewer"} for 10 minutes after a blocked word.");
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to mute a user after a blocked word", ex);
        }
    }

    private static bool ShouldExecuteCommand(string triggerName)
    {
        return triggerName is not "blaze.connected"
            and not "blaze.disconnected"
            and not "blaze.error"
            and not "blaze.unknown";
    }
}