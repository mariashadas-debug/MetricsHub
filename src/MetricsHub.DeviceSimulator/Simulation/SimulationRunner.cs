using MetricsHub.DeviceSimulator.Client;
using MetricsHub.DeviceSimulator.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MetricsHub.DeviceSimulator.Simulation;

public sealed class SimulationRunner(
    IOptions<SimulatorOptions> options,
    IMetricsHubApiClient apiClient,
    IMetricGeneratorFactory generatorFactory,
    ILogger<SimulationRunner> logger)
{
    private readonly SimulatorOptions _options = options.Value;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Simulator starting with {DeviceCount} devices at {IntervalSeconds}-second intervals",
            _options.Devices.Count,
            _options.IntervalSeconds);

        var tasks = _options.Devices.Select(device => RunDeviceAsync(device, cancellationToken));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during graceful shutdown.
        }

        logger.LogInformation("Simulator stopped");
    }

    private async Task RunDeviceAsync(
        SimulatedDeviceOptions device,
        CancellationToken cancellationToken)
    {
        var generator = generatorFactory.Create(device.Profile);
        var interval = TimeSpan.FromSeconds(_options.IntervalSeconds);
        var elapsed = TimeSpan.Zero;
        var registered = false;
        using var timer = new PeriodicTimer(interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!registered)
                {
                    var registration = await apiClient.EnsureDeviceRegisteredAsync(
                        device,
                        cancellationToken);
                    registered = registration.IsSuccess;

                    if (registered)
                    {
                        logger.LogInformation(
                            "Device ready: DeviceKey={DeviceKey}",
                            device.DeviceKey);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Device registration failed: DeviceKey={DeviceKey} StatusCode={StatusCode}",
                            device.DeviceKey,
                            registration.StatusCode);
                    }
                }

                if (registered)
                {
                    var snapshot = generator.Generate(elapsed, DateTime.UtcNow);
                    var result = await apiClient.SendTelemetryAsync(
                        device.DeviceKey,
                        snapshot,
                        cancellationToken);

                    if (result.IsSuccess)
                    {
                        logger.LogDebug("Telemetry sent: DeviceKey={DeviceKey}", device.DeviceKey);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Telemetry send failed: DeviceKey={DeviceKey} StatusCode={StatusCode}",
                            device.DeviceKey,
                            result.StatusCode);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Simulation cycle failed: DeviceKey={DeviceKey}",
                    device.DeviceKey);
            }

            if (!await timer.WaitForNextTickAsync(cancellationToken))
            {
                break;
            }

            elapsed = interval;
        }
    }
}
