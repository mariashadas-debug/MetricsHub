namespace MetricsHub.Infrastructure.Redis;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string KeyPrefix { get; set; } = "metricshub";

    public int StateTtlHours { get; set; } = 24;
}
