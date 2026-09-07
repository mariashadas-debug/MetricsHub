using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MetricsHub.Infrastructure.Data.Configurations;

internal static class UtcDateTimeConverters
{
    public static readonly ValueConverter<DateTime, DateTime> Required = new(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static readonly ValueConverter<DateTime?, DateTime?> Optional = new(
        value => value,
        value => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null);
}
