using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MetricsHub.Infrastructure.Data;

public sealed class MetricsHubDbContextFactory : IDesignTimeDbContextFactory<MetricsHubDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__MySql";

    public MetricsHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} before running Entity Framework Core tools.");
        }

        var options = new DbContextOptionsBuilder<MetricsHubDbContext>()
            .UseMySQL(connectionString)
            .Options;

        return new MetricsHubDbContext(options);
    }
}
