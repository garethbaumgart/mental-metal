using MentalMetal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory that swaps the Postgres connection string for the one
/// exposed by the shared Testcontainers Postgres, and injects minimum valid config
/// for JWT + AI-provider startup checks. A single instance is created per test
/// collection in <see cref="PostgresFixture"/> and shared across every test class.
/// Migrations run once in the fixture's <see cref="PostgresFixture.InitializeAsync"/>;
/// per-test isolation is handled via <see cref="ResetDatabaseAsync"/>.
/// </summary>
public sealed class MentalMetalWebApplicationFactory : WebApplicationFactory<Program>
{
    // Program.cs reads Jwt + ConnectionStrings from builder.Configuration during
    // top-level-statements execution (before WebApplicationFactory's
    // ConfigureAppConfiguration callback runs), so we must have these values present
    // as environment variables at the moment the entry point is invoked. Setting
    // them in a static ctor guarantees that — every test fixture that references
    // this factory triggers the initialiser, which is idempotent.
    internal const string IntegrationJwtSecret = "integration-test-secret-key-minimum-32-chars-long!";
    internal const string IntegrationJwtIssuer = "MentalMetal.IntegrationTests";
    internal const string IntegrationJwtAudience = "MentalMetal.IntegrationTests";

    public MentalMetalWebApplicationFactory(string connectionString)
    {
        // Program.cs top-level statements read configuration eagerly (Jwt settings,
        // connection string) BEFORE WebApplicationFactory.ConfigureAppConfiguration
        // would fire. Environment variables are read by the default builder's env
        // source, so seed them before the entry point runs (entry point runs lazily
        // on first Services / CreateClient access).
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
    }

    static MentalMetalWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", IntegrationJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", IntegrationJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", IntegrationJwtAudience);
        // AiProvider:EncryptionKey is ValidateOnStart-required and must be non-empty.
        Environment.SetEnvironmentVariable(
            "AiProvider__EncryptionKey",
            "aW50ZWdyYXRpb24tdGVzdC1lbmNyeXB0aW9uLWtleS0xMjM0NTY=");
        // Disable Google auth registration — no ClientId => the block in Program.cs is skipped.
        Environment.SetEnvironmentVariable("Authentication__Google__ClientId", "");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MentalMetalDbContext>();
        // Cascade truncate wipes child tables (RefreshTokens) alongside Users.
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"RefreshTokens\", \"Users\" RESTART IDENTITY CASCADE;");
    }
}
