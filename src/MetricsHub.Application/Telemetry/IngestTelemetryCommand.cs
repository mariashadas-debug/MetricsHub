namespace MetricsHub.Application.Telemetry;

public sealed record IngestTelemetryCommand(
    string DeviceKey,
    DateTime Timestamp,
    IReadOnlyCollection<TelemetryMetricInput> Metrics);
