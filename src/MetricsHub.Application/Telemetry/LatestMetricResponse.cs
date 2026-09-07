using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Telemetry;

public sealed record LatestMetricResponse(
    MetricType Type,
    double Value,
    string Unit,
    DateTime Timestamp);
