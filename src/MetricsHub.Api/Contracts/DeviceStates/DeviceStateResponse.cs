using MetricsHub.Domain.Enums;

namespace MetricsHub.Api.Contracts.DeviceStates;

public sealed record DeviceStateResponse(
    Guid DeviceId,
    string DeviceKey,
    DeviceStatus Status,
    DateTime? LastSeenAt,
    IReadOnlyList<LatestMetricStateResponse> Metrics);
