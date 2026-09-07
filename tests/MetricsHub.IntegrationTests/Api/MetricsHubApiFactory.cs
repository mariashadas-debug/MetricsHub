using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MetricsHub.IntegrationTests.Api;

public sealed class MetricsHubApiFactory(
    string connectionString,
    string redisConnectionString,
    int offlineAfterSeconds = 3600,
    int offlineCheckIntervalSeconds = 3600,
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MySql", connectionString);
        builder.UseSetting("ConnectionStrings:Redis", redisConnectionString);
        builder.UseSetting("Monitoring:OfflineAfterSeconds", offlineAfterSeconds.ToString());
        builder.UseSetting("Monitoring:OfflineCheckIntervalSeconds", offlineCheckIntervalSeconds.ToString());
        if (configureServices is not null) builder.ConfigureTestServices(configureServices);
    }
}
