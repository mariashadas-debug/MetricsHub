using MetricsHub.Application.Realtime;

namespace MetricsHub.Api.Realtime;

public interface IMonitoringClient
{
    Task DeviceStateUpdated(DeviceStateUpdatedEvent payload);
    Task TelemetryReceived(TelemetryReceivedEvent payload);
    Task DeviceStatusChanged(DeviceStatusChangedEvent payload);
    Task AlertRaised(AlertRaisedEvent payload);
    Task AlertResolved(AlertResolvedEvent payload);
}
