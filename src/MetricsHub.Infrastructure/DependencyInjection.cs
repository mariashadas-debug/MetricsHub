using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Infrastructure.Data;
using MetricsHub.Infrastructure.Persistence;
using MetricsHub.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddOptions<RedisOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection(RedisOptions.SectionName);
                options.KeyPrefix = section[nameof(RedisOptions.KeyPrefix)] ?? options.KeyPrefix;

                if (int.TryParse(section[nameof(RedisOptions.StateTtlHours)], out var stateTtlHours))
                {
                    options.StateTtlHours = stateTtlHours;
                }
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.KeyPrefix), "Redis:KeyPrefix is required.")
            .Validate(options => options.StateTtlHours > 0, "Redis:StateTtlHours must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ConnectionStrings:Redis' is not configured.");
            }

            var redisConfiguration = ConfigurationOptions.Parse(connectionString);
            redisConfiguration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisConfiguration);
        });

        services.AddSingleton<IDeviceStateStore, RedisDeviceStateStore>();

        return services;
    }
}
