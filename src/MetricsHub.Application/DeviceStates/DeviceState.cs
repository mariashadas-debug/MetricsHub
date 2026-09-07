using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.DeviceStates;

public sealed record DeviceState(
    Guid DeviceId,
    string DeviceKey,
    DeviceStatus Status,
    DateTime? LastSeenAt,
    IReadOnlyDictionary<MetricType, LatestMetricState> LatestMetrics);
