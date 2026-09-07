using MetricsHub.Application.Abstractions.Realtime;
using MetricsHub.Application.Alerts;
using MetricsHub.Application.AlertRules;
using MetricsHub.Application.Devices;
using MetricsHub.Application.Monitoring;
using MetricsHub.Application.Realtime;
using MetricsHub.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace MetricsHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IAlertRuleService, AlertRuleService>();
        services.AddScoped<IAlertEvaluationService, AlertEvaluationService>();
        services.AddSingleton<AlertRuleEvaluator>();
        services.AddScoped<IDeviceMonitoringService, DeviceMonitoringService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRealtimeNotifier, NullRealtimeNotifier>();
        return services;
    }
}
