namespace Wadevo.Services;

public static class WadevoCommandHub
{
    public static CommandService CommandService { get; } = new();

    public static CommandExecutionService ExecutionService { get; } =
        new(CommandService);
}