using System.ComponentModel.DataAnnotations;

namespace MetricsHub.Api.Contracts.Telemetry;

public sealed class IngestTelemetryRequest
{
    [Required, MaxLength(128)]
    public string DeviceKey { get; init; } = string.Empty;

    [Required]
    public DateTime? Timestamp { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyCollection<TelemetryMetricRequest> Metrics { get; init; } = [];
}
