using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.DeviceStates;

public sealed record LatestMetricState(
    MetricType Type,
    double Value,
    string Unit,
    DateTime Timestamp);
