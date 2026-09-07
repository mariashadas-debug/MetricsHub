using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Alerts;

public interface IAlertEvaluationService
{
    Task<AlertEvaluationResult> EvaluateAsync(Guid deviceId, string deviceKey, IReadOnlyCollection<TelemetryMetricInput> metrics, CancellationToken cancellationToken);
    bool IsViolated(double value, ComparisonOperator comparisonOperator, double threshold);
}
