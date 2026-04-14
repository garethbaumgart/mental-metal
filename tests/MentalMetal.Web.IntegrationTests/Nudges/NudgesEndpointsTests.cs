using System.Net;
using System.Net.Http.Json;
using MentalMetal.Application.Common;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Nudges;

public sealed class NudgesEndpointsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record CadenceBody(string Type, int? CustomIntervalDays, string? DayOfWeek, int? DayOfMonth);
    private sealed record NudgeBody(
        Guid Id,
        Guid UserId,
        string Title,
        CadenceBody Cadence,
        DateOnly StartDate,
        DateOnly? NextDueDate,
        DateTimeOffset? LastNudgedAt,
        Guid? PersonId,
        Guid? InitiativeId,
        string? Notes,
        bool IsActive,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
    private sealed record ProblemBody(string? Title, string? Code);

    private async Task<(Guid UserId, HttpClient Client)> AuthedAsync()
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        return (userId, WithBearer(CreateClient(), token));
    }

    private async Task<Guid> SeedPersonAsync(Guid userId, string name = "Sarah")
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
        var repo = sp.GetRequiredService<IPersonRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var p = Person.Create(userId, name, PersonType.DirectReport);
        await repo.AddAsync(p, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        return p.Id;
    }

    [Fact]
    public async Task CreateDaily_Returns201()
    {
        var (_, client) = await AuthedAsync();
        var resp = await client.PostAsJsonAsync("/api/nudges", new
        {
            title = "Review risk log",
            cadenceType = "Daily",
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<NudgeBody>();
        Assert.NotNull(body);
        Assert.Equal("Daily", body!.Cadence.Type);
        Assert.True(body.IsActive);
        Assert.NotNull(body.NextDueDate);
    }

    [Fact]
    public async Task CreateWeekly_AnchorsToDayOfWeek()
    {
        var (_, client) = await AuthedAsync();
        var resp = await client.PostAsJsonAsync("/api/nudges", new
        {
            title = "Project X risks",
            cadenceType = "Weekly",
            dayOfWeek = "Thursday",
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreateEmptyTitle_Returns400Validation()
    {
        var (_, client) = await AuthedAsync();
        var resp = await client.PostAsJsonAsync("/api/nudges", new
        {
            title = "",
            cadenceType = "Daily",
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.validation", problem!.Code);
    }

    [Fact]
    public async Task CreateWeeklyWithoutDayOfWeek_Returns400InvalidCadence()
    {
        var (_, client) = await AuthedAsync();
        var resp = await client.PostAsJsonAsync("/api/nudges", new
        {
            title = "t",
            cadenceType = "Weekly",
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.invalidCadence", problem!.Code);
    }

    [Fact]
    public async Task CreateCustomZeroInterval_Returns400InvalidCadence()
    {
        var (_, client) = await AuthedAsync();
        var resp = await client.PostAsJsonAsync("/api/nudges", new
        {
            title = "t",
            cadenceType = "Custom",
            customIntervalDays = 0,
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.invalidCadence", problem!.Code);
    }

    [Fact]
    public async Task CreateWithPersonOfAnotherUser_Returns400LinkedEntityNotFound()
    {
        var (userA, _) = await AuthedAsync();
        var personA = await SeedPersonAsync(userA);

        var (_, clientB) = await AuthedAsync();
        var resp = await clientB.PostAsJsonAsync("/api/nudges", new
        {
            title = "t",
            cadenceType = "Daily",
            personId = personA,
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.linkedEntityNotFound", problem!.Code);
    }

    [Fact]
    public async Task Get_NotFound_Returns404WithCode()
    {
        var (_, client) = await AuthedAsync();
        var resp = await client.GetAsync($"/api/nudges/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.notFound", problem!.Code);
    }

    [Fact]
    public async Task Get_WrongOwner_Returns404()
    {
        var (_, clientA) = await AuthedAsync();
        var create = await clientA.PostAsJsonAsync("/api/nudges", new { title = "a", cadenceType = "Daily" });
        var n = await create.Content.ReadFromJsonAsync<NudgeBody>();

        var (_, clientB) = await AuthedAsync();
        var resp = await clientB.GetAsync($"/api/nudges/{n!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByPerson()
    {
        var (userId, client) = await AuthedAsync();
        var personId = await SeedPersonAsync(userId);

        await client.PostAsJsonAsync("/api/nudges", new { title = "linked", cadenceType = "Daily", personId });
        await client.PostAsJsonAsync("/api/nudges", new { title = "unlinked", cadenceType = "Daily" });

        var resp = await client.GetAsync($"/api/nudges?personId={personId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<List<NudgeBody>>();
        Assert.Single(items!);
        Assert.Equal("linked", items![0].Title);
    }

    [Fact]
    public async Task MarkNudged_Paused_Returns409NotActive()
    {
        var (_, client) = await AuthedAsync();
        var create = await client.PostAsJsonAsync("/api/nudges", new { title = "t", cadenceType = "Daily" });
        var n = await create.Content.ReadFromJsonAsync<NudgeBody>();

        var pause = await client.PostAsync($"/api/nudges/{n!.Id}/pause", null);
        Assert.Equal(HttpStatusCode.OK, pause.StatusCode);

        var mark = await client.PostAsync($"/api/nudges/{n.Id}/mark-nudged", null);
        Assert.Equal(HttpStatusCode.Conflict, mark.StatusCode);
        var problem = await mark.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.notActive", problem!.Code);
    }

    [Fact]
    public async Task Pause_AlreadyPaused_Returns409()
    {
        var (_, client) = await AuthedAsync();
        var create = await client.PostAsJsonAsync("/api/nudges", new { title = "t", cadenceType = "Daily" });
        var n = await create.Content.ReadFromJsonAsync<NudgeBody>();

        await client.PostAsync($"/api/nudges/{n!.Id}/pause", null);
        var second = await client.PostAsync($"/api/nudges/{n.Id}/pause", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.alreadyPaused", problem!.Code);
    }

    [Fact]
    public async Task Resume_Active_Returns409AlreadyActive()
    {
        var (_, client) = await AuthedAsync();
        var create = await client.PostAsJsonAsync("/api/nudges", new { title = "t", cadenceType = "Daily" });
        var n = await create.Content.ReadFromJsonAsync<NudgeBody>();

        var resp = await client.PostAsync($"/api/nudges/{n!.Id}/resume", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.alreadyActive", problem!.Code);
    }

    [Fact]
    public async Task Delete_Found_Returns204()
    {
        var (_, client) = await AuthedAsync();
        var create = await client.PostAsJsonAsync("/api/nudges", new { title = "t", cadenceType = "Daily" });
        var n = await create.Content.ReadFromJsonAsync<NudgeBody>();

        var del = await client.DeleteAsync($"/api/nudges/{n!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var get = await client.GetAsync($"/api/nudges/{n.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task UpdateCadence_ToMonthly_RecomputesDueDate()
    {
        var (_, client) = await AuthedAsync();
        var create = await client.PostAsJsonAsync("/api/nudges", new { title = "t", cadenceType = "Weekly", dayOfWeek = "Thursday" });
        var n = await create.Content.ReadFromJsonAsync<NudgeBody>();

        var patch = await client.PatchAsJsonAsync($"/api/nudges/{n!.Id}/cadence", new
        {
            cadenceType = "Monthly",
            dayOfMonth = 1,
        });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var updated = await patch.Content.ReadFromJsonAsync<NudgeBody>();
        Assert.Equal("Monthly", updated!.Cadence.Type);
        Assert.Equal(1, updated.Cadence.DayOfMonth);
    }

    [Fact]
    public async Task Patch_EmptyTitle_Returns400Validation()
    {
        var (_, client) = await AuthedAsync();
        var create = await client.PostAsJsonAsync("/api/nudges", new { title = "t", cadenceType = "Daily" });
        var n = await create.Content.ReadFromJsonAsync<NudgeBody>();

        var patch = await client.PatchAsJsonAsync($"/api/nudges/{n!.Id}", new { title = "", notes = (string?)null, personId = (Guid?)null, initiativeId = (Guid?)null });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
        var problem = await patch.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.Equal("nudge.validation", problem!.Code);
    }
}
