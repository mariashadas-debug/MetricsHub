using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Abstractions.Realtime;
using MetricsHub.Application.Alerts;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Application.DeviceStates;
using MetricsHub.Application.Realtime;
using MetricsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MetricsHub.Application.Telemetry;

internal sealed class TelemetryService(
    IDeviceRepository deviceRepository,
    ITelemetryRepository telemetryRepository,
    IDeviceStateStore deviceStateStore,
    IAlertEvaluationService alertEvaluationService,
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider,
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

        var previousStatus = device.Status;
        telemetryRepository.AddRange(points);
        device.RecordTelemetry(command.Timestamp);
        var alertTransitions = await alertEvaluationService.EvaluateAsync(
            device.Id, device.DeviceKey, command.Metrics, cancellationToken);
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

        DeviceState notificationState;
        try
        {
            await deviceStateStore.SetAsync(state, cancellationToken);
            logger.LogDebug("Redis state updated: DeviceId={DeviceId}", device.Id);
            notificationState = await deviceStateStore.GetAsync(device.Id, cancellationToken) ?? state;
        }
        catch (DeviceStateStoreException exception)
        {
            logger.LogWarning(exception, "Redis state update failed: DeviceId={DeviceId}", device.Id);
            notificationState = await RebuildState(device, cancellationToken);
        }

        await PublishNotifications(device, previousStatus, command, notificationState, alertTransitions, cancellationToken);
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

    private async Task<DeviceState> RebuildState(Device device, CancellationToken cancellationToken)
    {
        var latest = await telemetryRepository.GetLatestByMetricAsync(device.Id, cancellationToken);
        return new DeviceState(device.Id, device.DeviceKey, device.Status, device.LastSeenAt,
            latest.ToDictionary(point => point.MetricType, point => new LatestMetricState(point.MetricType, point.Value, point.Unit, point.Timestamp)));
    }

    private async Task PublishNotifications(
        Device device,
        MetricsHub.Domain.Enums.DeviceStatus previousStatus,
        IngestTelemetryCommand command,
        DeviceState state,
        AlertEvaluationResult transitions,
        CancellationToken cancellationToken)
    {
        await BestEffortNotify(() => realtimeNotifier.TelemetryReceivedAsync(
            new TelemetryReceivedEvent(device.Id, device.DeviceKey, command.Timestamp,
                command.Metrics.Select(metric => new LatestMetricState(metric.Type, metric.Value, metric.Unit, command.Timestamp)).ToArray()), cancellationToken), "TelemetryReceived", cancellationToken);
        await BestEffortNotify(() => realtimeNotifier.DeviceStateUpdatedAsync(new DeviceStateUpdatedEvent(
            state.DeviceId, state.DeviceKey, state.Status, state.LastSeenAt, state.LatestMetrics.Values.ToArray()), cancellationToken), "DeviceStateUpdated", cancellationToken);

        if (previousStatus != device.Status)
        {
            logger.LogInformation("Device returned online: DeviceId={DeviceId}", device.Id);
            await BestEffortNotify(() => realtimeNotifier.DeviceStatusChangedAsync(
                new DeviceStatusChangedEvent(device.Id, device.DeviceKey, previousStatus, device.Status, timeProvider.GetUtcNow().UtcDateTime), cancellationToken), "DeviceStatusChanged", cancellationToken);
        }

        foreach (var alert in transitions.Raised)
        {
            await BestEffortNotify(() => realtimeNotifier.AlertRaisedAsync(
                new AlertRaisedEvent(alert.Id, alert.DeviceId, alert.AlertRuleId!.Value, alert.Severity, alert.Message, alert.CreatedAt), cancellationToken), "AlertRaised", cancellationToken);
        }

        foreach (var alert in transitions.Resolved)
        {
            await BestEffortNotify(() => realtimeNotifier.AlertResolvedAsync(
                new AlertResolvedEvent(alert.Id, alert.DeviceId, alert.ResolvedAt!.Value), cancellationToken), "AlertResolved", cancellationToken);
        }
    }

    private async Task BestEffortNotify(Func<Task> publish, string eventType, CancellationToken cancellationToken)
    {
        try { await publish(); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { logger.LogWarning(exception, "SignalR publish failed: EventType={EventType}", eventType); }
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
