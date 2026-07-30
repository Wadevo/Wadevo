using Wadevo.Models;

namespace Wadevo.Services;

public sealed class CommandExecutionResult
{
    public CommandModel Command { get; init; } = new();

    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}