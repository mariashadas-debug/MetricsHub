using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MetricsHub.IntegrationTests.Api;

public sealed class MetricsHubApiFactory(
    string connectionString,
    string redisConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MySql", connectionString);
        builder.UseSetting("ConnectionStrings:Redis", redisConnectionString);
    }
}
