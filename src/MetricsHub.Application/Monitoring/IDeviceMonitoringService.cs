namespace MetricsHub.Application.Monitoring;

public interface IDeviceMonitoringService
{
    Task<int> MarkStaleDevicesOfflineAsync(CancellationToken cancellationToken);
}
