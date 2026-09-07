using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.AlertRules;

public sealed record SaveAlertRuleCommand(Guid? DeviceId, string Name, MetricType MetricType, ComparisonOperator Operator, double Threshold, AlertSeverity Severity, bool IsEnabled = true);
