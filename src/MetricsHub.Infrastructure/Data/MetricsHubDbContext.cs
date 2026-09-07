using MetricsHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetricsHub.Infrastructure.Data;

public sealed class MetricsHubDbContext(DbContextOptions<MetricsHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();

    public DbSet<TelemetryPoint> TelemetryPoints => Set<TelemetryPoint>();

    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MetricsHubDbContext).Assembly);
    }
}
