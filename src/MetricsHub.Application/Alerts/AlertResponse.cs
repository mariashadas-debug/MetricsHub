using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Alerts;

public sealed record AlertResponse(Guid Id, Guid DeviceId, Guid? AlertRuleId, AlertSeverity Severity, string Message, DateTime CreatedAt, DateTime? ResolvedAt, bool IsResolved);
