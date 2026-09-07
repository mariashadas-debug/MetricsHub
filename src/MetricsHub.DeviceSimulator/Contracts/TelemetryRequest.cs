namespace MetricsHub.DeviceSimulator.Contracts;

public sealed record TelemetryRequest(
    string DeviceKey,
    DateTime Timestamp,
    IReadOnlyList<TelemetryMetricRequest> Metrics);
