using System.Globalization;
using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Entities;
using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Alerts;

public sealed record AlertRuleTransition(Alert? Raised, Alert? Resolved);

public sealed class AlertRuleEvaluator
{
    public AlertRuleTransition Evaluate(Device device, AlertRule rule, TelemetryMetricInput metric, Alert? activeAlert, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(rule);
        if (!rule.IsEnabled || rule.MetricType != metric.Type) return new(null, null);

        if (IsViolated(metric.Value, rule.Operator, rule.Threshold))
        {
            if (activeAlert is not null && !activeAlert.IsResolved) return new(null, null);
            return new(new Alert(device, rule.Severity, BuildMessage(device.DeviceKey, metric, rule), rule), null);
        }

        if (activeAlert is null || activeAlert.IsResolved) return new(null, null);
        activeAlert.Resolve(now);
        return new(null, activeAlert);
    }

    public static bool IsViolated(double value, ComparisonOperator comparisonOperator, double threshold) => comparisonOperator switch
    {
        ComparisonOperator.GreaterThan => value > threshold,
        ComparisonOperator.GreaterThanOrEqual => value >= threshold,
        ComparisonOperator.LessThan => value < threshold,
        ComparisonOperator.LessThanOrEqual => value <= threshold,
        ComparisonOperator.Equal => value.Equals(threshold),
        _ => throw new ArgumentOutOfRangeException(nameof(comparisonOperator), comparisonOperator, null)
    };

    private static string BuildMessage(string deviceKey, TelemetryMetricInput metric, AlertRule rule) =>
        $"{rule.MetricType} value {metric.Value.ToString(CultureInfo.InvariantCulture)}{metric.Unit} matched {OperatorText(rule.Operator)} threshold {rule.Threshold.ToString(CultureInfo.InvariantCulture)}{metric.Unit} on {deviceKey}.";

    private static string OperatorText(ComparisonOperator value) => value switch
    {
        ComparisonOperator.GreaterThan => ">",
        ComparisonOperator.GreaterThanOrEqual => ">=",
        ComparisonOperator.LessThan => "<",
        ComparisonOperator.LessThanOrEqual => "<=",
        ComparisonOperator.Equal => "=",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
