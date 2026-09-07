using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Devices;

public sealed record DeviceResponse(
    Guid Id,
    string DeviceKey,
    string Name,
    DeviceType Type,
    DeviceStatus Status,
    string? Hostname,
    string? OperatingSystem,
    string? Location,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? LastSeenAt);
