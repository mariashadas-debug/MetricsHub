using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Devices;

public sealed record CreateDeviceCommand(
    string DeviceKey,
    string Name,
    DeviceType Type,
    string? Hostname,
    string? OperatingSystem,
    string? Location);
