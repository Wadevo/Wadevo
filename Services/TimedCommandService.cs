namespace Wadevo.Services;

using Wadevo.Models;

/// <summary>
/// Periodically checks every command with TriggerMode="Timer" and fires any that are due,
/// using the same execution pipeline (chat message / alert / overlay media) that chat
/// triggers already use - this is purely a different way of deciding *when* to fire,
/// reusing WadevoCommandHub's shared CommandService/CommandExecutionService rather than
/// duplicating any of the actual "what does a command do" logic.
///
/// Runs as a simple background loop, same pattern used elsewhere in the app for this kind
/// of periodic background work, so it keeps
/// working regardless of which page of Wadevo happens to be open.
/// </summary>
public sealed class TimedCommandService
{
    private static readonly Lazy<TimedCommandService> LazyShared = new(() => new TimedCommandService());

    public static TimedCommandService Shared => LazyShared.Value;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly CommandService _commandService = WadevoCommandHub.CommandService;
    private readonly CommandExecutionService _executionService = WadevoCommandHub.ExecutionService;

    private CancellationTokenSource? _loopCancellation;

    public bool IsRunning => _loopCancellation is not null;

    private TimedCommandService()
    {
        Start();
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _loopCancellation = new CancellationTokenSource();
        _ = Task.Run(() => RunLoopAsync(_loopCancellation.Token));
    }

    public void Stop()
    {
        _loopCancellation?.Cancel();
        _loopCancellation = null;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            FireDueCommands();
        }
    }

    private void FireDueCommands()
    {
        DateTime now = DateTime.Now;
        bool anyFired = false;

        // Snapshot with ToList() - firing a command runs arbitrary output logic (chat
        // send, overlay trigger), and iterating a live collection while that runs risks
        // a collection-modified exception if someone edits/deletes a command mid-loop.
        foreach (CommandModel command in _commandService.Commands.ToList())
        {
            if (!command.IsEnabled || command.TriggerMode != "Timer")
            {
                continue;
            }

            int intervalMinutes = Math.Max(1, command.IntervalMinutes);
            DateTime baseline = command.LastFiredAt ?? command.CreatedAt;

            if ((now - baseline).TotalMinutes < intervalMinutes)
            {
                continue;
            }

            try
            {
                _executionService.ExecuteDirectly(command);
                command.LastFiredAt = now;
                anyFired = true;
            }
            catch (Exception ex)
            {
                WadevoLogger.Warning($"Timed command '{command.Name}' failed to fire: {ex.Message}");
            }
        }

        if (anyFired)
        {
            _commandService.Save();
        }
    }
}
