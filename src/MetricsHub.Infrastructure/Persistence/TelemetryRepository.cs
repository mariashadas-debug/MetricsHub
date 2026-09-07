using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Entities;
using MetricsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MetricsHub.Infrastructure.Persistence;

internal sealed class TelemetryRepository(MetricsHubDbContext context) : ITelemetryRepository
{
    public void AddRange(IEnumerable<TelemetryPoint> telemetryPoints) =>
        context.TelemetryPoints.AddRange(telemetryPoints);

    public async Task<IReadOnlyList<TelemetryPoint>> GetHistoryAsync(
        Guid deviceId,
        TelemetryHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var telemetry = context.TelemetryPoints
            .AsNoTracking()
            .Where(point => point.DeviceId == deviceId);

        if (query.MetricType.HasValue)
        {
            telemetry = telemetry.Where(point => point.MetricType == query.MetricType.Value);
        }

        if (query.From.HasValue)
        {
            telemetry = telemetry.Where(point => point.Timestamp >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            telemetry = telemetry.Where(point => point.Timestamp <= query.To.Value);
        }

        return await telemetry
            .OrderByDescending(point => point.Timestamp)
            .ThenByDescending(point => point.Id)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TelemetryPoint>> GetLatestByMetricAsync(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var latestTimestamps = context.TelemetryPoints
            .Where(point => point.DeviceId == deviceId)
            .GroupBy(point => point.MetricType)
            .Select(group => new
            {
                MetricType = group.Key,
                Timestamp = group.Max(point => point.Timestamp)
            });

        var query =
            from point in context.TelemetryPoints.AsNoTracking()
            join latest in latestTimestamps
                on new { point.MetricType, point.Timestamp }
                equals new { latest.MetricType, latest.Timestamp }
            where point.DeviceId == deviceId
            orderby point.MetricType, point.Id descending
            select point;

        var candidates = await query.ToListAsync(cancellationToken);

        return candidates
            .DistinctBy(point => point.MetricType)
            .ToArray();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
