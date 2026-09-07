using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Telemetry;

public sealed record TelemetryMetricInput(MetricType Type, double Value, string Unit);
