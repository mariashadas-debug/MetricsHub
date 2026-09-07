using MetricsHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsHub.Infrastructure.Data.Configurations;

internal sealed class TelemetryPointConfiguration : IEntityTypeConfiguration<TelemetryPoint>
{
    public void Configure(EntityTypeBuilder<TelemetryPoint> builder)
    {
        builder.ToTable("TelemetryPoints");

        builder.HasKey(telemetryPoint => telemetryPoint.Id);

        builder.Property(telemetryPoint => telemetryPoint.DeviceId)
            .IsRequired();

        builder.Property(telemetryPoint => telemetryPoint.MetricType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(telemetryPoint => telemetryPoint.Value)
            .IsRequired();

        builder.Property(telemetryPoint => telemetryPoint.Unit)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(telemetryPoint => telemetryPoint.Timestamp)
            .HasConversion(UtcDateTimeConverters.Required)
            .IsRequired();

        builder.HasIndex(telemetryPoint => telemetryPoint.MetricType);
        builder.HasIndex(telemetryPoint => telemetryPoint.Timestamp);
        builder.HasIndex(telemetryPoint => new { telemetryPoint.DeviceId, telemetryPoint.Timestamp });
    }
}
