using MetricsHub.Domain.Enums;

namespace MetricsHub.Application.Devices;

public sealed record UpdateDeviceCommand(
    string Name,
    DeviceType Type,
    string? Hostname,
    string? OperatingSystem,
    string? Location,
    bool IsEnabled);
