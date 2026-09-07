using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Application.DeviceStates;
using MetricsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MetricsHub.Application.Telemetry;

internal sealed class TelemetryService(
    IDeviceRepository deviceRepository,
    ITelemetryRepository telemetryRepository,
    IDeviceStateStore deviceStateStore,
    ILogger<TelemetryService> logger) : ITelemetryService
{
    public const int MaximumHistoryLimit = 1000;

    public async Task IngestAsync(IngestTelemetryCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.DeviceKey))
        {
            throw new ApplicationValidationException("DeviceKey is required.");
        }

        RequireUtc(command.Timestamp, "Timestamp");

        if (command.Metrics is null || command.Metrics.Count == 0)
        {
            throw new ApplicationValidationException("At least one metric is required.");
        }

        var device = await deviceRepository.GetByKeyAsync(command.DeviceKey, true, cancellationToken)
            ?? throw new NotFoundException($"DeviceKey '{command.DeviceKey}' was not found.");

        if (!device.IsEnabled)
        {
            throw new ConflictException("Telemetry cannot be accepted for a disabled device.");
        }

        var points = command.Metrics.Select(metric =>
        {
            if (string.IsNullOrWhiteSpace(metric.Unit))
            {
                throw new ApplicationValidationException("Every metric unit is required.");
            }

            if (metric.Unit.Length > 32)
            {
                throw new ApplicationValidationException("Metric units cannot exceed 32 characters.");
            }

            return new TelemetryPoint(
                device,
                metric.Type,
                metric.Value,
                metric.Unit,
                command.Timestamp);
        }).ToArray();

        telemetryRepository.AddRange(points);
        device.RecordTelemetry(command.Timestamp);
        await telemetryRepository.SaveChangesAsync(cancellationToken);

        var state = new DeviceState(
            device.Id,
            device.DeviceKey,
            device.Status,
            device.LastSeenAt,
            points
                .GroupBy(point => point.MetricType)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var point = group.Last();
                        return new LatestMetricState(
                            point.MetricType,
                            point.Value,
                            point.Unit,
                            point.Timestamp);
                    }));

        try
        {
            await deviceStateStore.SetAsync(state, cancellationToken);
            logger.LogDebug("Redis state updated: DeviceId={DeviceId}", device.Id);
        }
        catch (DeviceStateStoreException exception)
        {
            logger.LogWarning(exception, "Redis state update failed: DeviceId={DeviceId}", device.Id);
        }
    }

    public async Task<IReadOnlyList<TelemetryPointResponse>> GetHistoryAsync(
        Guid deviceId,
        TelemetryHistoryQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureDeviceExists(deviceId, cancellationToken);
        ValidateHistoryQuery(query);

        var points = await telemetryRepository.GetHistoryAsync(deviceId, query, cancellationToken);
        return points.Select(Map).ToArray();
    }

    public async Task<LatestTelemetryResponse> GetLatestAsync(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        await EnsureDeviceExists(deviceId, cancellationToken);
        var points = await telemetryRepository.GetLatestByMetricAsync(deviceId, cancellationToken);
        var metrics = points.Select(point => new LatestMetricResponse(
            point.MetricType,
            point.Value,
            point.Unit,
            point.Timestamp)).ToArray();

        return new LatestTelemetryResponse(
            deviceId,
            metrics.Length == 0 ? null : metrics.Max(metric => metric.Timestamp),
            metrics);
    }

    public async Task<DeviceState> GetStateAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(deviceId, false, cancellationToken)
            ?? throw new NotFoundException($"Device '{deviceId}' was not found.");

        var redisAvailable = true;

        try
        {
            var cached = await deviceStateStore.GetAsync(deviceId, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }
        catch (DeviceStateStoreException exception)
        {
            redisAvailable = false;
            logger.LogWarning(exception, "Redis state read failed: DeviceId={DeviceId}", deviceId);
        }

        var latestPoints = await telemetryRepository.GetLatestByMetricAsync(deviceId, cancellationToken);
        var rebuilt = new DeviceState(
            device.Id,
            device.DeviceKey,
            device.Status,
            device.LastSeenAt,
            latestPoints.ToDictionary(
                point => point.MetricType,
                point => new LatestMetricState(
                    point.MetricType,
                    point.Value,
                    point.Unit,
                    point.Timestamp)));

        if (redisAvailable)
        {
            try
            {
                await deviceStateStore.SetAsync(rebuilt, cancellationToken);
                logger.LogInformation("Redis state rebuilt from MySQL: DeviceId={DeviceId}", deviceId);
            }
            catch (DeviceStateStoreException exception)
            {
                logger.LogWarning(exception, "Redis state rebuild write failed: DeviceId={DeviceId}", deviceId);
            }
        }

        return rebuilt;
    }

    private async Task EnsureDeviceExists(Guid deviceId, CancellationToken cancellationToken)
    {
        if (await deviceRepository.GetByIdAsync(deviceId, false, cancellationToken) is null)
        {
            throw new NotFoundException($"Device '{deviceId}' was not found.");
        }
    }

    private static void ValidateHistoryQuery(TelemetryHistoryQuery query)
    {
        if (query.From.HasValue)
        {
            RequireUtc(query.From.Value, "From");
        }

        if (query.To.HasValue)
        {
            RequireUtc(query.To.Value, "To");
        }

        if (query.From > query.To)
        {
            throw new ApplicationValidationException("From cannot be later than To.");
        }

        if (query.Limit is < 1 or > MaximumHistoryLimit)
        {
            throw new ApplicationValidationException(
                $"Limit must be between 1 and {MaximumHistoryLimit}.");
        }
    }

    private static void RequireUtc(DateTime value, string fieldName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ApplicationValidationException($"{fieldName} must be an ISO-8601 UTC value ending in Z.");
        }
    }

    private static TelemetryPointResponse Map(TelemetryPoint point) => new(
        point.Id,
        point.DeviceId,
        point.MetricType,
        point.Value,
        point.Unit,
        point.Timestamp);
}
