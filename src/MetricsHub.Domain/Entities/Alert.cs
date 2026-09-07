using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class Alert
{
    public Alert(
        Device device,
        AlertSeverity severity,
        string message,
        AlertRule? alertRule = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Id = Guid.NewGuid();
        DeviceId = device.Id;
        AlertRuleId = alertRule?.Id;
        Severity = severity;
        Message = message;
        CreatedAt = DateTime.UtcNow;
        IsResolved = false;
        Device = device;
        AlertRule = alertRule;
    }

    public Guid Id { get; }

    public Guid DeviceId { get; }

    public Guid? AlertRuleId { get; }

    public AlertSeverity Severity { get; }

    public string Message { get; }

    public DateTime CreatedAt { get; }

    public DateTime? ResolvedAt { get; private set; }

    public bool IsResolved { get; private set; }

    public Device Device { get; }

    public AlertRule? AlertRule { get; }

    public void Resolve()
    {
        if (IsResolved)
        {
            return;
        }

        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
    }
}
