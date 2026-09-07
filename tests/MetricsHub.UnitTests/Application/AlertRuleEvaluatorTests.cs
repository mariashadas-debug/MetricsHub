using MetricsHub.Application.Alerts;
using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Entities;
using MetricsHub.Domain.Enums;

namespace MetricsHub.UnitTests.Application;

public sealed class AlertRuleEvaluatorTests
{
    private readonly AlertRuleEvaluator _evaluator = new();
    private readonly Device _device = new("Server", "server-01", DeviceType.Server);

    [Theory]
    [InlineData(ComparisonOperator.GreaterThan, 91, 90)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 90, 90)]
    [InlineData(ComparisonOperator.LessThan, 9, 10)]
    [InlineData(ComparisonOperator.LessThanOrEqual, 10, 10)]
    [InlineData(ComparisonOperator.Equal, 10, 10)]
    public void ComparisonOperators_TriggerAsDefined(ComparisonOperator comparison, double value, double threshold) =>
        Assert.True(AlertRuleEvaluator.IsViolated(value, comparison, threshold));

    [Fact]
    public void Equal_UsesExactDoubleEquality() =>
        Assert.False(AlertRuleEvaluator.IsViolated(0.1 + 0.2, ComparisonOperator.Equal, 0.3));

    [Fact]
    public void NonViolatingMetric_CreatesNoAlert()
    {
        var result = Evaluate(NewRule(), 50);
        Assert.Null(result.Raised);
        Assert.Null(result.Resolved);
    }

    [Fact]
    public void DisabledRule_IsIgnored()
    {
        var rule = NewRule();
        rule.Update(rule.Name, rule.MetricType, rule.Operator, rule.Threshold, rule.Severity, false, null);
        Assert.Null(Evaluate(rule, 99).Raised);
    }

    [Fact]
    public void DifferentMetricType_IsIgnored()
    {
        var rule = NewRule();
        var result = _evaluator.Evaluate(_device, rule, new TelemetryMetricInput(MetricType.MemoryUsage, 99, "%"), null, DateTime.UtcNow);
        Assert.Null(result.Raised);
    }

    [Fact]
    public void ExistingUnresolvedAlert_PreventsDuplicate()
    {
        var rule = NewRule();
        var active = new Alert(_device, rule.Severity, "Active", rule);
        Assert.Null(Evaluate(rule, 99, active).Raised);
    }

    [Fact]
    public void ReturningToNormal_ResolvesActiveAlert()
    {
        var rule = NewRule();
        var active = new Alert(_device, rule.Severity, "Active", rule);
        var result = Evaluate(rule, 50, active);
        Assert.Same(active, result.Resolved);
        Assert.True(active.IsResolved);
        Assert.NotNull(active.ResolvedAt);
    }

    [Fact]
    public void CrossingAgainAfterResolution_CreatesNewAlert()
    {
        var rule = NewRule();
        var oldAlert = new Alert(_device, rule.Severity, "Old", rule);
        oldAlert.Resolve(DateTime.UtcNow);
        var result = Evaluate(rule, 99, oldAlert);
        Assert.NotNull(result.Raised);
        Assert.NotEqual(oldAlert.Id, result.Raised.Id);
    }

    private AlertRuleTransition Evaluate(AlertRule rule, double value, Alert? active = null) =>
        _evaluator.Evaluate(_device, rule, new TelemetryMetricInput(MetricType.CpuUsage, value, "%"), active, DateTime.UtcNow);

    private static AlertRule NewRule() => new("High CPU", MetricType.CpuUsage, ComparisonOperator.GreaterThan, 90, AlertSeverity.Critical);
}
