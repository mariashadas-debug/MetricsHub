using MetricsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;

namespace MetricsHub.IntegrationTests.Persistence;

public sealed class MySqlDatabaseFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("metricshub_tests")
        .WithUsername("metricshub_tests")
        .WithPassword("integration-test-only-password")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string ConnectionString => _container.GetConnectionString();

    public MetricsHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MetricsHubDbContext>()
            .UseMySQL(_container.GetConnectionString())
            .Options;

        return new MetricsHubDbContext(options);
    }
}
