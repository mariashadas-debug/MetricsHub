using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Entities;
using MetricsHub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MetricsHub.Application.Alerts;

internal sealed class AlertEvaluationService(IAlertRepository repository, IDeviceRepository deviceRepository, AlertRuleEvaluator evaluator, TimeProvider timeProvider, ILogger<AlertEvaluationService> logger) : IAlertEvaluationService
{
    public async Task<AlertEvaluationResult> EvaluateAsync(Guid deviceId, string deviceKey, IReadOnlyCollection<TelemetryMetricInput> metrics, CancellationToken cancellationToken)
    {
        var rules = await repository.GetApplicableRulesAsync(deviceId, cancellationToken);
        var active = await repository.GetActiveByRuleIdsAsync(deviceId, rules.Select(rule => rule.Id).ToArray(), cancellationToken);
        var values = metrics.GroupBy(metric => metric.Type).ToDictionary(group => group.Key, group => group.Last());
        var raised = new List<Alert>();
        var resolved = new List<Alert>();
        var device = await RequireDevice(deviceId, cancellationToken);

        foreach (var rule in rules)
        {
            if (!values.TryGetValue(rule.MetricType, out var metric)) continue;
            active.TryGetValue(rule.Id, out var activeAlert);
            var transition = evaluator.Evaluate(device, rule, metric, activeAlert, timeProvider.GetUtcNow().UtcDateTime);
            if (transition.Raised is not null)
            {
                repository.AddAlert(transition.Raised);
                raised.Add(transition.Raised);
                logger.LogInformation("Alert raised: DeviceId={DeviceId} RuleId={RuleId}", deviceId, rule.Id);
            }
            else if (transition.Resolved is not null)
            {
                resolved.Add(transition.Resolved);
                logger.LogInformation("Alert resolved: AlertId={AlertId}", transition.Resolved.Id);
            }
        }

        return new AlertEvaluationResult(raised, resolved);
    }

    public bool IsViolated(double value, ComparisonOperator comparisonOperator, double threshold) =>
        AlertRuleEvaluator.IsViolated(value, comparisonOperator, threshold);

    private async Task<Device> RequireDevice(Guid deviceId, CancellationToken cancellationToken) =>
        await deviceRepository.GetByIdAsync(deviceId, true, cancellationToken)
            ?? throw new InvalidOperationException($"Device '{deviceId}' disappeared during alert evaluation.");

}
