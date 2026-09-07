using MetricsHub.Application.Alerts;
using MetricsHub.Domain.Entities;

namespace MetricsHub.Application.Abstractions.Persistence;

public interface IAlertRepository
{
    Task<IReadOnlyList<AlertRule>> GetApplicableRulesAsync(Guid deviceId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, Alert>> GetActiveByRuleIdsAsync(Guid deviceId, IReadOnlyCollection<Guid> ruleIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alert>> GetAlertsAsync(AlertQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alert>> GetDeviceAlertsAsync(Guid deviceId, int limit, CancellationToken cancellationToken);
    Task<Alert?> GetAlertAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRule>> GetRulesAsync(CancellationToken cancellationToken);
    Task<AlertRule?> GetRuleAsync(Guid id, bool trackChanges, CancellationToken cancellationToken);
    void AddAlert(Alert alert);
    void AddRule(AlertRule rule);
    void RemoveRule(AlertRule rule);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
