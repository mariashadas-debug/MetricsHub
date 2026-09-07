using MetricsHub.DeviceSimulator.Configuration;

namespace MetricsHub.DeviceSimulator.Simulation;

public sealed class MetricGenerator : IMetricGenerator
{
    private const double NetworkMaximumKilobytesPerSecond = 25_000;
    private readonly IRandomSource _random;
    private readonly double _spikeProbability;
    private readonly ProfileBounds _profile;
    private double _cpu;
    private double _memory;
    private double _disk;
    private double _networkIn;
    private double _networkOut;
    private double _uptimeSeconds;

    public MetricGenerator(
        SimulationProfile profile,
        IRandomSource random,
        double spikeProbability = 0.01)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(spikeProbability);

        _random = random;
        _spikeProbability = spikeProbability;
        _profile = ProfileBounds.For(profile);
        _cpu = Initial(_profile.CpuMinimum, _profile.CpuMaximum);
        _memory = Initial(_profile.MemoryMinimum, _profile.MemoryMaximum);
        _disk = Initial(_profile.DiskMinimum, _profile.DiskMaximum);
        _networkIn = Initial(_profile.NetworkMinimum, _profile.NetworkMaximum);
        _networkOut = Initial(_profile.NetworkMinimum * 0.5, _profile.NetworkMaximum * 0.7);
        _uptimeSeconds = _random.NextDouble(3_600, 604_800);
    }

    public TelemetrySnapshot Generate(TimeSpan elapsed, DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must use UTC.", nameof(timestamp));
        }

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        _cpu = Walk(_cpu, _profile.CpuMidpoint, 4.0, 0, 100);
        _memory = Walk(_memory, _profile.MemoryMidpoint, 1.2, 0, 100);
        _disk = Walk(_disk, _profile.DiskMidpoint, 0.04, 0, 100, 0.01);
        _networkIn = Walk(_networkIn, _profile.NetworkMidpoint, 300, 0, NetworkMaximumKilobytesPerSecond);
        _networkOut = Walk(_networkOut, _profile.NetworkMidpoint * 0.6, 180, 0, NetworkMaximumKilobytesPerSecond);

        if (_random.NextDouble() < _spikeProbability)
        {
            _cpu = Clamp(_cpu + _random.NextDouble(25, 50), 0, 100);
        }

        if (_random.NextDouble() < _spikeProbability * 0.5)
        {
            _memory = Clamp(_memory + _random.NextDouble(8, 18), 0, 100);
        }

        if (_random.NextDouble() < _spikeProbability)
        {
            _networkIn = Clamp(_networkIn * _random.NextDouble(2, 5), 0, NetworkMaximumKilobytesPerSecond);
            _networkOut = Clamp(_networkOut * _random.NextDouble(1.5, 3), 0, NetworkMaximumKilobytesPerSecond);
        }

        _uptimeSeconds += elapsed.TotalSeconds;

        return new TelemetrySnapshot(timestamp,
        [
            Sample("CpuUsage", _cpu, "%"),
            Sample("MemoryUsage", _memory, "%"),
            Sample("DiskUsage", _disk, "%"),
            Sample("NetworkIn", _networkIn, "KB/s"),
            Sample("NetworkOut", _networkOut, "KB/s"),
            Sample("Uptime", _uptimeSeconds, "seconds")
        ]);
    }

    private double Initial(double minimum, double maximum) =>
        _random.NextDouble(minimum, maximum);

    private double Walk(
        double current,
        double target,
        double maximumStep,
        double minimum,
        double maximum,
        double driftStrength = 0.08)
    {
        var drift = (target - current) * driftStrength;
        var noise = _random.NextDouble(-maximumStep, maximumStep);
        return Clamp(current + drift + noise, minimum, maximum);
    }

    private static MetricSample Sample(string type, double value, string unit) =>
        new(type, Math.Round(value, 2), unit);

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Clamp(value, minimum, maximum);

    private sealed record ProfileBounds(
        double CpuMinimum,
        double CpuMaximum,
        double MemoryMinimum,
        double MemoryMaximum,
        double DiskMinimum,
        double DiskMaximum,
        double NetworkMinimum,
        double NetworkMaximum)
    {
        public double CpuMidpoint => (CpuMinimum + CpuMaximum) / 2;
        public double MemoryMidpoint => (MemoryMinimum + MemoryMaximum) / 2;
        public double DiskMidpoint => (DiskMinimum + DiskMaximum) / 2;
        public double NetworkMidpoint => (NetworkMinimum + NetworkMaximum) / 2;

        public static ProfileBounds For(SimulationProfile profile) => profile switch
        {
            SimulationProfile.DevelopmentWorkstation => new(20, 45, 55, 75, 45, 70, 300, 2_500),
            SimulationProfile.Server => new(30, 60, 60, 85, 55, 80, 500, 5_000),
            SimulationProfile.IdleVirtualMachine => new(5, 20, 30, 50, 25, 55, 50, 700),
            SimulationProfile.ContainerHost => new(25, 55, 50, 80, 40, 75, 800, 8_000),
            _ => new(15, 45, 40, 70, 35, 70, 200, 2_000)
        };
    }
}
