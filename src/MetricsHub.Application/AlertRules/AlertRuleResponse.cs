using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.AlertRules;

public sealed record AlertRuleResponse(Guid Id, Guid? DeviceId, string Name, MetricType MetricType, ComparisonOperator Operator, double Threshold, AlertSeverity Severity, bool IsEnabled);
