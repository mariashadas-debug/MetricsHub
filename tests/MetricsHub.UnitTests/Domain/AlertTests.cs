using MetricsHub.Domain.Entities;
using MetricsHub.Domain.Enums;

namespace MetricsHub.UnitTests.Domain;

public sealed class AlertTests
{
    [Fact]
    public void Constructor_StartsUnresolved()
    {
        var alert = CreateAlert();

        Assert.False(alert.IsResolved);
        Assert.Null(alert.ResolvedAt);
    }

    [Fact]
    public void Resolve_MarksAlertResolved()
    {
        var alert = CreateAlert();

        alert.Resolve();

        Assert.True(alert.IsResolved);
    }

    [Fact]
    public void Resolve_SetsResolvedAtToUtcTime()
    {
        var beforeResolution = DateTime.UtcNow;
        var alert = CreateAlert();

        alert.Resolve();

        Assert.NotNull(alert.ResolvedAt);
        Assert.Equal(DateTimeKind.Utc, alert.ResolvedAt.Value.Kind);
        Assert.InRange(alert.ResolvedAt.Value, beforeResolution, DateTime.UtcNow);
    }

    [Fact]
    public void Resolve_WhenAlreadyResolved_PreservesResolvedAt()
    {
        var alert = CreateAlert();
        alert.Resolve();
        var firstResolvedAt = alert.ResolvedAt;

        alert.Resolve();

        Assert.Equal(firstResolvedAt, alert.ResolvedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyMessage(string message)
    {
        Assert.Throws<ArgumentException>(() =>
            new Alert(CreateDevice(), AlertSeverity.Warning, message));
    }

    private static Alert CreateAlert() =>
        new(CreateDevice(), AlertSeverity.Warning, "CPU usage exceeded 90%.");

    private static Device CreateDevice() =>
        new("Production server", "production-server-01", DeviceType.Server);
}
