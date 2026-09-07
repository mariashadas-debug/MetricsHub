namespace MetricsHub.DeviceSimulator.Configuration;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulation";

    public int IntervalSeconds { get; init; } = 5;

    public IReadOnlyList<SimulatedDeviceOptions> Devices { get; init; } = [];
}
