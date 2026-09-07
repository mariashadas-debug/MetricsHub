namespace MetricsHub.Application.Telemetry;

using MetricsHub.Application.DeviceStates;

public interface ITelemetryService
{
    Task IngestAsync(IngestTelemetryCommand command, CancellationToken cancellationToken);

    Task<IReadOnlyList<TelemetryPointResponse>> GetHistoryAsync(
        Guid deviceId,
        TelemetryHistoryQuery query,
        CancellationToken cancellationToken);

    Task<LatestTelemetryResponse> GetLatestAsync(Guid deviceId, CancellationToken cancellationToken);

    Task<DeviceState> GetStateAsync(Guid deviceId, CancellationToken cancellationToken);
}
