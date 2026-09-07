namespace MetricsHub.Application.Alerts;

public interface IAlertService
{
    Task<IReadOnlyList<AlertResponse>> GetAllAsync(AlertQuery query, CancellationToken cancellationToken);
    Task<AlertResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertResponse>> GetForDeviceAsync(Guid deviceId, int limit, CancellationToken cancellationToken);
}
