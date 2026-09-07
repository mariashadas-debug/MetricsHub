using MetricsHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsHub.Infrastructure.Data.Configurations;

internal sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");

        builder.HasKey(alertRule => alertRule.Id);

        builder.Property(alertRule => alertRule.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(alertRule => alertRule.MetricType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(alertRule => alertRule.Operator)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(alertRule => alertRule.Threshold)
            .IsRequired();

        builder.Property(alertRule => alertRule.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(alertRule => alertRule.IsEnabled)
            .IsRequired();

        builder.HasIndex(alertRule => alertRule.DeviceId);
    }
}
