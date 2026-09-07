using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsHub.Api.Contracts.DeviceStates;
using MetricsHub.Application.Alerts;
using MetricsHub.Application.AlertRules;
using MetricsHub.Application.Devices;
using MetricsHub.Application.Monitoring;
using MetricsHub.Application.Abstractions.Realtime;
using MetricsHub.Application.Realtime;
using MetricsHub.Domain.Enums;
using MetricsHub.IntegrationTests.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MetricsHub.IntegrationTests.Api;

[Collection(InfrastructureCollection.Name)]
public sealed class AlertAndMonitoringIntegrationTests(MySqlDatabaseFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [MySqlIntegrationFact]
    public async Task AlertRuleCrud_WorksAndListCanBeFiltered()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDevice(client);
        var created = await CreateRule(client, device.Id, 50);

        var loaded = await client.GetFromJsonAsync<AlertRuleResponse>($"/api/v1/alert-rules/{created.Id}", JsonOptions);
        var update = RuleRequest(device.Id, "Updated CPU", 60, true);
        var put = await client.PutAsJsonAsync($"/api/v1/alert-rules/{created.Id}", update, JsonOptions);
        var rules = await client.GetFromJsonAsync<AlertRuleResponse[]>("/api/v1/alert-rules", JsonOptions);
        var delete = await client.DeleteAsync($"/api/v1/alert-rules/{created.Id}");

        Assert.NotNull(loaded);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Contains(rules!, rule => rule.Id == created.Id && rule.Threshold == 60);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [MySqlIntegrationFact]
    public async Task AlertLifecycle_PreventsDuplicatesResolvesAndAllowsLaterAlert()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var device = await CreateDevice(client);
        await CreateRule(client, device.Id, 50);

        await SendCpu(client, device.DeviceKey, 80);
        await SendCpu(client, device.DeviceKey, 90);
        var active = await GetAlerts(client, device.Id, false);
        var first = Assert.Single(active);

        await SendCpu(client, device.DeviceKey, 20);
        var resolved = await GetAlerts(client, device.Id, true);
        Assert.Contains(resolved, alert => alert.Id == first.Id && alert.ResolvedAt.HasValue);

        await SendCpu(client, device.DeviceKey, 75);
        var all = await GetAlerts(client, device.Id, null);
        Assert.Equal(2, all.Length);
        Assert.Single(all, alert => !alert.IsResolved);
    }

    [MySqlIntegrationFact]
    public async Task GlobalDeviceSpecificAndDisabledRules_ApplyToCorrectDevices()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var first = await CreateDevice(client);
        var second = await CreateDevice(client);
        await CreateRule(client, null, 50, "Global");
        await CreateRule(client, first.Id, 60, "First only");
        await CreateRule(client, second.Id, 10, "Disabled", false);

        await SendCpu(client, first.DeviceKey, 80);
        await SendCpu(client, second.DeviceKey, 80);
        var firstAlerts = await GetAlerts(client, first.Id, null);
        var secondAlerts = await GetAlerts(client, second.Id, null);

        Assert.Equal(2, firstAlerts.Length);
        Assert.Single(secondAlerts);
    }

    [MySqlIntegrationFact]
    public async Task OfflineService_UpdatesMySqlRedisAndTelemetryReturnsDeviceOnline()
    {
        await fixture.FlushRedisAsync();
        using var factory = new MetricsHubApiFactory(fixture.ConnectionString, fixture.RedisConnectionString, 30, 3600);
        using var client = factory.CreateClient();
        var device = await CreateDevice(client);
        await SendCpu(client, device.DeviceKey, 20, DateTime.UtcNow.AddMinutes(-2));

        using (var scope = factory.Services.CreateScope())
        {
            var changed = await scope.ServiceProvider.GetRequiredService<IDeviceMonitoringService>()
                .MarkStaleDevicesOfflineAsync(CancellationToken.None);
            Assert.True(changed >= 1);
        }

        var offline = await client.GetFromJsonAsync<DeviceResponse>($"/api/v1/devices/{device.Id}", JsonOptions);
        var state = await client.GetFromJsonAsync<DeviceStateResponse>($"/api/v1/devices/{device.Id}/state", JsonOptions);
        Assert.Equal(DeviceStatus.Offline, offline!.Status);
        Assert.Equal(DeviceStatus.Offline, state!.Status);
        Assert.Single(state.Metrics);

        await SendCpu(client, device.DeviceKey, 25, DateTime.UtcNow);
        var online = await client.GetFromJsonAsync<DeviceResponse>($"/api/v1/devices/{device.Id}", JsonOptions);
        Assert.Equal(DeviceStatus.Online, online!.Status);
    }

    [MySqlIntegrationFact]
    public async Task RedisOutage_DoesNotPreventAlertPersistence()
    {
        using var healthyFactory = CreateFactory();
        using var healthyClient = healthyFactory.CreateClient();
        var device = await CreateDevice(healthyClient);
        await CreateRule(healthyClient, device.Id, 50);
        using var outageFactory = new MetricsHubApiFactory(fixture.ConnectionString, "127.0.0.1:6399,abortConnect=false,connectTimeout=250,syncTimeout=250,asyncTimeout=250");
        using var outageClient = outageFactory.CreateClient();

        var response = await SendCpu(outageClient, device.DeviceKey, 90);
        var alerts = await GetAlerts(healthyClient, device.Id, false);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(alerts);
    }

    [MySqlIntegrationFact]
    public async Task SignalRFailure_DoesNotPreventTelemetryOrAlertPersistence()
    {
        using var setupFactory = CreateFactory();
        using var setupClient = setupFactory.CreateClient();
        var device = await CreateDevice(setupClient);
        await CreateRule(setupClient, device.Id, 50);
        using var failingFactory = new MetricsHubApiFactory(
            fixture.ConnectionString, fixture.RedisConnectionString,
            configureServices: services => services.AddSingleton<IRealtimeNotifier, FailingNotifier>());
        using var client = failingFactory.CreateClient();

        var response = await SendCpu(client, device.DeviceKey, 90);
        var alerts = await GetAlerts(setupClient, device.Id, false);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(alerts);
        await using var context = fixture.CreateDbContext();
        Assert.True(await context.TelemetryPoints.AnyAsync(point => point.DeviceId == device.Id));
    }

    private MetricsHubApiFactory CreateFactory() => new(fixture.ConnectionString, fixture.RedisConnectionString);

    private static async Task<DeviceResponse> CreateDevice(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/devices", new { deviceKey = $"phase7-{Guid.NewGuid():N}", name = "Phase 7 device", type = "Server" }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceResponse>(JsonOptions))!;
    }

    private static async Task<AlertRuleResponse> CreateRule(HttpClient client, Guid? deviceId, double threshold, string name = "High CPU", bool enabled = true)
    {
        var response = await client.PostAsJsonAsync("/api/v1/alert-rules", RuleRequest(deviceId, name, threshold, enabled), JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AlertRuleResponse>(JsonOptions))!;
    }

    private static object RuleRequest(Guid? deviceId, string name, double threshold, bool enabled) => new
    {
        deviceId, name, metricType = "CpuUsage", @operator = "GreaterThan", threshold, severity = "Critical", isEnabled = enabled
    };

    private static Task<HttpResponseMessage> SendCpu(HttpClient client, string key, double value, DateTime? timestamp = null) =>
        client.PostAsJsonAsync("/api/v1/telemetry", new
        {
            deviceKey = key,
            timestamp = timestamp ?? DateTime.UtcNow,
            metrics = new[] { new { type = "CpuUsage", value, unit = "%" } }
        }, JsonOptions);

    private static async Task<AlertResponse[]> GetAlerts(HttpClient client, Guid deviceId, bool? resolved)
    {
        var suffix = resolved.HasValue ? $"&isResolved={resolved.Value.ToString().ToLowerInvariant()}" : string.Empty;
        return (await client.GetFromJsonAsync<AlertResponse[]>($"/api/v1/alerts?deviceId={deviceId}&limit=20{suffix}", JsonOptions))!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class FailingNotifier : IRealtimeNotifier
    {
        private static Task Fail() => Task.FromException(new InvalidOperationException("Expected test notification failure."));
        public Task DeviceStateUpdatedAsync(DeviceStateUpdatedEvent payload, CancellationToken cancellationToken) => Fail();
        public Task TelemetryReceivedAsync(TelemetryReceivedEvent payload, CancellationToken cancellationToken) => Fail();
        public Task DeviceStatusChangedAsync(DeviceStatusChangedEvent payload, CancellationToken cancellationToken) => Fail();
        public Task AlertRaisedAsync(AlertRaisedEvent payload, CancellationToken cancellationToken) => Fail();
        public Task AlertResolvedAsync(AlertResolvedEvent payload, CancellationToken cancellationToken) => Fail();
    }
}
