namespace MetricsHub.DeviceSimulator.Simulation;

public interface IRandomSource
{
    double NextDouble();

    double NextDouble(double minimum, double maximum);
}
