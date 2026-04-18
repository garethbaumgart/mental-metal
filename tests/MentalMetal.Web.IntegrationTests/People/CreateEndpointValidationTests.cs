using System.Net;
using System.Text.Json;
using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.People;

/// <summary>
/// Regression coverage for issue #113: create endpoints that previously silently
/// accepted missing/invalid required fields now return a clean 400 ProblemDetails
/// response with a machine-readable <c>code</c> (and, where relevant, <c>field</c>).
/// </summary>
public sealed class CreateEndpointValidationTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private async Task<(Guid UserId, HttpClient Client)> AuthedClientAsync()
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        return (userId, WithBearer(CreateClient(), token));
    }

    private async Task<Guid> SeedPersonAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPersonRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var person = Person.Create(userId, $"Person-{Guid.NewGuid():N}", PersonType.DirectReport);
        await repo.AddAsync(person, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        return person.Id;
    }

    private static async Task<(string? code, string? field, string rawBody)> ReadProblemAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        string? code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
        string? field = root.TryGetProperty("field", out var f) ? f.GetString() : null;
        return (code, field, raw);
    }

    [Fact]
    public async Task CreatePerson_WithEmptyName_Returns400AndDoesNotLeakEfException()
    {
        var (_, client) = await AuthedClientAsync();

        var response = await client.PostAsync("/api/people", JsonBody(new
        {
            name = "",
            type = "DirectReport",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (code, field, raw) = await ReadProblemAsync(response);
        Assert.Equal("person.validation", code);
        Assert.Equal("name", field);
        // Sanity: the EF Core / Npgsql internals must not leak through.
        Assert.DoesNotContain("LINQ", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ArgumentException", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePerson_WithMissingName_Returns400AndDoesNotLeakEfException()
    {
        var (_, client) = await AuthedClientAsync();

        var response = await client.PostAsync("/api/people", JsonBody(new
        {
            type = "DirectReport",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (code, field, raw) = await ReadProblemAsync(response);
        Assert.Equal("person.validation", code);
        Assert.Equal("name", field);
        Assert.DoesNotContain("LINQ", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", raw, StringComparison.OrdinalIgnoreCase);
    }
}
