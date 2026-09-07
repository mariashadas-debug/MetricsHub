using MetricsHub.DeviceSimulator.Configuration;

namespace MetricsHub.DeviceSimulator.Simulation;

public sealed class MetricGeneratorFactory : IMetricGeneratorFactory
{
    public IMetricGenerator Create(SimulationProfile profile) =>
        new MetricGenerator(profile, new SystemRandomSource());
}
