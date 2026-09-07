using MetricsHub.Domain.Entities;
using MetricsHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MetricsHub.IntegrationTests.Persistence;

public sealed class MetricsHubPersistenceTests(MySqlDatabaseFixture fixture)
    : IClassFixture<MySqlDatabaseFixture>
{
    [MySqlIntegrationFact]
    public async Task Device_CanBePersistedAndLoaded()
    {
        var device = CreateDevice();

        await using (var writeContext = fixture.CreateDbContext())
        {
            writeContext.Devices.Add(device);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateDbContext();
        var loaded = await readContext.Devices.SingleAsync(candidate => candidate.Id == device.Id);

        Assert.Equal(device.DeviceKey, loaded.DeviceKey);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
    }

    [MySqlIntegrationFact]
    public async Task TelemetryPoint_CanBePersistedForDeviceAndLoaded()
    {
        var device = CreateDevice();
        var timestamp = DateTime.UtcNow;
        var telemetryPoint = new TelemetryPoint(device, MetricType.CpuUsage, 48.2, "%", timestamp);

        await using (var writeContext = fixture.CreateDbContext())
        {
            writeContext.TelemetryPoints.Add(telemetryPoint);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateDbContext();
        var loaded = await readContext.TelemetryPoints
            .Include(point => point.Device)
            .SingleAsync(point => point.Id == telemetryPoint.Id);

        Assert.Equal(device.Id, loaded.DeviceId);
        Assert.Equal(MetricType.CpuUsage, loaded.MetricType);
        Assert.Equal(48.2, loaded.Value);
        Assert.Equal("%", loaded.Unit);
        Assert.Equal(DateTimeKind.Utc, loaded.Timestamp.Kind);
        Assert.Equal(device.DeviceKey, loaded.Device.DeviceKey);
    }

    [MySqlIntegrationFact]
    public async Task DeviceKey_DuplicateValueIsRejectedByDatabase()
    {
        var deviceKey = $"duplicate-{Guid.NewGuid():N}";

        await using (var firstContext = fixture.CreateDbContext())
        {
            firstContext.Devices.Add(new Device("First device", deviceKey, DeviceType.Server));
            await firstContext.SaveChangesAsync();
        }

        await using var secondContext = fixture.CreateDbContext();
        secondContext.Devices.Add(new Device("Second device", deviceKey, DeviceType.Workstation));

        await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());
    }

    [MySqlIntegrationFact]
    public async Task AlertRule_CanBePersisted()
    {
        var rule = new AlertRule(
            "High CPU usage",
            MetricType.CpuUsage,
            ComparisonOperator.GreaterThan,
            90,
            AlertSeverity.Critical);

        await using (var writeContext = fixture.CreateDbContext())
        {
            writeContext.AlertRules.Add(rule);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateDbContext();
        var loaded = await readContext.AlertRules.SingleAsync(candidate => candidate.Id == rule.Id);

        Assert.Equal(90, loaded.Threshold);
        Assert.Null(loaded.DeviceId);
    }

    [MySqlIntegrationFact]
    public async Task Alert_CanBePersistedWithDeviceAssociation()
    {
        var device = CreateDevice();
        var alert = new Alert(device, AlertSeverity.Warning, "Memory usage exceeded 85%.");

        await using (var writeContext = fixture.CreateDbContext())
        {
            writeContext.Alerts.Add(alert);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateDbContext();
        var loaded = await readContext.Alerts
            .Include(candidate => candidate.Device)
            .SingleAsync(candidate => candidate.Id == alert.Id);

        Assert.Equal(device.Id, loaded.DeviceId);
        Assert.Equal(device.DeviceKey, loaded.Device.DeviceKey);
        Assert.False(loaded.IsResolved);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
    }

    private static Device CreateDevice() =>
        new("Integration test device", $"device-{Guid.NewGuid():N}", DeviceType.Server);
}
