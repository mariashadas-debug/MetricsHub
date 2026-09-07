using MetricsHub.Domain.Entities;
using MetricsHub.Domain.Enums;

namespace MetricsHub.UnitTests.Domain;

public sealed class DeviceOfflineTests
{
    [Fact]
    public void NeverSeenDevice_RemainsUnknown()
    {
        var device = new Device("New", "new-01", DeviceType.Server);
        Assert.False(device.MarkOffline());
        Assert.Equal(DeviceStatus.Unknown, device.Status);
    }

    [Fact]
    public void OnlineDevice_CanTransitionOfflineOnlyOnce()
    {
        var device = new Device("Server", "server-01", DeviceType.Server);
        device.RecordTelemetry(DateTime.UtcNow);
        Assert.True(device.MarkOffline());
        Assert.False(device.MarkOffline());
        Assert.Equal(DeviceStatus.Offline, device.Status);
    }

    [Fact]
    public void Telemetry_ReturnsOfflineDeviceOnline()
    {
        var device = new Device("Server", "server-01", DeviceType.Server);
        device.RecordTelemetry(DateTime.UtcNow.AddMinutes(-1));
        device.MarkOffline();
        device.RecordTelemetry(DateTime.UtcNow);
        Assert.Equal(DeviceStatus.Online, device.Status);
    }
}
