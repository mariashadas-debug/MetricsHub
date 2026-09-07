using MetricsHub.DeviceSimulator.Configuration;
using MetricsHub.DeviceSimulator.Simulation;

namespace MetricsHub.UnitTests.DeviceSimulator;

public sealed class MetricGeneratorTests
{
    [Theory]
    [InlineData("CpuUsage")]
    [InlineData("MemoryUsage")]
    [InlineData("DiskUsage")]
    public void Generate_PercentageMetricRemainsBetweenZeroAndOneHundred(string metricType)
    {
        var generator = CreateGenerator(new AlternatingRandomSource());

        for (var index = 0; index < 100; index++)
        {
            var snapshot = generator.Generate(TimeSpan.FromSeconds(5), DateTime.UtcNow);
            var metric = snapshot.Metrics.Single(candidate => candidate.Type == metricType);
            Assert.InRange(metric.Value, 0, 100);
        }
    }

    [Fact]
    public void Generate_ConsecutiveNormalCpuSamplesChangeGradually()
    {
        var generator = CreateGenerator(new FixedRandomSource(0.75));
        var first = GetMetric(generator.Generate(TimeSpan.Zero, DateTime.UtcNow), "CpuUsage");
        var second = GetMetric(generator.Generate(TimeSpan.FromSeconds(5), DateTime.UtcNow), "CpuUsage");

        Assert.InRange(Math.Abs(second - first), 0, 4.5);
    }

    [Fact]
    public void Generate_DiskChangesSlowly()
    {
        var generator = CreateGenerator(new FixedRandomSource(0.75));
        var first = GetMetric(generator.Generate(TimeSpan.Zero, DateTime.UtcNow), "DiskUsage");
        var second = GetMetric(generator.Generate(TimeSpan.FromSeconds(5), DateTime.UtcNow), "DiskUsage");

        Assert.InRange(Math.Abs(second - first), 0, 0.25);
    }

    [Fact]
    public void Generate_UptimeIncreasesByElapsedSeconds()
    {
        var generator = CreateGenerator(new FixedRandomSource(0.5));
        var first = GetMetric(generator.Generate(TimeSpan.Zero, DateTime.UtcNow), "Uptime");
        var second = GetMetric(generator.Generate(TimeSpan.FromSeconds(5), DateTime.UtcNow), "Uptime");

        Assert.Equal(5, second - first, precision: 2);
    }

    [Fact]
    public void SeparateGenerators_DoNotShareMetricState()
    {
        var firstGenerator = CreateGenerator(new FixedRandomSource(0.75));
        var secondGenerator = CreateGenerator(new FixedRandomSource(0.75));
        var referenceGenerator = CreateGenerator(new FixedRandomSource(0.75));

        firstGenerator.Generate(TimeSpan.Zero, DateTime.UtcNow);
        firstGenerator.Generate(TimeSpan.FromSeconds(5), DateTime.UtcNow);

        var secondFirstCpu = GetMetric(
            secondGenerator.Generate(TimeSpan.Zero, DateTime.UtcNow),
            "CpuUsage");
        var referenceFirstCpu = GetMetric(
            referenceGenerator.Generate(TimeSpan.Zero, DateTime.UtcNow),
            "CpuUsage");

        Assert.Equal(referenceFirstCpu, secondFirstCpu);
    }

    [Fact]
    public void Generate_IncludesEveryRequiredMetricType()
    {
        var snapshot = CreateGenerator(new FixedRandomSource(0.5))
            .Generate(TimeSpan.Zero, DateTime.UtcNow);

        var types = snapshot.Metrics.Select(metric => metric.Type).ToHashSet();

        var expected = new HashSet<string>
        {
            "CpuUsage", "MemoryUsage", "DiskUsage", "NetworkIn", "NetworkOut", "Uptime"
        };

        Assert.True(expected.SetEquals(types));
    }

    [Fact]
    public void Generate_PreservesUtcTimestamp()
    {
        var timestamp = DateTime.UtcNow;

        var snapshot = CreateGenerator(new FixedRandomSource(0.5))
            .Generate(TimeSpan.Zero, timestamp);

        Assert.Equal(timestamp, snapshot.Timestamp);
        Assert.Equal(DateTimeKind.Utc, snapshot.Timestamp.Kind);
    }

    private static MetricGenerator CreateGenerator(IRandomSource random) =>
        new(SimulationProfile.DevelopmentWorkstation, random, spikeProbability: 0);

    private static double GetMetric(TelemetrySnapshot snapshot, string type) =>
        snapshot.Metrics.Single(metric => metric.Type == type).Value;

    private sealed class FixedRandomSource(double value) : IRandomSource
    {
        public double NextDouble() => value;

        public double NextDouble(double minimum, double maximum) =>
            minimum + (value * (maximum - minimum));
    }

    private sealed class AlternatingRandomSource : IRandomSource
    {
        private bool _high;

        public double NextDouble()
        {
            _high = !_high;
            return _high ? 1 : 0;
        }

        public double NextDouble(double minimum, double maximum) =>
            minimum + (NextDouble() * (maximum - minimum));
    }
}
