using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Application.DeviceStates;
using MetricsHub.Domain.Enums;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MetricsHub.Infrastructure.Redis;

internal sealed class RedisDeviceStateStore(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<RedisOptions> options) : IDeviceStateStore
{
    private const string UpdateScript = """
        local key = KEYS[1]
        local incomingLastSeen = tonumber(ARGV[5])
        local currentLastSeen = tonumber(redis.call('HGET', key, 'lastSeenTimestamp') or '-1')

        redis.call('HSET', key, 'deviceId', ARGV[1], 'deviceKey', ARGV[2])

        if currentLastSeen < 0 or incomingLastSeen >= currentLastSeen then
            redis.call('HSET', key,
                'status', ARGV[3],
                'lastSeenAt', ARGV[4],
                'lastSeenTimestamp', ARGV[5])
        end

        local index = 7
        while index <= #ARGV do
            local metricField = ARGV[index]
            local metricJson = ARGV[index + 1]
            local incomingTimestamp = tonumber(ARGV[index + 2])
            local timestampField = 'metric-ts:' .. string.sub(metricField, 8)
            local currentTimestamp = tonumber(redis.call('HGET', key, timestampField) or '-1')

            if currentTimestamp < 0 or incomingTimestamp >= currentTimestamp then
                redis.call('HSET', key,
                    metricField, metricJson,
                    timestampField, ARGV[index + 2])
            end

            index = index + 3
        end

        redis.call('EXPIRE', key, tonumber(ARGV[6]))
        return 1
        """;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();
    private readonly RedisOptions _options = options.Value;

    public async Task<DeviceState?> GetAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        try
        {
            var entries = await _database.HashGetAllAsync(BuildKey(deviceId)).WaitAsync(cancellationToken);
            if (entries.Length == 0)
            {
                return null;
            }

            var values = entries.ToDictionary(
                entry => entry.Name.ToString(),
                entry => entry.Value.ToString(),
                StringComparer.Ordinal);

            var metrics = new Dictionary<MetricType, LatestMetricState>();
            foreach (var metricType in Enum.GetValues<MetricType>())
            {
                if (values.TryGetValue($"metric:{metricType}", out var json))
                {
                    var metric = JsonSerializer.Deserialize<LatestMetricState>(json, JsonOptions);
                    if (metric is not null)
                    {
                        metrics[metricType] = metric;
                    }
                }
            }

            return new DeviceState(
                Guid.Parse(values["deviceId"]),
                values["deviceKey"],
                Enum.Parse<DeviceStatus>(values["status"]),
                ParseOptionalUtc(values.GetValueOrDefault("lastSeenAt")),
                metrics);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is RedisException or FormatException or JsonException)
        {
            throw new DeviceStateStoreException("Redis current state could not be read.", exception);
        }
    }

    public async Task SetAsync(DeviceState state, CancellationToken cancellationToken)
    {
        var arguments = new List<RedisValue>
        {
            state.DeviceId.ToString("D"),
            state.DeviceKey,
            state.Status.ToString(),
            state.LastSeenAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            state.LastSeenAt.HasValue ? ToUnixMicroseconds(state.LastSeenAt.Value) : -1,
            checked(_options.StateTtlHours * 60 * 60)
        };

        foreach (var metric in state.LatestMetrics.OrderBy(pair => pair.Key))
        {
            arguments.Add($"metric:{metric.Key}");
            arguments.Add(JsonSerializer.Serialize(metric.Value, JsonOptions));
            arguments.Add(ToUnixMicroseconds(metric.Value.Timestamp));
        }

        try
        {
            await _database.ScriptEvaluateAsync(
                    UpdateScript,
                    [BuildKey(state.DeviceId)],
                    arguments.ToArray())
                .WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RedisException exception)
        {
            throw new DeviceStateStoreException("Redis current state could not be updated.", exception);
        }
    }

    public async Task RemoveAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        try
        {
            await _database.KeyDeleteAsync(BuildKey(deviceId)).WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RedisException exception)
        {
            throw new DeviceStateStoreException("Redis current state could not be removed.", exception);
        }
    }

    private RedisKey BuildKey(Guid deviceId) =>
        $"{_options.KeyPrefix}:device:{deviceId:D}:state";

    private static long ToUnixMicroseconds(DateTime value) =>
        (value.ToUniversalTime().Ticks - DateTime.UnixEpoch.Ticks) / 10;

    private static DateTime? ParseOptionalUtc(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
