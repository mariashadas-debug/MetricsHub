using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Abstractions.Realtime;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Application.DeviceStates;
using MetricsHub.Application.Realtime;
using MetricsHub.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MetricsHub.Application.Monitoring;

internal sealed class DeviceMonitoringService(
    IDeviceRepository deviceRepository,
    ITelemetryRepository telemetryRepository,
    IDeviceStateStore stateStore,
    IRealtimeNotifier notifier,
    IOptions<MonitoringOptions> options,
    TimeProvider timeProvider,
    ILogger<DeviceMonitoringService> logger) : IDeviceMonitoringService
{
    public async Task<int> MarkStaleDevicesOfflineAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now.AddSeconds(-options.Value.OfflineAfterSeconds);
        var devices = await deviceRepository.GetStaleOnlineDevicesAsync(cutoff, cancellationToken);
        if (devices.Count == 0) return 0;

        foreach (var device in devices) device.MarkOffline();
        await deviceRepository.SaveChangesAsync(cancellationToken);

        foreach (var device in devices)
        {
            logger.LogInformation("Device marked offline: DeviceId={DeviceId} LastSeenAt={LastSeenAt}", device.Id, device.LastSeenAt);
            var state = await BuildState(device.Id, device.DeviceKey, device.LastSeenAt, cancellationToken);
            await BestEffortStateUpdate(state, cancellationToken);
            await BestEffortNotify(
                () => notifier.DeviceStatusChangedAsync(new DeviceStatusChangedEvent(device.Id, device.DeviceKey, DeviceStatus.Online, DeviceStatus.Offline, now), cancellationToken),
                "DeviceStatusChanged", cancellationToken);
            await BestEffortNotify(
                () => notifier.DeviceStateUpdatedAsync(new DeviceStateUpdatedEvent(
                    state.DeviceId, state.DeviceKey, state.Status, state.LastSeenAt, state.LatestMetrics.Values.ToArray()), cancellationToken),
                "DeviceStateUpdated", cancellationToken);
        }

        return devices.Count;
    }

    private async Task<DeviceState> BuildState(Guid deviceId, string deviceKey, DateTime? lastSeenAt, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await stateStore.GetAsync(deviceId, cancellationToken);
            if (cached is not null) return cached with { Status = DeviceStatus.Offline, LastSeenAt = lastSeenAt };
        }
        catch (DeviceStateStoreException exception)
        {
            logger.LogWarning(exception, "Redis state read failed: DeviceId={DeviceId}", deviceId);
        }

        var points = await telemetryRepository.GetLatestByMetricAsync(deviceId, cancellationToken);
        return new DeviceState(deviceId, deviceKey, DeviceStatus.Offline, lastSeenAt,
            points.ToDictionary(point => point.MetricType, point => new LatestMetricState(point.MetricType, point.Value, point.Unit, point.Timestamp)));
    }

    private async Task BestEffortStateUpdate(DeviceState state, CancellationToken cancellationToken)
    {
        try { await stateStore.SetAsync(state, cancellationToken); }
        catch (DeviceStateStoreException exception) { logger.LogWarning(exception, "Redis state update failed: DeviceId={DeviceId}", state.DeviceId); }
    }

    private async Task BestEffortNotify(Func<Task> publish, string eventType, CancellationToken cancellationToken)
    {
        try { await publish(); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { logger.LogWarning(exception, "SignalR publish failed: EventType={EventType}", eventType); }
    }
}
