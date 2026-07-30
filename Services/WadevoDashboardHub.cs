namespace Wadevo.Services;

public static class WadevoDashboardHub
{
    public static DashboardStatsService StatsService { get; } = new();
}
