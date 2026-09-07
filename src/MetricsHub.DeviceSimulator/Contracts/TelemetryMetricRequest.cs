namespace MetricsHub.DeviceSimulator.Contracts;

public sealed record TelemetryMetricRequest(string Type, double Value, string Unit);
