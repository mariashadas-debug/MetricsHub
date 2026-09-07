using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Alerts;
using MetricsHub.Domain.Entities;
using MetricsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MetricsHub.Infrastructure.Persistence;

internal sealed class AlertRepository(MetricsHubDbContext context) : IAlertRepository
{
    public async Task<IReadOnlyList<AlertRule>> GetApplicableRulesAsync(Guid deviceId, CancellationToken cancellationToken) =>
        await context.AlertRules.Include(rule => rule.Device)
            .Where(rule => rule.IsEnabled && (rule.DeviceId == null || rule.DeviceId == deviceId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Alert>> GetActiveByRuleIdsAsync(Guid deviceId, IReadOnlyCollection<Guid> ruleIds, CancellationToken cancellationToken) =>
        await context.Alerts
            .Where(alert => alert.DeviceId == deviceId && !alert.IsResolved && alert.AlertRuleId.HasValue && ruleIds.Contains(alert.AlertRuleId.Value))
            .ToDictionaryAsync(alert => alert.AlertRuleId!.Value, cancellationToken);

    public async Task<IReadOnlyList<Alert>> GetAlertsAsync(AlertQuery query, CancellationToken cancellationToken)
    {
        var alerts = context.Alerts.AsNoTracking().AsQueryable();
        if (query.IsResolved.HasValue) alerts = alerts.Where(alert => alert.IsResolved == query.IsResolved.Value);
        if (query.Severity.HasValue) alerts = alerts.Where(alert => alert.Severity == query.Severity.Value);
        if (query.DeviceId.HasValue) alerts = alerts.Where(alert => alert.DeviceId == query.DeviceId.Value);
        return await alerts.OrderByDescending(alert => alert.CreatedAt).Take(query.Limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> GetDeviceAlertsAsync(Guid deviceId, int limit, CancellationToken cancellationToken) =>
        await context.Alerts.AsNoTracking().Where(alert => alert.DeviceId == deviceId)
            .OrderByDescending(alert => alert.CreatedAt).Take(limit).ToListAsync(cancellationToken);

    public Task<Alert?> GetAlertAsync(Guid id, CancellationToken cancellationToken) =>
        context.Alerts.AsNoTracking().SingleOrDefaultAsync(alert => alert.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AlertRule>> GetRulesAsync(CancellationToken cancellationToken) =>
        await context.AlertRules.AsNoTracking().OrderBy(rule => rule.Name).ToListAsync(cancellationToken);

    public Task<AlertRule?> GetRuleAsync(Guid id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? context.AlertRules : context.AlertRules.AsNoTracking();
        return query.SingleOrDefaultAsync(rule => rule.Id == id, cancellationToken);
    }

    public void AddAlert(Alert alert) => context.Alerts.Add(alert);
    public void AddRule(AlertRule rule) => context.AlertRules.Add(rule);
    public void RemoveRule(AlertRule rule) => context.AlertRules.Remove(rule);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
