using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class Alert
{
    private Alert()
    {
        Message = null!;
        Device = null!;
    }

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

    public Guid Id { get; private set; }

    public Guid DeviceId { get; private set; }

    public Guid? AlertRuleId { get; private set; }

    public AlertSeverity Severity { get; private set; }

    public string Message { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public bool IsResolved { get; private set; }

    public Device Device { get; private set; }

    public AlertRule? AlertRule { get; private set; }

    public void Resolve()
        => Resolve(DateTime.UtcNow);

    public void Resolve(DateTime resolvedAt)
    {
        if (IsResolved)
        {
            return;
        }

        if (resolvedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Resolution timestamp must use UTC.", nameof(resolvedAt));
        }

        IsResolved = true;
        ResolvedAt = resolvedAt;
    }
}
