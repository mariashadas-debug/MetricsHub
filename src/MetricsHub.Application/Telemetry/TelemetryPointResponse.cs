using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Telemetry;

public sealed record TelemetryPointResponse(
    long Id,
    Guid DeviceId,
    MetricType Type,
    double Value,
    string Unit,
    DateTime Timestamp);
