using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class TelemetryPoint
{
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

    public Guid DeviceId { get; }

    public MetricType MetricType { get; }

    public double Value { get; }

    public string Unit { get; }

    public DateTime Timestamp { get; }

    public Device Device { get; }
}
