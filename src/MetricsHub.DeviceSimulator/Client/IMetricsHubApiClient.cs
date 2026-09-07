using MetricsHub.DeviceSimulator.Configuration;
using MetricsHub.DeviceSimulator.Simulation;

namespace MetricsHub.DeviceSimulator.Client;

public interface IMetricsHubApiClient
{
    Task<ApiCallResult> EnsureDeviceRegisteredAsync(
        SimulatedDeviceOptions device,
        CancellationToken cancellationToken);

    Task<ApiCallResult> SendTelemetryAsync(
        string deviceKey,
        TelemetrySnapshot snapshot,
        CancellationToken cancellationToken);
}
