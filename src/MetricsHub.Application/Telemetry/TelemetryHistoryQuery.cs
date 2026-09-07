using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Telemetry;

public sealed record TelemetryHistoryQuery(
    MetricType? MetricType,
    DateTime? From,
    DateTime? To,
    int Limit = 500);
