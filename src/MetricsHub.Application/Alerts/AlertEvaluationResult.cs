using MetricsHub.Domain.Entities;

namespace MetricsHub.Application.Alerts;

public sealed record AlertEvaluationResult(IReadOnlyList<Alert> Raised, IReadOnlyList<Alert> Resolved);
