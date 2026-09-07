using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Domain.Entities;

namespace MetricsHub.Application.Alerts;

internal sealed class AlertService(IAlertRepository alertRepository, IDeviceRepository deviceRepository) : IAlertService
{
    public const int MaximumLimit = 1000;

    public async Task<IReadOnlyList<AlertResponse>> GetAllAsync(AlertQuery query, CancellationToken cancellationToken)
    {
        ValidateLimit(query.Limit);
        return (await alertRepository.GetAlertsAsync(query, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<AlertResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await alertRepository.GetAlertAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Alert '{id}' was not found."));

    public async Task<IReadOnlyList<AlertResponse>> GetForDeviceAsync(Guid deviceId, int limit, CancellationToken cancellationToken)
    {
        ValidateLimit(limit);
        if (await deviceRepository.GetByIdAsync(deviceId, false, cancellationToken) is null)
        {
            throw new NotFoundException($"Device '{deviceId}' was not found.");
        }

        return (await alertRepository.GetDeviceAlertsAsync(deviceId, limit, cancellationToken)).Select(Map).ToArray();
    }

    private static void ValidateLimit(int limit)
    {
        if (limit is < 1 or > MaximumLimit)
        {
            throw new ApplicationValidationException($"Limit must be between 1 and {MaximumLimit}.");
        }
    }

    private static AlertResponse Map(Alert alert) => new(alert.Id, alert.DeviceId, alert.AlertRuleId, alert.Severity, alert.Message, alert.CreatedAt, alert.ResolvedAt, alert.IsResolved);
}
