using MetricsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using Testcontainers.Redis;
using StackExchange.Redis;

namespace MetricsHub.IntegrationTests.Persistence;

public sealed class MySqlDatabaseFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("metricshub_tests")
        .WithUsername("metricshub_tests")
        .WithPassword("integration-test-only-password")
        .Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:8.10.1").Build();
    private IConnectionMultiplexer? _redisConnection;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_container.StartAsync(), _redisContainer.StartAsync());

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_redisConnection is not null)
        {
            await _redisConnection.DisposeAsync();
        }

        await _redisContainer.DisposeAsync();
        await _container.DisposeAsync();
    }

    public string ConnectionString => _container.GetConnectionString();

    public string RedisConnectionString => _redisContainer.GetConnectionString();

    public IConnectionMultiplexer RedisConnection =>
        _redisConnection ??= ConnectionMultiplexer.Connect(new ConfigurationOptions
        {
            EndPoints = { RedisConnectionString },
            AllowAdmin = true
        });

    public async Task FlushRedisAsync()
    {
        await RedisConnection.GetDatabase().ExecuteAsync("FLUSHDB");
    }

    public MetricsHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MetricsHubDbContext>()
            .UseMySQL(_container.GetConnectionString())
            .Options;

        return new MetricsHubDbContext(options);
    }
}
