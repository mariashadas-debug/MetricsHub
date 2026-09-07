using MetricsHub.Application.DeviceStates;
using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Realtime;

public sealed record DeviceStateUpdatedEvent(Guid DeviceId, string DeviceKey, DeviceStatus Status, DateTime? LastSeenAt, IReadOnlyCollection<LatestMetricState> Metrics);
public sealed record TelemetryReceivedEvent(Guid DeviceId, string DeviceKey, DateTime Timestamp, IReadOnlyList<LatestMetricState> Metrics);
public sealed record DeviceStatusChangedEvent(Guid DeviceId, string DeviceKey, DeviceStatus PreviousStatus, DeviceStatus NewStatus, DateTime ChangedAt);
public sealed record AlertRaisedEvent(Guid AlertId, Guid DeviceId, Guid RuleId, AlertSeverity Severity, string Message, DateTime CreatedAt);
public sealed record AlertResolvedEvent(Guid AlertId, Guid DeviceId, DateTime ResolvedAt);
