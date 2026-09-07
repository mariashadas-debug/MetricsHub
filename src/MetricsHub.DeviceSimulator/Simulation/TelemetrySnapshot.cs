namespace MetricsHub.DeviceSimulator.Simulation;

public sealed record TelemetrySnapshot(
    DateTime Timestamp,
    IReadOnlyList<MetricSample> Metrics);
