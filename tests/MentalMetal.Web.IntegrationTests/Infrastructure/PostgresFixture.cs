using Testcontainers.PostgreSql;

namespace MentalMetal.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Boots a disposable Postgres container once per xUnit collection. The container is
/// shared by every test class in the collection; per-test isolation is provided by
/// <see cref="IntegrationTestBase.InitializeAsync"/> which truncates user/token tables.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("mentalmetal_integration")
        .WithUsername("mentalmetal")
        .WithPassword("integration-test")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
