using MetricsHub.Application.Abstractions.Realtime;

namespace MetricsHub.Application.Realtime;

internal sealed class NullRealtimeNotifier : IRealtimeNotifier
{
    public Task DeviceStateUpdatedAsync(DeviceStateUpdatedEvent payload, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task TelemetryReceivedAsync(TelemetryReceivedEvent payload, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task DeviceStatusChangedAsync(DeviceStatusChangedEvent payload, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AlertRaisedAsync(AlertRaisedEvent payload, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task AlertResolvedAsync(AlertResolvedEvent payload, CancellationToken cancellationToken) => Task.CompletedTask;
}
