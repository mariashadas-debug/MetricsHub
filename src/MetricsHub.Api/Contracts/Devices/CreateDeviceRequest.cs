using System.ComponentModel.DataAnnotations;
using MetricsHub.Domain.Enums;

namespace MetricsHub.Api.Contracts.Devices;

public sealed class CreateDeviceRequest
{
    [Required, MaxLength(128)]
    public string DeviceKey { get; init; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public DeviceType? Type { get; init; }

    [MaxLength(255)]
    public string? Hostname { get; init; }

    [MaxLength(200)]
    public string? OperatingSystem { get; init; }

    [MaxLength(200)]
    public string? Location { get; init; }
}
