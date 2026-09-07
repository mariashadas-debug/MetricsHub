using MetricsHub.Application.Realtime;

namespace MetricsHub.Application.Abstractions.Realtime;

public interface IRealtimeNotifier
{
    Task DeviceStateUpdatedAsync(DeviceStateUpdatedEvent payload, CancellationToken cancellationToken);
    Task TelemetryReceivedAsync(TelemetryReceivedEvent payload, CancellationToken cancellationToken);
    Task DeviceStatusChangedAsync(DeviceStatusChangedEvent payload, CancellationToken cancellationToken);
    Task AlertRaisedAsync(AlertRaisedEvent payload, CancellationToken cancellationToken);
    Task AlertResolvedAsync(AlertResolvedEvent payload, CancellationToken cancellationToken);
}
