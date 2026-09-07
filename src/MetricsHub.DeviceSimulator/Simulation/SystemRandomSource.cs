namespace MetricsHub.DeviceSimulator.Simulation;

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random = new();

    public double NextDouble() => _random.NextDouble();

    public double NextDouble(double minimum, double maximum) =>
        minimum + (_random.NextDouble() * (maximum - minimum));
}
