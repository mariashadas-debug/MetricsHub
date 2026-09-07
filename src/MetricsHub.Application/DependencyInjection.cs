using MetricsHub.Application.Devices;
using MetricsHub.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace MetricsHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        return services;
    }
}
