namespace MetricsHub.DeviceSimulator.Configuration;

public sealed class MetricsHubOptions
{
    public const string SectionName = "MetricsHub";

    public string ApiBaseUrl { get; init; } = string.Empty;
}
