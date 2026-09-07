using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsHub.Application.Devices;
using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Enums;
using MetricsHub.IntegrationTests.Persistence;

namespace MetricsHub.IntegrationTests.Api;

[Collection(InfrastructureCollection.Name)]
public sealed class MetricsHubApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly MetricsHubApiFactory _factory;
    private readonly HttpClient _client;

    public MetricsHubApiTests(MySqlDatabaseFixture fixture)
    {
        _factory = new MetricsHubApiFactory(fixture.ConnectionString, fixture.RedisConnectionString);
        _client = _factory.CreateClient();
    }

    [MySqlIntegrationFact]
    public async Task PostDevice_CreatesDevice()
    {
        var response = await CreateDeviceAsync();

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(DeviceStatus.Unknown, response.Status);
        Assert.True(response.IsEnabled);
    }

    [MySqlIntegrationFact]
    public async Task PostDevice_WithDuplicateDeviceKey_ReturnsConflict()
    {
        var deviceKey = NewDeviceKey();
        await CreateDeviceAsync(deviceKey);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/devices",
            CreateDeviceRequest(deviceKey),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [MySqlIntegrationFact]
    public async Task GetDevice_ReturnsPersistedDevice()
    {
        var created = await CreateDeviceAsync();

        var response = await _client.GetAsync($"/api/v1/devices/{created.Id}");
        var loaded = await response.Content.ReadFromJsonAsync<DeviceResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal(created.DeviceKey, loaded.DeviceKey);
        Assert.Equal(created.Name, loaded.Name);
        AssertUtcTimestampEqual(created.CreatedAt, loaded.CreatedAt);
    }

    [MySqlIntegrationFact]
    public async Task GetDevice_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [MySqlIntegrationFact]
    public async Task PutDevice_UpdatesMutableDetailsWithoutChangingDeviceKey()
    {
        var created = await CreateDeviceAsync();
        var update = new
        {
            name = "Updated workstation",
            type = "Workstation",
            hostname = "UPDATED-PC",
            operatingSystem = "Windows 11 Pro",
            location = "Zlin",
            isEnabled = false
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/devices/{created.Id}",
            update,
            JsonOptions);
        var updated = await _client.GetFromJsonAsync<DeviceResponse>(
            $"/api/v1/devices/{created.Id}",
            JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(created.DeviceKey, updated.DeviceKey);
        Assert.Equal("Updated workstation", updated.Name);
        Assert.Equal("Windows 11 Pro", updated.OperatingSystem);
        Assert.False(updated.IsEnabled);
    }

    [MySqlIntegrationFact]
    public async Task PostTelemetry_StoresMultiplePointsAndUpdatesDeviceState()
    {
        var device = await CreateDeviceAsync();
        var timestamp = DateTime.UtcNow.AddMinutes(-1);

        var response = await PostTelemetryAsync(device.DeviceKey, timestamp,
            ("CpuUsage", 43.8, "%"),
            ("MemoryUsage", 68.2, "%"));
        var history = await _client.GetFromJsonAsync<TelemetryPointResponse[]>(
            $"/api/v1/devices/{device.Id}/telemetry",
            JsonOptions);
        var updatedDevice = await _client.GetFromJsonAsync<DeviceResponse>(
            $"/api/v1/devices/{device.Id}",
            JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(history);
        Assert.Equal(2, history.Length);
        Assert.NotNull(updatedDevice);
        Assert.Equal(DeviceStatus.Online, updatedDevice.Status);
        Assert.NotNull(updatedDevice.LastSeenAt);
        AssertUtcTimestampEqual(timestamp, updatedDevice.LastSeenAt.Value);
    }

    [MySqlIntegrationFact]
    public async Task PostTelemetry_WithUnknownDeviceKey_ReturnsNotFound()
    {
        var response = await PostTelemetryAsync(NewDeviceKey(), DateTime.UtcNow,
            ("CpuUsage", 43.8, "%"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [MySqlIntegrationFact]
    public async Task GetTelemetryHistory_AppliesMetricAndDateFiltersNewestFirst()
    {
        var device = await CreateDeviceAsync();
        var firstTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var secondTimestamp = firstTimestamp.AddMinutes(5);
        await PostTelemetryAsync(device.DeviceKey, firstTimestamp,
            ("CpuUsage", 10, "%"),
            ("MemoryUsage", 20, "%"));
        await PostTelemetryAsync(device.DeviceKey, secondTimestamp,
            ("CpuUsage", 30, "%"));

        var from = Uri.EscapeDataString(firstTimestamp.AddMinutes(1).ToString("O"));
        var to = Uri.EscapeDataString(secondTimestamp.AddMinutes(1).ToString("O"));
        var history = await _client.GetFromJsonAsync<TelemetryPointResponse[]>(
            $"/api/v1/devices/{device.Id}/telemetry?metricType=CpuUsage&from={from}&to={to}&limit=10",
            JsonOptions);

        Assert.NotNull(history);
        var point = Assert.Single(history);
        Assert.Equal(30, point.Value);
        AssertUtcTimestampEqual(secondTimestamp, point.Timestamp);
    }

    [MySqlIntegrationFact]
    public async Task GetLatestTelemetry_ReturnsLatestPointForEachMetricType()
    {
        var device = await CreateDeviceAsync();
        var firstTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var secondTimestamp = firstTimestamp.AddMinutes(5);
        await PostTelemetryAsync(device.DeviceKey, firstTimestamp,
            ("CpuUsage", 10, "%"),
            ("MemoryUsage", 20, "%"));
        await PostTelemetryAsync(device.DeviceKey, secondTimestamp,
            ("CpuUsage", 30, "%"));

        var latest = await _client.GetFromJsonAsync<LatestTelemetryResponse>(
            $"/api/v1/devices/{device.Id}/telemetry/latest",
            JsonOptions);

        Assert.NotNull(latest);
        Assert.NotNull(latest.Timestamp);
        AssertUtcTimestampEqual(secondTimestamp, latest.Timestamp.Value);
        Assert.Equal(2, latest.Metrics.Count);
        Assert.Equal(30, latest.Metrics.Single(metric => metric.Type == MetricType.CpuUsage).Value);
        AssertUtcTimestampEqual(
            firstTimestamp,
            latest.Metrics.Single(metric => metric.Type == MetricType.MemoryUsage).Timestamp);
    }

    [MySqlIntegrationFact]
    public async Task PostTelemetry_WithNonUtcTimestamp_ReturnsBadRequest()
    {
        var device = await CreateDeviceAsync();
        var request = new
        {
            deviceKey = device.DeviceKey,
            timestamp = "2026-09-07T18:30:00",
            metrics = new[] { new { type = "CpuUsage", value = 1, unit = "%" } }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/telemetry", request, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [MySqlIntegrationFact]
    public async Task DeleteDevice_WithHistoricalTelemetry_ReturnsConflict()
    {
        var device = await CreateDeviceAsync();
        await PostTelemetryAsync(device.DeviceKey, DateTime.UtcNow,
            ("CpuUsage", 43.8, "%"));

        var response = await _client.DeleteAsync($"/api/v1/devices/{device.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<DeviceResponse> CreateDeviceAsync(string? deviceKey = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/devices",
            CreateDeviceRequest(deviceKey ?? NewDeviceKey()),
            JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceResponse>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> PostTelemetryAsync(
        string deviceKey,
        DateTime timestamp,
        params (string Type, double Value, string Unit)[] metrics) =>
        _client.PostAsJsonAsync("/api/v1/telemetry", new
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

    private static object CreateDeviceRequest(string deviceKey) => new
    {
        deviceKey,
        name = "API integration device",
        type = "Workstation",
        hostname = "TEST-PC",
        operatingSystem = "Windows 11",
        location = "Zlin"
    };

    private static string NewDeviceKey() => $"api-device-{Guid.NewGuid():N}";

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
