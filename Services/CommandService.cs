using System.Text.Json;
using Wadevo.Models;

namespace Wadevo.Services;

public class CommandService
{
    private readonly List<CommandModel> _commands = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string DataFolder =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

    private static readonly string CommandsFilePath =
        Path.Combine(DataFolder, "commands.json");

    public IReadOnlyList<CommandModel> Commands => _commands;

    public CommandService()
    {
        Load();
    }

    public CommandModel AddCommand(string name, string trigger, string commandKind)
    {
        CommandModel command = new()
        {
            Name = name,
            Trigger = NormalizeTrigger(trigger),
            CommandKind = commandKind,
            IsEnabled = true
        };

        _commands.Add(command);
        Save();

        return command;
    }

    public CommandModel AddCommand(string trigger, string response)
    {
        CommandModel command = AddCommand(trigger, trigger, "Chat Message");
        command.Response = response;
        Save();

        return command;
    }

    public CommandModel DuplicateCommand(CommandModel source)
    {
        CommandModel copy = new()
        {
            Name = $"{source.Name} Copy",
            Trigger = CreateDuplicateTrigger(source.Trigger),
            RequireExclamation = source.RequireExclamation,
            CommandKind = source.CommandKind,
            Response = source.Response,
            MediaFilePath = source.MediaFilePath,
            Width = source.Width,
            Height = source.Height,
            DurationSeconds = source.DurationSeconds,
            FadeIn = source.FadeIn,
            FadeOut = source.FadeOut,
            IsEnabled = source.IsEnabled
        };

        _commands.Add(copy);
        Save();

        return copy;
    }

    public void Save()
    {
        Directory.CreateDirectory(DataFolder);

        string json = JsonSerializer.Serialize(_commands, JsonOptions);
        File.WriteAllText(CommandsFilePath, json);
    }

    public void Load()
    {
        _commands.Clear();

        if (!File.Exists(CommandsFilePath))
            return;

        try
        {
            string json = File.ReadAllText(CommandsFilePath);

            List<CommandModel>? loadedCommands =
                JsonSerializer.Deserialize<List<CommandModel>>(json, JsonOptions);

            if (loadedCommands is null)
                return;

            _commands.AddRange(loadedCommands);
        }
        catch
        {
            _commands.Clear();
        }
    }

    public IEnumerable<CommandModel> FindMatchingCommands(string trigger)
    {
        string normalizedTrigger = NormalizeTrigger(trigger);

        if (string.IsNullOrWhiteSpace(normalizedTrigger))
            return Enumerable.Empty<CommandModel>();

        return _commands.Where(command =>
            command.IsEnabled &&
            command.TriggerMode != "Timer" &&
            GetCommandTriggers(command).Any(commandTrigger =>
                TriggersEqual(commandTrigger, normalizedTrigger)));
    }

    public IEnumerable<CommandModel> FindMatchingChatCommands(string chatMessage)
    {
        string normalizedMessage = NormalizeTrigger(chatMessage);

        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return Enumerable.Empty<CommandModel>();

        string firstWord = GetFirstWord(normalizedMessage);

        return _commands.Where(command =>
            command.IsEnabled &&
            command.TriggerMode != "Timer" &&
            IsChatCommandMatch(command, normalizedMessage, firstWord));
    }

    public bool HasMatchingCommand(string trigger)
    {
        return FindMatchingCommands(trigger).Any();
    }

    public bool HasMatchingChatCommand(string chatMessage)
    {
        return FindMatchingChatCommands(chatMessage).Any();
    }

    public void RemoveCommand(CommandModel command)
    {
        _commands.Remove(command);
        Save();
    }

    public void Clear()
    {
        _commands.Clear();
        Save();
    }

    private static bool IsChatCommandMatch(
        CommandModel command,
        string fullChatMessage,
        string firstWord)
    {
        foreach (string trigger in GetCommandTriggers(command))
        {
            string commandTrigger = NormalizeTrigger(trigger);

            if (string.IsNullOrWhiteSpace(commandTrigger))
                continue;

            if (command.RequireExclamation)
                commandTrigger = EnsureExclamation(commandTrigger);

            if (TriggersEqual(commandTrigger, fullChatMessage))
                return true;

            if (TriggersEqual(commandTrigger, firstWord))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> GetCommandTriggers(CommandModel command)
    {
        if (string.IsNullOrWhiteSpace(command.Trigger))
            yield break;

        foreach (string trigger in command.Trigger.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string normalized = NormalizeTrigger(trigger);

            if (!string.IsNullOrWhiteSpace(normalized))
                yield return normalized;
        }
    }

    private static string GetFirstWord(string value)
    {
        string normalized = NormalizeTrigger(value);

        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        int spaceIndex = normalized.IndexOf(' ');

        return spaceIndex < 0
            ? normalized
            : normalized[..spaceIndex].Trim();
    }

    private static bool TriggersEqual(string left, string right)
    {
        return string.Equals(
            NormalizeTrigger(left),
            NormalizeTrigger(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTrigger(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim();
    }

    private static string EnsureExclamation(string value)
    {
        string normalized = NormalizeTrigger(value);

        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return normalized.StartsWith('!')
            ? normalized
            : $"!{normalized}";
    }

    private static string CreateDuplicateTrigger(string trigger)
    {
        string normalizedTrigger = NormalizeTrigger(trigger);

        if (string.IsNullOrWhiteSpace(normalizedTrigger))
            return "copy";

        return normalizedTrigger.EndsWith("-copy", StringComparison.OrdinalIgnoreCase)
            ? $"{normalizedTrigger}-2"
            : $"{normalizedTrigger}-copy";
    }
}