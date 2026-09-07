namespace MetricsHub.Application.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int OfflineAfterSeconds { get; set; } = 30;
    public int OfflineCheckIntervalSeconds { get; set; } = 10;
}
