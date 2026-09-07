namespace MetricsHub.DeviceSimulator.Configuration;

public sealed class SimulatedDeviceOptions
{
    public string DeviceKey { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Hostname { get; init; } = string.Empty;

    public string OperatingSystem { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public SimulationProfile Profile { get; init; } = SimulationProfile.Generic;
}
