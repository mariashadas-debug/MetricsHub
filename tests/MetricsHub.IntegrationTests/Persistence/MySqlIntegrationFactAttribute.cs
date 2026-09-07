namespace MetricsHub.IntegrationTests.Persistence;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlIntegrationFactAttribute : FactAttribute
{
    public MySqlIntegrationFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("METRICSHUB_RUN_INTEGRATION_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set METRICSHUB_RUN_INTEGRATION_TESTS=true to run Docker-backed MySQL tests.";
        }
    }
}
