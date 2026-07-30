using Wadevo.Models;
using Wadevo.Services.Blaze;
using Wadevo.Services.Obs;
using Wadevo.Services.Platforms;
using Wadevo.Services.Twitch;

namespace Wadevo.Services;

public sealed class CommandExecutionService
{
    private readonly CommandService _commandService;
    private readonly Dictionary<Guid, DateTime> _lastExecutedUtc = new();

    public CommandExecutionService(CommandService commandService)
    {
        _commandService = commandService
            ?? throw new ArgumentNullException(nameof(commandService));
    }

    // For Timer-mode commands, fired directly by TimedCommandService on a schedule rather
    // than matched against chat input. Cooldown doesn't apply here - the interval itself
    // already governs how often this fires, so a separate cooldown check would be redundant.
    public CommandExecutionResult ExecuteDirectly(CommandModel command)
    {
        ExecuteCommand(command, null);
        return CreateSuccessResult(command, command.Name);
    }

    public IReadOnlyList<CommandExecutionResult> ExecuteTrigger(string trigger)
    {
        string normalizedTrigger = NormalizeInput(trigger);

        if (string.IsNullOrWhiteSpace(normalizedTrigger))
            return Array.Empty<CommandExecutionResult>();

        CommandSenderContext platformOnlySender = new()
        {
            SourcePlatform = GetPlatformFromTriggerName(normalizedTrigger)
        };

        List<CommandExecutionResult> results = new();

        foreach (CommandModel command in _commandService.FindMatchingCommands(normalizedTrigger))
        {
            if (!TryEnterCooldown(command))
                continue;

            ExecuteCommand(command, platformOnlySender);
            results.Add(CreateSuccessResult(command, normalizedTrigger));
        }

        return results;
    }

    // Trigger names are namespaced (e.g. "blaze.follow", "twitch.raid"), so the platform
    // that fired this trigger can be read straight off the name without a separate parameter.
    private static CommandSourcePlatform GetPlatformFromTriggerName(string trigger)
    {
        return PlatformRegistry.GetByTriggerName(trigger)?.Platform
            ?? CommandSourcePlatform.Unknown;
    }

    public IReadOnlyList<CommandExecutionResult> ExecuteChatCommand(
        string chatMessage,
        CommandSenderContext? sender = null)
    {
        string normalizedMessage = NormalizeInput(chatMessage);

        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return Array.Empty<CommandExecutionResult>();

        List<CommandExecutionResult> results = new();

        foreach (CommandModel command in _commandService.FindMatchingChatCommands(normalizedMessage))
        {
            if (!CommandPermissionChecker.MeetsMinimumRole(command, sender))
                continue;

            if (!TryEnterCooldown(command))
                continue;

            ExecuteCommand(command, sender);
            results.Add(CreateSuccessResult(command, normalizedMessage));
        }

        return results;
    }

    private bool TryEnterCooldown(CommandModel command)
    {
        if (command.CooldownSeconds <= 0)
        {
            return true;
        }

        DateTime nowUtc = DateTime.UtcNow;

        if (_lastExecutedUtc.TryGetValue(command.Id, out DateTime lastUtc))
        {
            double secondsSince = (nowUtc - lastUtc).TotalSeconds;

            if (secondsSince < command.CooldownSeconds)
            {
                return false;
            }
        }

        _lastExecutedUtc[command.Id] = nowUtc;

        return true;
    }


    public bool CanExecute(string trigger)
    {
        string normalizedTrigger = NormalizeInput(trigger);

        if (string.IsNullOrWhiteSpace(normalizedTrigger))
            return false;

        return _commandService.HasMatchingCommand(normalizedTrigger);
    }

    public bool CanExecuteChatCommand(string chatMessage)
    {
        string normalizedMessage = NormalizeInput(chatMessage);

        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return false;

        return _commandService.HasMatchingChatCommand(normalizedMessage);
    }

    private static string NormalizeInput(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static CommandExecutionResult CreateSuccessResult(
        CommandModel command,
        string source)
    {
        return new CommandExecutionResult
        {
            Command = command,
            Success = true,
            Message = $"Executed '{command.Name}' from '{source}'."
        };
    }

    private static void ExecuteCommand(CommandModel command, CommandSenderContext? sender)
    {
        if (command.CommandKind.Equals("Multi Action", StringComparison.OrdinalIgnoreCase))
        {
            ExecuteMultiActionCommand(command, sender);
            return;
        }

        ExecuteSingleCommand(command, sender);
    }

    private static void ExecuteSingleCommand(CommandModel command, CommandSenderContext? sender)
    {
        string title = GetCommandTitle(command);
        string message = GetCommandMessage(command, sender);

        if (command.CommandKind.Equals("Alert", StringComparison.OrdinalIgnoreCase))
        {
            OverlayServer.TriggerAlert(title, message);
            return;
        }

        if (command.CommandKind.Equals("Chat Message", StringComparison.OrdinalIgnoreCase))
        {
            _ = SendChatMessageAsync(message, sender);
            return;
        }

        if (command.CommandKind.Equals("Change OBS Scene", StringComparison.OrdinalIgnoreCase))
        {
            string sceneName = command.Response.Trim();

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                _ = ChangeObsSceneAsync(sceneName);
            }

            return;
        }

        if (command.CommandKind.Equals("Sound Effect", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(command.MediaFilePath))
        {
            try
            {
                string? outputDevice = new Wadevo.Services.Soundboard.SoundboardSettingsService().Load().OutputDeviceName;
                new Wadevo.Services.Soundboard.SoundPlaybackService().Play(command.MediaFilePath, 100, outputDevice);
            }
            catch (Exception ex)
            {
                WadevoLogger.Error("Failed to play Sound Effect command", ex);
            }

            return;
        }

        if (IsMediaKind(command.CommandKind) && !string.IsNullOrWhiteSpace(command.MediaFilePath))
        {
            OverlayServer.TriggerCommand(title, title, command.MediaFilePath, command.Width, command.Height);
            return;
        }

        OverlayServer.TriggerCommand(title, message);
    }

    public static async Task<(bool Success, string Message)> ChangeObsSceneAsync(string sceneName)
    {
        if (!ObsConnectionService.Shared.IsConnected)
        {
            const string notConnectedMessage = "OBS isn't connected - check the Connections page.";
            WadevoLogger.Warning("Change OBS Scene command skipped - OBS isn't connected.");
            return (false, notConnectedMessage);
        }

        try
        {
            await ObsConnectionService.Shared.SetCurrentSceneAsync(sceneName);
            return (true, $"Switched OBS to \"{sceneName}\".");
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Failed to switch OBS scene '{sceneName}' via command: {ex.Message}");
            return (false, $"Couldn't switch to \"{sceneName}\" - check the scene name matches exactly.");
        }
    }

    private static async Task SendChatMessageAsync(string message, CommandSenderContext? sender)
    {
        if (sender?.SourcePlatform == CommandSourcePlatform.Twitch)
        {
            await SendTwitchChatMessageAsync(message);
            return;
        }

        // Blaze remains the default for CommandSourcePlatform.Blaze as well as Unknown
        // (e.g. Timer-mode commands, which aren't tied to a triggering platform at all) -
        // this preserves the exact previous behavior for every case except a Twitch-sourced one.
        await SendBlazeChatMessageAsync(message);
    }

    private static async Task SendTwitchChatMessageAsync(string message)
    {
        TwitchAuthenticationService auth = TwitchAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            WadevoLogger.Warning("Chat Message command skipped - Twitch is not connected.");
            return;
        }

        WadevoAppSettingsModel appSettings = new WadevoAppSettingsStore().Load();
        TwitchBotAuthenticationService botAuth = TwitchBotAuthenticationService.Shared;
        bool useBot = appSettings.UseTwitchBotIdentityForCommands && botAuth.IsConnected;

        try
        {
            if (useBot)
            {
                await new TwitchChatService().SendMessageAsync(
                    botAuth.Connection.AccessToken,
                    auth.Settings.ClientId,
                    auth.Connection.UserId,
                    botAuth.Connection.UserId,
                    message);
            }
            else
            {
                await new TwitchChatService().SendMessageAsync(
                    auth.Connection.AccessToken,
                    auth.Settings.ClientId,
                    auth.Connection.UserId,
                    auth.Connection.UserId,
                    message);
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to send Twitch chat message", ex);
        }
    }

    private static async Task SendBlazeChatMessageAsync(string message)
    {
        WadevoAppSettingsModel appSettings = new WadevoAppSettingsStore().Load();

        if (!appSettings.BlazeCommandsEnabled)
        {
            WadevoLogger.Info("Chat Message command skipped - Blaze commands are turned off in Connections.");
            return;
        }

        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            WadevoLogger.Warning("Chat Message command skipped - Blaze is not connected.");
            return;
        }

        BlazeBotAuthenticationService botAuth = BlazeBotAuthenticationService.Shared;
        bool useBot = appSettings.UseBotIdentityForCommands && botAuth.IsConnected;

        try
        {
            if (useBot)
            {
                string appToken = await new BlazeAppAccessTokenService().GetTokenAsync(
                    auth.Settings.ClientId,
                    auth.Settings.ClientSecret);

                await new BlazeChatService().SendMessageAsBotAsync(
                    appToken,
                    auth.Settings.ClientId,
                    auth.Settings.ClientSecret,
                    auth.Connection.UserId,
                    botAuth.BotUserId,
                    message);
            }
            else
            {
                await new BlazeChatService().SendMessageAsync(
                    auth.Connection.AccessToken,
                    auth.Settings.ClientId,
                    auth.Settings.ClientSecret,
                    auth.Connection.UserId,
                    message);
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to send Blaze chat message", ex);
        }
    }

    private static bool IsMediaKind(string commandKind)
    {
        // "Sound Effect" is deliberately not included here - it's handled separately,
        // above, by actually playing the audio file directly rather than showing a visual
        // overlay event (which was the previous, incorrect fallback - it showed a text
        // popup and never played anything).
        return commandKind is "GIF / Image" or "GIF/Image" or "Video Clip";
    }

    private static void ExecuteMultiActionCommand(CommandModel command, CommandSenderContext? sender)
    {
        string title = GetCommandTitle(command);

        string[] actions = (command.Response ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (actions.Length == 0)
        {
            OverlayServer.TriggerCommand(title, title);
            return;
        }

        foreach (string action in actions)
            ExecuteMultiActionLine(title, action, sender);
    }

    private static void ExecuteMultiActionLine(string commandTitle, string action, CommandSenderContext? sender)
    {
        if (string.IsNullOrWhiteSpace(action))
            return;

        string actionType = action.Trim();
        string actionValue = string.Empty;

        int separatorIndex = action.IndexOf(':');

        if (separatorIndex >= 0)
        {
            actionType = action[..separatorIndex].Trim();
            actionValue = action[(separatorIndex + 1)..].Trim();
        }

        if (string.IsNullOrWhiteSpace(actionValue))
            actionValue = commandTitle;

        if (actionType.Equals("Show Alert", StringComparison.OrdinalIgnoreCase))
        {
            OverlayServer.TriggerAlert(commandTitle, ApplyVariables(actionValue, sender));
            return;
        }

        if (actionType.Equals("Send Message", StringComparison.OrdinalIgnoreCase))
        {
            _ = SendChatMessageAsync(ApplyVariables(actionValue, sender), sender);
            return;
        }

        if (actionType.Equals("Switch OBS Scene", StringComparison.OrdinalIgnoreCase))
        {
            _ = ChangeObsSceneAsync(actionValue);
            return;
        }

        if (actionType.Equals("Play GIF/Image", StringComparison.OrdinalIgnoreCase))
        {
            OverlayServer.TriggerCommand(commandTitle, commandTitle, actionValue);
            return;
        }

        if (actionType.Equals("Play Sound", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                string? outputDevice = new Wadevo.Services.Soundboard.SoundboardSettingsService().Load().OutputDeviceName;
                new Wadevo.Services.Soundboard.SoundPlaybackService().Play(actionValue, 100, outputDevice);
            }
            catch (Exception ex)
            {
                WadevoLogger.Error("Failed to play sound in Multi Action command", ex);
            }

            return;
        }

        if (actionType.Equals("Wait", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        OverlayServer.TriggerCommand(commandTitle, action);
    }

    private static string GetCommandTitle(CommandModel command)
    {
        return string.IsNullOrWhiteSpace(command.Name)
            ? "Wadevo Command"
            : command.Name.Trim();
    }

    private static string GetCommandMessage(CommandModel command, CommandSenderContext? sender)
    {
        string message = string.IsNullOrWhiteSpace(command.Response)
            ? command.Trigger
            : command.Response.Trim();

        message = string.IsNullOrWhiteSpace(message)
            ? GetCommandTitle(command)
            : message;

        return ApplyVariables(message, sender);
    }

    // Reuses the exact same token formatter Alerts already relies on, so {username},
    // {message}, etc. behave identically in both places instead of two separate, possibly
    // inconsistent implementations. Falls back to sample values (matching how Alerts'
    // own preview works) when there's no real sender - a manual "Test" trigger or a
    // Timer-mode command firing on a schedule, neither of which has a real chatter behind it.
    private static string ApplyVariables(string template, CommandSenderContext? sender)
    {
        BlazeEventCommandContext context = new()
        {
            Username = string.IsNullOrWhiteSpace(sender?.Username) ? "Viewer" : sender.Username,
            Message = sender?.ChatMessage ?? "",
            Data = new Dictionary<string, object?>
            {
                ["viewerCount"] = 0,
                ["giftCount"] = 1,
                ["voteAmount"] = 0
            }
        };

        return AlertProfileTextFormatter.Format(template, context);
    }
}