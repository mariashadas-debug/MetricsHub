using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsHub.Api.Contracts.DeviceStates;
using MetricsHub.Application.Devices;
using MetricsHub.Domain.Enums;
using MetricsHub.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace MetricsHub.IntegrationTests.Api;

[Collection(InfrastructureCollection.Name)]
public sealed class RedisStateIntegrationTests(MySqlDatabaseFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [MySqlIntegrationFact]
    public async Task Telemetry_WritesNamespacedRedisStateWithMultipleMetrics()
    {
        await fixture.FlushRedisAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDeviceAsync(client);
        var timestamp = DateTime.UtcNow.AddMinutes(-1);

        var ingest = await PostTelemetryAsync(client, device.DeviceKey, timestamp,
            ("CpuUsage", 42.3, "%"),
            ("MemoryUsage", 67.1, "%"));
        var state = await client.GetFromJsonAsync<DeviceStateResponse>(
            $"/api/v1/devices/{device.Id}/state", JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, ingest.StatusCode);
        Assert.NotNull(state);
        Assert.Equal(DeviceStatus.Online, state.Status);
        Assert.Equal(2, state.Metrics.Count);
        Assert.Equal(42.3, state.Metrics.Single(metric => metric.Type == MetricType.CpuUsage).Value);
        Assert.Equal(67.1, state.Metrics.Single(metric => metric.Type == MetricType.MemoryUsage).Value);
        var database = RedisDatabase();
        Assert.True(await database.KeyExistsAsync(StateKey(device.Id)));
        var ttl = await database.KeyTimeToLiveAsync(StateKey(device.Id));
        Assert.NotNull(ttl);
        Assert.InRange(ttl.Value, TimeSpan.FromHours(23), TimeSpan.FromHours(24));
    }

    [MySqlIntegrationFact]
    public async Task PartialTelemetry_PreservesPreviouslyKnownMetrics()
    {
        await fixture.FlushRedisAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDeviceAsync(client);
        var initialTimestamp = DateTime.UtcNow.AddMinutes(-2);
        await PostTelemetryAsync(client, device.DeviceKey, initialTimestamp,
            ("CpuUsage", 20, "%"), ("MemoryUsage", 60, "%"));

        await PostTelemetryAsync(client, device.DeviceKey, initialTimestamp.AddMinutes(1),
            ("CpuUsage", 30, "%"));
        var state = await GetStateAsync(client, device.Id);

        Assert.Equal(2, state.Metrics.Count);
        Assert.Equal(30, state.Metrics.Single(metric => metric.Type == MetricType.CpuUsage).Value);
        Assert.Equal(60, state.Metrics.Single(metric => metric.Type == MetricType.MemoryUsage).Value);
    }

    [MySqlIntegrationFact]
    public async Task OlderTelemetry_DoesNotReplaceNewerRedisMetric()
    {
        await fixture.FlushRedisAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDeviceAsync(client);
        var newerTimestamp = DateTime.UtcNow.AddMinutes(-1);
        await PostTelemetryAsync(client, device.DeviceKey, newerTimestamp, ("CpuUsage", 80, "%"));

        await PostTelemetryAsync(client, device.DeviceKey, newerTimestamp.AddMinutes(-5), ("CpuUsage", 10, "%"));
        var cpu = (await GetStateAsync(client, device.Id)).Metrics.Single();

        Assert.Equal(80, cpu.Value);
        AssertUtcTimestampEqual(newerTimestamp, cpu.Timestamp);
    }

    [MySqlIntegrationFact]
    public async Task CacheMiss_RebuildsFromMySqlAndWritesRedisState()
    {
        await fixture.FlushRedisAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDeviceAsync(client);
        await PostTelemetryAsync(client, device.DeviceKey, DateTime.UtcNow.AddMinutes(-1),
            ("CpuUsage", 45, "%"), ("DiskUsage", 70, "%"));
        var database = RedisDatabase();
        await database.KeyDeleteAsync(StateKey(device.Id));

        var state = await GetStateAsync(client, device.Id);

        Assert.Equal(2, state.Metrics.Count);
        Assert.True(await database.KeyExistsAsync(StateKey(device.Id)));
    }

    [MySqlIntegrationFact]
    public async Task PhysicalDeviceDelete_RemovesRedisState()
    {
        await fixture.FlushRedisAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDeviceAsync(client);
        await GetStateAsync(client, device.Id);
        Assert.True(await RedisDatabase().KeyExistsAsync(StateKey(device.Id)));

        var response = await client.DeleteAsync($"/api/v1/devices/{device.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await RedisDatabase().KeyExistsAsync(StateKey(device.Id)));
    }

    [MySqlIntegrationFact]
    public async Task RedisOutage_DoesNotPreventDurableMySqlTelemetryPersistence()
    {
        using var healthyFactory = CreateFactory();
        using var healthyClient = healthyFactory.CreateClient();
        var device = await CreateDeviceAsync(healthyClient);
        using var unavailableFactory = new MetricsHubApiFactory(
            fixture.ConnectionString,
            "127.0.0.1:6399,abortConnect=false,connectTimeout=250,syncTimeout=250,asyncTimeout=250");
        using var unavailableClient = unavailableFactory.CreateClient();

        var response = await PostTelemetryAsync(
            unavailableClient, device.DeviceKey, DateTime.UtcNow, ("CpuUsage", 55, "%"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var context = fixture.CreateDbContext();
        Assert.True(await context.TelemetryPoints.AnyAsync(point => point.DeviceId == device.Id));
    }

    [MySqlIntegrationFact]
    public async Task ConcurrentUpdates_KeepNewestMetricTimestamp()
    {
        await fixture.FlushRedisAsync();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDeviceAsync(client);
        var older = DateTime.UtcNow.AddMinutes(-2);
        var newer = older.AddMinutes(1);

        var requests = new[]
        {
            PostTelemetryAsync(client, device.DeviceKey, newer, ("CpuUsage", 90, "%")),
            PostTelemetryAsync(client, device.DeviceKey, older, ("CpuUsage", 15, "%"))
        };
        var responses = await Task.WhenAll(requests);
        var cpu = (await GetStateAsync(client, device.Id)).Metrics.Single();

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
        Assert.Equal(90, cpu.Value);
        AssertUtcTimestampEqual(newer, cpu.Timestamp);
    }

    private MetricsHubApiFactory CreateFactory() =>
        new(fixture.ConnectionString, fixture.RedisConnectionString);

    private IDatabase RedisDatabase() => fixture.RedisConnection.GetDatabase();

    private static string StateKey(Guid deviceId) => $"metricshub:device:{deviceId:D}:state";

    private static async Task<DeviceResponse> CreateDeviceAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/devices", new
        {
            deviceKey = $"redis-device-{Guid.NewGuid():N}",
            name = "Redis integration device",
            type = "Server",
            hostname = "REDIS-TEST",
            operatingSystem = "Linux",
            location = "Test lab"
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceResponse>(JsonOptions))!;
    }

    private static Task<HttpResponseMessage> PostTelemetryAsync(
        HttpClient client,
        string deviceKey,
        DateTime timestamp,
        params (string Type, double Value, string Unit)[] metrics) =>
        client.PostAsJsonAsync("/api/v1/telemetry", new
        {
            deviceKey,
            timestamp,
            metrics = metrics.Select(metric => new
            {
                type = metric.Type,
                value = metric.Value,
                unit = metric.Unit
            })
        }, JsonOptions);

    private static async Task<DeviceStateResponse> GetStateAsync(HttpClient client, Guid deviceId) =>
        (await client.GetFromJsonAsync<DeviceStateResponse>(
            $"/api/v1/devices/{deviceId}/state", JsonOptions))!;

    private static void AssertUtcTimestampEqual(DateTime expected, DateTime actual)
    {
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMicroseconds(1));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
