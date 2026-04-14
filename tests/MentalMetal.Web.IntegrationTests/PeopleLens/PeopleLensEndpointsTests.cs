using System.Net;
using System.Net.Http.Json;
using MentalMetal.Web.IntegrationTests.Infrastructure;

namespace MentalMetal.Web.IntegrationTests.PeopleLens;

public sealed class PeopleLensEndpointsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record OneOnOneBody(
        Guid Id,
        Guid UserId,
        Guid PersonId,
        DateOnly OccurredAt,
        string? Notes,
        int? MoodRating,
        IReadOnlyList<string> Topics,
        IReadOnlyList<ActionItemBody> ActionItems,
        IReadOnlyList<FollowUpBody> FollowUps);

    private sealed record ActionItemBody(Guid Id, string Description, bool Completed);
    private sealed record FollowUpBody(Guid Id, string Description, bool Resolved);

    private sealed record ObservationBody(
        Guid Id,
        Guid UserId,
        Guid PersonId,
        string Description,
        string Tag,
        DateOnly OccurredAt,
        Guid? SourceCaptureId);

    private sealed record GoalBody(
        Guid Id,
        Guid UserId,
        Guid PersonId,
        string Title,
        string? Description,
        string GoalType,
        string Status,
        DateOnly? TargetDate,
        string? DeferralReason,
        DateTimeOffset? AchievedAt,
        IReadOnlyList<CheckInBody> CheckIns);

    private sealed record CheckInBody(Guid Id, string Note, int? Progress, DateTimeOffset RecordedAt);

    private async Task<HttpClient> AuthedClientAsync()
    {
        var (_, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        return WithBearer(CreateClient(), token);
    }

    // ---- OneOnOne ----

    [Fact]
    public async Task CreateOneOnOne_Minimal_Returns201AndPersists()
    {
        var client = await AuthedClientAsync();
        var personId = Guid.NewGuid();

        var resp = await client.PostAsJsonAsync("/api/one-on-ones", new
        {
            personId,
            occurredAt = "2026-04-10",
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await ReadJsonAsync<OneOnOneBody>(resp);
        Assert.NotNull(body);
        Assert.Equal(personId, body!.PersonId);
        Assert.Empty(body.Topics);
    }

    [Fact]
    public async Task CreateOneOnOne_MissingPersonId_Returns400()
    {
        var client = await AuthedClientAsync();

        var resp = await client.PostAsJsonAsync("/api/one-on-ones", new
        {
            personId = Guid.Empty,
            occurredAt = "2026-04-10",
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task OneOnOne_ActionItemLifecycle_Works()
    {
        var client = await AuthedClientAsync();
        var personId = Guid.NewGuid();

        var createResp = await client.PostAsJsonAsync("/api/one-on-ones", new
        {
            personId,
            occurredAt = "2026-04-10",
        });
        var oneOnOne = (await ReadJsonAsync<OneOnOneBody>(createResp))!;

        var addResp = await client.PostAsJsonAsync(
            $"/api/one-on-ones/{oneOnOne.Id}/action-items",
            new { description = "Follow up" });
        Assert.Equal(HttpStatusCode.OK, addResp.StatusCode);
        var withItem = (await ReadJsonAsync<OneOnOneBody>(addResp))!;
        Assert.Single(withItem.ActionItems);
        var itemId = withItem.ActionItems[0].Id;

        var completeResp = await client.PostAsJsonAsync(
            $"/api/one-on-ones/{oneOnOne.Id}/action-items/{itemId}/complete", new { });
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completed = (await ReadJsonAsync<OneOnOneBody>(completeResp))!;
        Assert.True(completed.ActionItems[0].Completed);

        var deleteResp = await client.DeleteAsync(
            $"/api/one-on-ones/{oneOnOne.Id}/action-items/{itemId}");
        Assert.Equal(HttpStatusCode.OK, deleteResp.StatusCode);
    }

    [Fact]
    public async Task ListOneOnOnes_FilterByPerson_ReturnsOnlyMatching()
    {
        var client = await AuthedClientAsync();
        var personA = Guid.NewGuid();
        var personB = Guid.NewGuid();

        await client.PostAsJsonAsync("/api/one-on-ones", new { personId = personA, occurredAt = "2026-04-01" });
        await client.PostAsJsonAsync("/api/one-on-ones", new { personId = personB, occurredAt = "2026-04-02" });

        var resp = await client.GetAsync($"/api/one-on-ones?personId={personA}");
        var list = await ReadJsonAsync<List<OneOnOneBody>>(resp);
        Assert.Single(list!);
        Assert.Equal(personA, list![0].PersonId);
    }

    [Fact]
    public async Task GetOneOnOne_OtherUser_Returns404()
    {
        var clientA = await AuthedClientAsync();
        var personId = Guid.NewGuid();
        var createResp = await clientA.PostAsJsonAsync("/api/one-on-ones", new { personId, occurredAt = "2026-04-10" });
        var oneOnOne = (await ReadJsonAsync<OneOnOneBody>(createResp))!;

        var clientB = await AuthedClientAsync();
        var resp = await clientB.GetAsync($"/api/one-on-ones/{oneOnOne.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---- Observation ----

    [Fact]
    public async Task CreateObservation_Valid_Returns201()
    {
        var client = await AuthedClientAsync();
        var personId = Guid.NewGuid();

        var resp = await client.PostAsJsonAsync("/api/observations", new
        {
            personId,
            description = "Led incident response",
            tag = "Win",
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await ReadJsonAsync<ObservationBody>(resp);
        Assert.Equal("Win", body!.Tag);
    }

    [Fact]
    public async Task CreateObservation_EmptyDescription_Returns400()
    {
        var client = await AuthedClientAsync();

        var resp = await client.PostAsJsonAsync("/api/observations", new
        {
            personId = Guid.NewGuid(),
            description = "",
            tag = "Win",
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ListObservations_FilterByTag_ReturnsMatching()
    {
        var client = await AuthedClientAsync();
        var personId = Guid.NewGuid();

        await client.PostAsJsonAsync("/api/observations", new { personId, description = "a win", tag = "Win" });
        await client.PostAsJsonAsync("/api/observations", new { personId, description = "a concern", tag = "Concern" });

        var resp = await client.GetAsync("/api/observations?tag=Win");
        var list = await ReadJsonAsync<List<ObservationBody>>(resp);
        Assert.Single(list!);
        Assert.Equal("Win", list![0].Tag);
    }

    [Fact]
    public async Task DeleteObservation_Existing_Returns204()
    {
        var client = await AuthedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/observations", new
        {
            personId = Guid.NewGuid(),
            description = "to delete",
            tag = "Growth",
        });
        var obs = (await ReadJsonAsync<ObservationBody>(createResp))!;

        var resp = await client.DeleteAsync($"/api/observations/{obs.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var getResp = await client.GetAsync($"/api/observations/{obs.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    // ---- Goal ----

    [Fact]
    public async Task CreateGoal_Valid_Returns201WithActiveStatus()
    {
        var client = await AuthedClientAsync();

        var resp = await client.PostAsJsonAsync("/api/goals", new
        {
            personId = Guid.NewGuid(),
            title = "Ship roadmap",
            goalType = "Performance",
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await ReadJsonAsync<GoalBody>(resp);
        Assert.Equal("Active", body!.Status);
    }

    [Fact]
    public async Task AchieveGoal_Active_Returns200()
    {
        var client = await AuthedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/goals", new
        {
            personId = Guid.NewGuid(),
            title = "Ship roadmap",
            goalType = "Performance",
        });
        var goal = (await ReadJsonAsync<GoalBody>(createResp))!;

        var resp = await client.PostAsJsonAsync($"/api/goals/{goal.Id}/achieve", new { });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var after = (await ReadJsonAsync<GoalBody>(resp))!;
        Assert.Equal("Achieved", after.Status);
        Assert.NotNull(after.AchievedAt);
    }

    [Fact]
    public async Task AchieveGoal_AlreadyAchieved_Returns409()
    {
        var client = await AuthedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/goals", new
        {
            personId = Guid.NewGuid(),
            title = "t",
            goalType = "Development",
        });
        var goal = (await ReadJsonAsync<GoalBody>(createResp))!;
        await client.PostAsJsonAsync($"/api/goals/{goal.Id}/achieve", new { });

        var resp = await client.PostAsJsonAsync($"/api/goals/{goal.Id}/achieve", new { });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task RecordGoalCheckIn_Valid_Returns200WithCheckIn()
    {
        var client = await AuthedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/goals", new
        {
            personId = Guid.NewGuid(),
            title = "t",
            goalType = "Development",
        });
        var goal = (await ReadJsonAsync<GoalBody>(createResp))!;

        var resp = await client.PostAsJsonAsync(
            $"/api/goals/{goal.Id}/check-ins",
            new { note = "Completed 3 of 5 modules", progress = 60 });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var after = (await ReadJsonAsync<GoalBody>(resp))!;
        Assert.Single(after.CheckIns);
        Assert.Equal(60, after.CheckIns[0].Progress);
    }

    [Fact]
    public async Task DeferGoal_WithReason_Returns200WithDeferralReason()
    {
        var client = await AuthedClientAsync();
        var createResp = await client.PostAsJsonAsync("/api/goals", new
        {
            personId = Guid.NewGuid(),
            title = "t",
            goalType = "Development",
        });
        var goal = (await ReadJsonAsync<GoalBody>(createResp))!;

        var resp = await client.PostAsJsonAsync(
            $"/api/goals/{goal.Id}/defer",
            new { reason = "Reprioritized to Q3" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var after = (await ReadJsonAsync<GoalBody>(resp))!;
        Assert.Equal("Deferred", after.Status);
        Assert.Equal("Reprioritized to Q3", after.DeferralReason);
    }

    [Fact]
    public async Task EvidenceSummary_DefaultQuarter_ReturnsOkWithEmpty()
    {
        var client = await AuthedClientAsync();
        var personId = Guid.NewGuid();

        var resp = await client.GetAsync($"/api/people/{personId}/evidence-summary");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
