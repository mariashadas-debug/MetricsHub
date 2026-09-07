using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Entities;

namespace MetricsHub.Application.Abstractions.Persistence;

public interface ITelemetryRepository
{
    void AddRange(IEnumerable<TelemetryPoint> telemetryPoints);

    Task<IReadOnlyList<TelemetryPoint>> GetHistoryAsync(
        Guid deviceId,
        TelemetryHistoryQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TelemetryPoint>> GetLatestByMetricAsync(
        Guid deviceId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
