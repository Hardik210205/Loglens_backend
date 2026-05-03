namespace LogLens.Application.DTOs
{
    public record DashboardStatsDto(
        int TotalLogs24h,
        int ActiveIncidents,
        int PendingAlerts24h,
        int SystemHealthPercent
    );
}
