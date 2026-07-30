namespace Wadevo.Services.Blaze;

using Wadevo.Models;

public static class BlazeDemoCommandSeeder
{
    public static void EnsureDemoCommands()
    {
        EnsureCommand(
            "Blaze Chat Event",
            "blaze.chat",
            "Chat Message",
            "Blaze chat event received.");

        EnsureCommand(
            "Blaze Follow Alert",
            "blaze.follow",
            "Alert",
            "Thanks for the follow!");

        EnsureCommand(
            "Blaze Raid Alert",
            "blaze.raid",
            "Alert",
            "Raid incoming!");
    }

    private static void EnsureCommand(
        string name,
        string trigger,
        string commandKind,
        string response)
    {
        if (WadevoCommandHub.CommandService.Commands.Any(command =>
                command.Trigger.Equals(trigger, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        CommandModel command = WadevoCommandHub.CommandService.AddCommand(
            name,
            trigger,
            commandKind);

        command.Response = response;
        command.RequireExclamation = false;
        command.IsEnabled = true;
    }
}