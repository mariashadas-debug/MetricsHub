using MetricsHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsHub.Infrastructure.Data.Configurations;

internal sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");

        builder.HasKey(alert => alert.Id);

        builder.Property(alert => alert.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(alert => alert.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(alert => alert.CreatedAt)
            .HasConversion(UtcDateTimeConverters.Required)
            .IsRequired();

        builder.Property(alert => alert.ResolvedAt)
            .HasConversion(UtcDateTimeConverters.Optional);

        builder.Property(alert => alert.IsResolved)
            .IsRequired();

        builder.HasIndex(alert => alert.DeviceId);
        builder.HasIndex(alert => alert.CreatedAt);
        builder.HasIndex(alert => alert.IsResolved);

        builder.HasOne(alert => alert.AlertRule)
            .WithMany()
            .HasForeignKey(alert => alert.AlertRuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
