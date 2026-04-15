using System.Net;
using System.Net.Http.Json;
using MentalMetal.Web.IntegrationTests.Infrastructure;

namespace MentalMetal.Web.IntegrationTests.People;

public sealed class PeopleEndpointsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record PersonBody(
        Guid Id,
        Guid UserId,
        string Name,
        string Type,
        bool IsArchived);

    private async Task<HttpClient> AuthedClientAsync()
    {
        var (_, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        return WithBearer(CreateClient(), token);
    }

    // Regression: the people list endpoint previously required `includeArchived` in the
    // query string (non-nullable `bool`). Front-end calls without the param — like
    // `PeopleListComponent.loadPeople()` — returned 400 and left the list empty. The
    // endpoint now treats the param as optional and defaults it to `false`. See issue #88.
    [Fact]
    public async Task GetPeople_WithNoQueryParameters_Returns200()
    {
        var client = await AuthedClientAsync();

        // Seed a person so the happy path also asserts payload shape.
        var create = await client.PostAsJsonAsync("/api/people", new
        {
            name = "Issue 88 Person",
            type = "Stakeholder",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var resp = await client.GetAsync("/api/people");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await ReadJsonAsync<List<PersonBody>>(resp);
        Assert.NotNull(body);
        Assert.Single(body!);
        Assert.Equal("Issue 88 Person", body![0].Name);
        Assert.False(body![0].IsArchived);
    }

    [Fact]
    public async Task GetPeople_WithIncludeArchivedFalse_OmitsArchivedPeople()
    {
        var client = await AuthedClientAsync();

        var create = await client.PostAsJsonAsync("/api/people", new
        {
            name = "Alice",
            type = "DirectReport",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await ReadJsonAsync<PersonBody>(create);
        Assert.NotNull(created);

        var archive = await client.PostAsync($"/api/people/{created!.Id}/archive", content: null);
        Assert.True(archive.IsSuccessStatusCode, $"Archive failed: {archive.StatusCode}");

        // Default request — the fix makes includeArchived optional (false by default).
        var resp = await client.GetAsync("/api/people");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await ReadJsonAsync<List<PersonBody>>(resp);
        Assert.NotNull(body);
        Assert.Empty(body!);

        // Explicit includeArchived=true returns the archived row.
        var respIncludeArchived = await client.GetAsync("/api/people?includeArchived=true");
        Assert.Equal(HttpStatusCode.OK, respIncludeArchived.StatusCode);
        var bodyIncludeArchived = await ReadJsonAsync<List<PersonBody>>(respIncludeArchived);
        Assert.NotNull(bodyIncludeArchived);
        Assert.Single(bodyIncludeArchived!);
        Assert.True(bodyIncludeArchived![0].IsArchived);
    }
}
