using MetricsHub.DeviceSimulator.Configuration;

namespace MetricsHub.DeviceSimulator.Simulation;

public interface IMetricGeneratorFactory
{
    IMetricGenerator Create(SimulationProfile profile);
}
