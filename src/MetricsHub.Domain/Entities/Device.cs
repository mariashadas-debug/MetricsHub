using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class Device
{
    private Device()
    {
        Name = null!;
        DeviceKey = null!;
    }

    public Device(
        string name,
        string deviceKey,
        DeviceType type,
        string? hostname = null,
        string? operatingSystem = null,
        string? location = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceKey);

        Id = Guid.NewGuid();
        Name = name;
        DeviceKey = deviceKey;
        Type = type;
        Status = DeviceStatus.Unknown;
        Hostname = hostname;
        OperatingSystem = operatingSystem;
        Location = location;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string DeviceKey { get; private set; }

    public DeviceType Type { get; private set; }

    public DeviceStatus Status { get; private set; }

    public string? Hostname { get; private set; }

    public string? OperatingSystem { get; private set; }

    public string? Location { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? LastSeenAt { get; private set; }

    public ICollection<TelemetryPoint> TelemetryPoints { get; } = [];

    public ICollection<Alert> Alerts { get; } = [];

    public ICollection<AlertRule> AlertRules { get; } = [];

    public void UpdateDetails(
        string name,
        DeviceType type,
        string? hostname,
        string? operatingSystem,
        string? location,
        bool isEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Type = type;
        Hostname = hostname;
        OperatingSystem = operatingSystem;
        Location = location;
        IsEnabled = isEnabled;
    }

    public void RecordTelemetry(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must use UTC.", nameof(timestamp));
        }

        Status = DeviceStatus.Online;

        if (LastSeenAt is null || timestamp > LastSeenAt)
        {
            LastSeenAt = timestamp;
        }
    }
}
