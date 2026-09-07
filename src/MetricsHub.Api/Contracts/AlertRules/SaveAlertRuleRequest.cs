using System.ComponentModel.DataAnnotations;
using MetricsHub.Domain.Enums;

namespace MetricsHub.Api.Contracts.AlertRules;

public sealed class SaveAlertRuleRequest
{
    public Guid? DeviceId { get; init; }
    [Required, MaxLength(200)] public string Name { get; init; } = string.Empty;
    [Required] public MetricType? MetricType { get; init; }
    [Required] public ComparisonOperator? Operator { get; init; }
    public double Threshold { get; init; }
    [Required] public AlertSeverity? Severity { get; init; }
    public bool IsEnabled { get; init; } = true;
}
