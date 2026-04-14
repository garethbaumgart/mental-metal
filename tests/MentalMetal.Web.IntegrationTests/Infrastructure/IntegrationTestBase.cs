using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Auth;
using MentalMetal.Domain.Users;
// IUserRepository lives in MentalMetal.Domain.Users (domain-owned abstraction).
using MentalMetal.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Per-test lifecycle base: ensures schema + wipes users/refresh-tokens before each test.
/// Requires the xUnit collection fixture <see cref="PostgresFixture"/> to have booted Postgres.
/// </summary>
[Collection(PostgresCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly MentalMetalWebApplicationFactory Factory;
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected IntegrationTestBase(PostgresFixture postgres)
    {
        Factory = new MentalMetalWebApplicationFactory(postgres.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await Factory.EnsureSchemaAsync();
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    protected HttpClient CreateClient() => Factory.CreateClient();

    /// <summary>
    /// Seeds a user directly through the domain + repository (bypasses the HTTP register endpoint)
    /// and returns an access token for them. Use when a test needs an authenticated principal but
    /// is not exercising registration itself.
    /// </summary>
    protected async Task<(Guid UserId, string AccessToken)> SeedUserWithPasswordAndSignInAsync(
        string email, string password, string name = "Test User")
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var hasher = sp.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<User>>();
        var repo = sp.GetRequiredService<IUserRepository>();
        var tokenService = sp.GetRequiredService<ITokenService>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var user = User.RegisterWithPassword(email, name, Password.Create(password, hasher), null);
        await repo.AddAsync(user, CancellationToken.None);
        var tokens = tokenService.GenerateTokens(user);
        await uow.SaveChangesAsync(CancellationToken.None);

        return (user.Id, tokens.AccessToken);
    }

    /// <summary>
    /// Seeds a Google-only user (no password) and returns their id.
    /// </summary>
    protected async Task<Guid> SeedGoogleOnlyUserAsync(string externalAuthId, string email, string name = "Google User")
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var repo = sp.GetRequiredService<IUserRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var user = User.Register(externalAuthId, email, name, avatarUrl: null);
        await repo.AddAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        return user.Id;
    }

    protected async Task<User?> FindUserByEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MentalMetalDbContext>();
        var normalised = email.Trim().ToLower();
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            db.Users,
            u => u.Email.Value == normalised);
    }

    protected async Task<int> CountRefreshTokensAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MentalMetalDbContext>();
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
            db.RefreshTokens,
            t => t.UserId == userId);
    }

    protected static HttpClient WithBearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    protected static StringContent JsonBody(object value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), System.Text.Encoding.UTF8, "application/json");

    protected static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions);
}
