using System.Net;
using System.Net.Http.Json;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Auth;

/// <summary>
/// End-to-end-through-HTTP coverage for the three email/password endpoints introduced by
/// the <c>email-password-auth</c> change: register, login, and set-password.
/// </summary>
public sealed class AuthEndpointsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record AuthEnvelope(string AccessToken, UserEnvelope User);

    private sealed record UserEnvelope(
        Guid Id,
        string Email,
        string Name,
        bool HasPassword);

    // ---------- Register -----------------------------------------------------

    [Fact]
    public async Task Register_WithValidBody_Returns200_PersistsUser_AndSetsRefreshCookie()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "new-user@example.com",
            password = "correct-horse-battery",
            name = "New User"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync<AuthEnvelope>(response);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.Equal("new-user@example.com", body.User.Email);
        Assert.True(body.User.HasPassword);

        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];
        Assert.Contains(cookies, c => c.StartsWith("refresh_token=", StringComparison.Ordinal));

        var persisted = await FindUserByEmailAsync("new-user@example.com");
        Assert.NotNull(persisted);
        Assert.NotNull(persisted!.PasswordHash);
        Assert.Null(persisted.ExternalAuthId);
        Assert.True(await CountRefreshTokensAsync(persisted.Id) >= 1);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        const string email = "dup@example.com";
        await SeedUserWithPasswordAndSignInAsync(email, "initial-password");

        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "another-password-123",
            name = "Second User"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---------- Login --------------------------------------------------------

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200_AndAccessToken()
    {
        const string email = "login-ok@example.com";
        const string password = "valid-password-01";
        await SeedUserWithPasswordAndSignInAsync(email, password);

        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync<AuthEnvelope>(response);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.Equal(email, body.User.Email);
        Assert.True(body.User.HasPassword);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        const string email = "wrong-pass@example.com";
        await SeedUserWithPasswordAndSignInAsync(email, "the-correct-one");

        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "nope-this-is-wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "ghost@example.com",
            password = "anything-at-all"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ForGoogleOnlyUser_Returns401()
    {
        const string email = "google-only@example.com";
        await SeedGoogleOnlyUserAsync("google-sub-123", email);

        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "any-password-works"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Set password -------------------------------------------------

    [Fact]
    public async Task SetPassword_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/password", new
        {
            newPassword = "brand-new-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetPassword_ForGoogleOnlyUser_SetsPassword_AndEnablesPasswordLogin()
    {
        const string email = "link-password@example.com";
        var userId = await SeedGoogleOnlyUserAsync("google-sub-link", email);

        // Mint an access token for the Google user via the in-DI TokenService.
        string accessToken;
        using (var scope = Factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var repo = sp.GetRequiredService<MentalMetal.Domain.Users.IUserRepository>();
            var tokenService = sp.GetRequiredService<MentalMetal.Application.Common.Auth.ITokenService>();
            var uow = sp.GetRequiredService<MentalMetal.Application.Common.IUnitOfWork>();
            var user = await repo.GetByIdAsync(userId, CancellationToken.None);
            Assert.NotNull(user);
            var tokens = tokenService.GenerateTokens(user!);
            await uow.SaveChangesAsync(CancellationToken.None);
            accessToken = tokens.AccessToken;
        }

        var authedClient = WithBearer(CreateClient(), accessToken);
        const string newPassword = "freshly-chosen-pw";

        var setResponse = await authedClient.PostAsJsonAsync("/api/auth/password", new { newPassword });
        Assert.Equal(HttpStatusCode.NoContent, setResponse.StatusCode);

        // Confirm login now works with the password the user just set.
        var anonClient = CreateClient();
        var loginResponse = await anonClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task SetPassword_WithShortPassword_Returns400()
    {
        var (_, accessToken) = await SeedUserWithPasswordAndSignInAsync(
            "short-pw@example.com", "initial-long-password");

        var authedClient = WithBearer(CreateClient(), accessToken);

        var response = await authedClient.PostAsJsonAsync("/api/auth/password", new
        {
            newPassword = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
