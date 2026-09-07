namespace MetricsHub.DeviceSimulator.Simulation;

public interface IMetricGenerator
{
    TelemetrySnapshot Generate(TimeSpan elapsed, DateTime timestamp);
}
