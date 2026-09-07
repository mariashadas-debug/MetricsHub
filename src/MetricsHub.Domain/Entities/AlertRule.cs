using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class AlertRule
{
    private AlertRule()
    {
        Name = null!;
    }

    public AlertRule(
        string name,
        MetricType metricType,
        ComparisonOperator @operator,
        double threshold,
        AlertSeverity severity,
        Device? device = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        DeviceId = device?.Id;
        Name = name;
        MetricType = metricType;
        Operator = @operator;
        Threshold = threshold;
        Severity = severity;
        IsEnabled = true;
        Device = device;
    }

    public Guid Id { get; private set; }

    public Guid? DeviceId { get; private set; }

    public string Name { get; private set; }

    public MetricType MetricType { get; private set; }

    public ComparisonOperator Operator { get; private set; }

    public double Threshold { get; private set; }

    public AlertSeverity Severity { get; private set; }

    public bool IsEnabled { get; private set; }

    public Device? Device { get; private set; }
}
