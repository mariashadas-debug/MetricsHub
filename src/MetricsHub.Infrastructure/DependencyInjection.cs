using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Infrastructure.Data;
using MetricsHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MetricsHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MetricsHubDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("MySql");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ConnectionStrings:MySql' is not configured.");
            }

            options.UseMySQL(connectionString);
        });

        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();

        return services;
    }
}
