using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class TelemetryPoint
{
    private TelemetryPoint()
    {
        Unit = null!;
        Device = null!;
    }

    public TelemetryPoint(
        Device device,
        MetricType metricType,
        double value,
        string unit,
        DateTime timestamp)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(unit);

        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must use UTC.", nameof(timestamp));
        }

        Device = device;
        DeviceId = device.Id;
        MetricType = metricType;
        Value = value;
        Unit = unit;
        Timestamp = timestamp;
    }

    public long Id { get; private set; }

    public Guid DeviceId { get; private set; }

    public MetricType MetricType { get; private set; }

    public double Value { get; private set; }

    public string Unit { get; private set; }

    public DateTime Timestamp { get; private set; }

    public Device Device { get; private set; }
}
