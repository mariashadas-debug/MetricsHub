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

    private static Device CreateDevice() =>
        new("Production server", "production-server-01", DeviceType.Server);
}
