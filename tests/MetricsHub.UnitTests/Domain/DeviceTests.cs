using MetricsHub.Domain.Entities;
using MetricsHub.Domain.Enums;

namespace MetricsHub.UnitTests.Domain;

public sealed class DeviceTests
{
    [Fact]
    public void Constructor_SetsStatusToUnknown()
    {
        var device = CreateDevice();

        Assert.Equal(DeviceStatus.Unknown, device.Status);
    }

    [Fact]
    public void Constructor_EnablesDevice()
    {
        var device = CreateDevice();

        Assert.True(device.IsEnabled);
    }

    [Fact]
    public void Constructor_UsesUtcCreationTime()
    {
        var device = CreateDevice();

        Assert.Equal(DateTimeKind.Utc, device.CreatedAt.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Device(name, "device-key", DeviceType.Server));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyDeviceKey(string deviceKey)
    {
        Assert.Throws<ArgumentException>(() =>
            new Device("Production server", deviceKey, DeviceType.Server));
    }

    [Fact]
    public void UpdateDetails_ChangesMutableFieldsButPreservesDeviceKey()
    {
        var device = CreateDevice();
        var originalDeviceKey = device.DeviceKey;

        device.UpdateDetails(
            "Updated device",
            DeviceType.Workstation,
            "UPDATED-PC",
            "Windows 11 Pro",
            "Zlin",
            false);

        Assert.Equal(originalDeviceKey, device.DeviceKey);
        Assert.Equal("Updated device", device.Name);
        Assert.Equal(DeviceType.Workstation, device.Type);
        Assert.False(device.IsEnabled);
    }

    [Fact]
    public void RecordTelemetry_SetsOnlineStatusAndLastSeenTime()
    {
        var device = CreateDevice();
        var timestamp = DateTime.UtcNow;

        device.RecordTelemetry(timestamp);

        Assert.Equal(DeviceStatus.Online, device.Status);
        Assert.Equal(timestamp, device.LastSeenAt);
    }

    [Fact]
    public void RecordTelemetry_WithOlderTimestamp_DoesNotMoveLastSeenBackward()
    {
        var device = CreateDevice();
        var latestTimestamp = DateTime.UtcNow;
        device.RecordTelemetry(latestTimestamp);

        device.RecordTelemetry(latestTimestamp.AddMinutes(-5));

        Assert.Equal(latestTimestamp, device.LastSeenAt);
    }

    private static Device CreateDevice() =>
        new("Production server", "production-server-01", DeviceType.Server);
}
