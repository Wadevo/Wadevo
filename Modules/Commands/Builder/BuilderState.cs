namespace Wadevo.Modules.Commands.Builder;

public class BuilderState
{
    public string CommandType { get; set; } = "Chat Message";
    public string CommandName { get; set; } = "";
    public string ChatTriggers { get; set; } = "";
    public string TriggerMode { get; set; } = "Chat Trigger";
    public string IntervalMinutes { get; set; } = "30";
    public string Output { get; set; } = "";
    public bool RequirePrefix { get; set; } = true;
    public bool EnableCommand { get; set; } = true;

    public bool ShowInQuickPanel { get; set; } = false;
    public string CooldownSeconds { get; set; } = "0";
    public string MinimumRole { get; set; } = "Everyone";

    public string Width { get; set; } = "500";
    public string Height { get; set; } = "300";
    public string Duration { get; set; } = "5";
}