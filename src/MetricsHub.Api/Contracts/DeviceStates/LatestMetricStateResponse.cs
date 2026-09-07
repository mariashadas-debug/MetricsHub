using MetricsHub.Domain.Enums;

namespace MetricsHub.Api.Contracts.DeviceStates;

public sealed record LatestMetricStateResponse(
    MetricType Type,
    double Value,
    string Unit,
    DateTime Timestamp);
