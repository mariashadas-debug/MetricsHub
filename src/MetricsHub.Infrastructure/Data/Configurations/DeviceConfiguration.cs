using MetricsHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsHub.Infrastructure.Data.Configurations;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(device => device.Id);

        builder.Property(device => device.DeviceKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(device => device.DeviceKey)
            .IsUnique();

        builder.Property(device => device.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(device => device.Hostname)
            .HasMaxLength(255);

        builder.Property(device => device.OperatingSystem)
            .HasMaxLength(200);

        builder.Property(device => device.Location)
            .HasMaxLength(200);

        builder.Property(device => device.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(device => device.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(device => device.IsEnabled)
            .IsRequired();

        builder.Property(device => device.CreatedAt)
            .HasConversion(UtcDateTimeConverters.Required)
            .IsRequired();

        builder.Property(device => device.LastSeenAt)
            .HasConversion(UtcDateTimeConverters.Optional);

        builder.HasMany(device => device.TelemetryPoints)
            .WithOne(telemetryPoint => telemetryPoint.Device)
            .HasForeignKey(telemetryPoint => telemetryPoint.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(device => device.Alerts)
            .WithOne(alert => alert.Device)
            .HasForeignKey(alert => alert.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(device => device.AlertRules)
            .WithOne(alertRule => alertRule.Device)
            .HasForeignKey(alertRule => alertRule.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
