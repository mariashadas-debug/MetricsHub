using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MetricsHub.Application.AlertRules;
using MetricsHub.Application.Devices;
using MetricsHub.Application.Realtime;
using MetricsHub.Domain.Enums;
using MetricsHub.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MetricsHub.Application.Monitoring;

namespace MetricsHub.IntegrationTests.Api;

[Collection(InfrastructureCollection.Name)]
public sealed class SignalRIntegrationTests(MySqlDatabaseFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [MySqlIntegrationFact]
    public async Task DeviceGroupReceivesTelemetryStateAndAlertLifecycleEvents()
    {
        using var factory = new MetricsHubApiFactory(fixture.ConnectionString, fixture.RedisConnectionString);
        using var client = factory.CreateClient();
        var device = await CreateDevice(client);
        await CreateRule(client, device.Id);
        await using var connection = CreateConnection(factory);
        var telemetry = Completion<TelemetryReceivedEvent>();
        var state = Completion<DeviceStateUpdatedEvent>();
        var raised = Completion<AlertRaisedEvent>();
        var resolved = Completion<AlertResolvedEvent>();
        connection.On<TelemetryReceivedEvent>("TelemetryReceived", value => telemetry.TrySetResult(value));
        connection.On<DeviceStateUpdatedEvent>("DeviceStateUpdated", value => state.TrySetResult(value));
        connection.On<AlertRaisedEvent>("AlertRaised", value => raised.TrySetResult(value));
        connection.On<AlertResolvedEvent>("AlertResolved", value => resolved.TrySetResult(value));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToDevice", device.Id);
        await SendCpu(client, device.DeviceKey, 90);

        Assert.Equal(device.Id, (await telemetry.Task.WaitAsync(TimeSpan.FromSeconds(10))).DeviceId);
        Assert.Equal(device.Id, (await state.Task.WaitAsync(TimeSpan.FromSeconds(10))).DeviceId);
        var raisedEvent = await raised.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(device.Id, raisedEvent.DeviceId);

        await SendCpu(client, device.DeviceKey, 10);
        Assert.Equal(raisedEvent.AlertId, (await resolved.Task.WaitAsync(TimeSpan.FromSeconds(10))).AlertId);
    }

    [MySqlIntegrationFact]
    public async Task OfflineTransitionEmitsDeviceStatusChanged()
    {
        using var factory = new MetricsHubApiFactory(fixture.ConnectionString, fixture.RedisConnectionString, 30, 3600);
        using var client = factory.CreateClient();
        var device = await CreateDevice(client);
        await SendCpu(client, device.DeviceKey, 10, DateTime.UtcNow.AddMinutes(-2));
        await using var connection = CreateConnection(factory);
        var changed = Completion<DeviceStatusChangedEvent>();
        connection.On<DeviceStatusChangedEvent>("DeviceStatusChanged", value =>
        {
            if (value.DeviceId == device.Id && value.NewStatus == DeviceStatus.Offline) changed.TrySetResult(value);
        });
        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToDevice", device.Id);

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDeviceMonitoringService>()
                .MarkStaleDevicesOfflineAsync(CancellationToken.None);
        }

        var transition = await changed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(DeviceStatus.Online, transition.PreviousStatus);
        Assert.Equal(DeviceStatus.Offline, transition.NewStatus);
    }

    private static HubConnection CreateConnection(MetricsHubApiFactory factory) =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/monitoring", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

    private static TaskCompletionSource<T> Completion<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task<DeviceResponse> CreateDevice(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/devices", new { deviceKey = $"signalr-{Guid.NewGuid():N}", name = "SignalR device", type = "Server" }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceResponse>(JsonOptions))!;
    }

    private static async Task CreateRule(HttpClient client, Guid deviceId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/alert-rules", new
        {
            deviceId, name = "SignalR CPU", metricType = "CpuUsage", @operator = "GreaterThan", threshold = 50, severity = "Critical", isEnabled = true
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    private static async Task SendCpu(HttpClient client, string key, double value, DateTime? timestamp = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/telemetry", new
        {
            deviceKey = key, timestamp = timestamp ?? DateTime.UtcNow, metrics = new[] { new { type = "CpuUsage", value, unit = "%" } }
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
