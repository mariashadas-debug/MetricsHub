using MetricsHub.Application.Abstractions.Realtime;
using MetricsHub.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace MetricsHub.Api.Realtime;

internal sealed class SignalRRealtimeNotifier(IHubContext<MonitoringHub, IMonitoringClient> hubContext) : IRealtimeNotifier
{
    public Task DeviceStateUpdatedAsync(DeviceStateUpdatedEvent payload, CancellationToken cancellationToken) =>
        Publish(payload.DeviceId, client => client.DeviceStateUpdated(payload), cancellationToken);

    public Task TelemetryReceivedAsync(TelemetryReceivedEvent payload, CancellationToken cancellationToken) =>
        Publish(payload.DeviceId, client => client.TelemetryReceived(payload), cancellationToken);

    public Task DeviceStatusChangedAsync(DeviceStatusChangedEvent payload, CancellationToken cancellationToken) =>
        Publish(payload.DeviceId, client => client.DeviceStatusChanged(payload), cancellationToken);

    public Task AlertRaisedAsync(AlertRaisedEvent payload, CancellationToken cancellationToken) =>
        Publish(payload.DeviceId, client => client.AlertRaised(payload), cancellationToken);

    public Task AlertResolvedAsync(AlertResolvedEvent payload, CancellationToken cancellationToken) =>
        Publish(payload.DeviceId, client => client.AlertResolved(payload), cancellationToken);

    private async Task Publish(Guid deviceId, Func<IMonitoringClient, Task> send, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.WhenAll(
            send(hubContext.Clients.Group(MonitoringHub.AllDevicesGroup)),
            send(hubContext.Clients.Group(MonitoringHub.DeviceGroup(deviceId))));
    }
}
