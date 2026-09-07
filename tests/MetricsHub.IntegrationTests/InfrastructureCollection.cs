using MetricsHub.IntegrationTests.Persistence;

namespace MetricsHub.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<MySqlDatabaseFixture>
{
    public const string Name = "MySQL and Redis infrastructure";
}
