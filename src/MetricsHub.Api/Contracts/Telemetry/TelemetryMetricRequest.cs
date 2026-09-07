using System.ComponentModel.DataAnnotations;
using MetricsHub.Domain.Enums;

namespace MetricsHub.Api.Contracts.Telemetry;

public sealed class TelemetryMetricRequest
{
    [Required]
    public MetricType? Type { get; init; }

    public double Value { get; init; }

    [Required, MaxLength(32)]
    public string Unit { get; init; } = string.Empty;
}
