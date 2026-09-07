using MetricsHub.Domain.Enums;

namespace MetricsHub.Domain.Entities;

public sealed class AlertRule
{
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

    public Guid Id { get; }

    public Guid? DeviceId { get; }

    public string Name { get; }

    public MetricType MetricType { get; }

    public ComparisonOperator Operator { get; }

    public double Threshold { get; }

    public AlertSeverity Severity { get; }

    public bool IsEnabled { get; private set; }

    public Device? Device { get; }
}
