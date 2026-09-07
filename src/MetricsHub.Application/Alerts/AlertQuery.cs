using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Alerts;

public sealed record AlertQuery(bool? IsResolved, AlertSeverity? Severity, Guid? DeviceId, int Limit = 200);
