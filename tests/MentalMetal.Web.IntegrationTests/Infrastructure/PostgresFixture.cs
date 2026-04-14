using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MentalMetal.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Boots a disposable Postgres container once per xUnit collection and creates a
/// single <see cref="MentalMetalWebApplicationFactory"/> shared across every test
/// class in the collection. Migrations therefore run exactly once per test run,
/// not per test. Per-test isolation is provided by
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

    public MentalMetalWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        Factory = new MentalMetalWebApplicationFactory(ConnectionString);

        // Run migrations once, up-front, against the shared container.
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MentalMetalDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
        await Container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
