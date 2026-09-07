namespace MetricsHub.Application.Telemetry;

public sealed record LatestTelemetryResponse(
    Guid DeviceId,
    DateTime? Timestamp,
    IReadOnlyList<LatestMetricResponse> Metrics);
