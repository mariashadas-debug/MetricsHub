using MetricsHub.DeviceSimulator.Client;
using MetricsHub.DeviceSimulator.Configuration;
using MetricsHub.DeviceSimulator.Simulation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
    .AddEnvironmentVariables();

builder.Services.AddOptions<MetricsHubOptions>()
    .Bind(builder.Configuration.GetSection(MetricsHubOptions.SectionName))
    .Validate(options =>
        Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "MetricsHub:ApiBaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();

builder.Services.AddOptions<SimulatorOptions>()
    .Bind(builder.Configuration.GetSection(SimulatorOptions.SectionName))
    .Validate(options => options.IntervalSeconds > 0, "Simulation:IntervalSeconds must be greater than zero.")
    .Validate(options => options.Devices.Count > 0, "At least one simulated device is required.")
    .Validate(options => options.Devices.All(device =>
            !string.IsNullOrWhiteSpace(device.DeviceKey)
            && !string.IsNullOrWhiteSpace(device.Name)
            && !string.IsNullOrWhiteSpace(device.Type)),
        "Every simulated device requires DeviceKey, Name, and Type.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IMetricsHubApiClient, MetricsHubApiClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<MetricsHubOptions>>().Value;
    client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IMetricGeneratorFactory, MetricGeneratorFactory>();
builder.Services.AddSingleton<SimulationRunner>();

using var host = builder.Build();
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

await host.StartAsync(cancellation.Token);

try
{
    await host.Services.GetRequiredService<SimulationRunner>().RunAsync(cancellation.Token);
}
finally
{
    await host.StopAsync(CancellationToken.None);
}
