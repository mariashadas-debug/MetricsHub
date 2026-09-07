using MetricsHub.Application.Monitoring;
using Microsoft.Extensions.Options;

namespace MetricsHub.Api.BackgroundServices;

internal sealed class DeviceOfflineMonitor(IServiceScopeFactory scopeFactory, IOptions<MonitoringOptions> options, ILogger<DeviceOfflineMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.OfflineCheckIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IDeviceMonitoringService>()
                    .MarkStaleDevicesOfflineAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Offline device check failed."); }

            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
