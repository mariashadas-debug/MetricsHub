using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class Device
{
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

    public Guid Id { get; }

    public string Name { get; }

    public string DeviceKey { get; }

    public DeviceType Type { get; }

    public DeviceStatus Status { get; private set; }

    public string? Hostname { get; private set; }

    public string? OperatingSystem { get; private set; }

    public string? Location { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? LastSeenAt { get; private set; }

    public ICollection<TelemetryPoint> TelemetryPoints { get; } = [];

    public ICollection<Alert> Alerts { get; } = [];

    public ICollection<AlertRule> AlertRules { get; } = [];
}
