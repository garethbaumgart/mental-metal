using System.Net;
using System.Net.Http.Json;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Interviews;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using MentalMetal.Web.IntegrationTests.Briefings;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Interviews;

public sealed class InterviewEndpointsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record InterviewBody(
        Guid Id,
        Guid UserId,
        Guid CandidatePersonId,
        string RoleTitle,
        string Stage,
        DateTimeOffset? ScheduledAtUtc,
        DateTimeOffset? CompletedAtUtc,
        string? Decision,
        InterviewTranscriptBody? Transcript,
        IReadOnlyList<ScorecardBody> Scorecards,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    private sealed record ScorecardBody(Guid Id, string Competency, int Rating, string? Notes, DateTimeOffset RecordedAtUtc);
    private sealed record InterviewTranscriptBody(string RawText, string? Summary, string? RecommendedDecision, IReadOnlyList<string>? RiskSignals, DateTimeOffset? AnalyzedAtUtc, string? Model);
    private sealed record AnalysisBody(string Summary, string? RecommendedDecision, IReadOnlyList<string> RiskSignals, string Model, DateTimeOffset AnalyzedAtUtc, string? Warning);

    private HttpClient CreateClientWithFakeAi(FakeAiCompletionService fakeAi)
    {
        var customised = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.Where(s => s.ServiceType == typeof(IAiCompletionService)).ToList();
                foreach (var s in existing) services.Remove(s);
                services.AddScoped<IAiCompletionService>(_ => fakeAi);
            });
        });
        return customised.CreateClient();
    }

    private async Task<(Guid UserId, HttpClient Client)> AuthedAsync()
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        return (userId, WithBearer(CreateClient(), token));
    }

    private async Task<Guid> SeedCandidateAsync(Guid userId, string name = "Cara Candidate")
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
        var repo = sp.GetRequiredService<IPersonRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var p = Person.Create(userId, name, PersonType.Candidate);
        await repo.AddAsync(p, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        return p.Id;
    }

    private async Task ConfigureAiProviderAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
        var users = sp.GetRequiredService<IUserRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var user = await users.GetByIdAsync(userId, CancellationToken.None);
        user!.ConfigureAiProvider(AiProvider.Anthropic, "fake", "claude-test", maxTokens: 1500);
        await uow.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CreateAdvanceDecision_HappyPath()
    {
        var (userId, client) = await AuthedAsync();
        var candidateId = await SeedCandidateAsync(userId);

        var create = await client.PostAsJsonAsync("/api/interviews", new
        {
            candidatePersonId = candidateId,
            roleTitle = "Staff Engineer",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var interview = await ReadJsonAsync<InterviewBody>(create);
        Assert.NotNull(interview);
        Assert.Equal("Applied", interview!.Stage);

        // Advance to ScreenScheduled then ScreenCompleted
        var adv1 = await client.PostAsJsonAsync(
            $"/api/interviews/{interview.Id}/advance",
            new { targetStage = "ScreenScheduled" });
        Assert.Equal(HttpStatusCode.OK, adv1.StatusCode);

        var adv2 = await client.PostAsJsonAsync(
            $"/api/interviews/{interview.Id}/advance",
            new { targetStage = "ScreenCompleted" });
        Assert.Equal(HttpStatusCode.OK, adv2.StatusCode);

        // Record decision
        var dec = await client.PostAsJsonAsync(
            $"/api/interviews/{interview.Id}/decision",
            new { decision = "Hire" });
        Assert.Equal(HttpStatusCode.OK, dec.StatusCode);
        var body = await ReadJsonAsync<InterviewBody>(dec);
        Assert.Equal("Hire", body!.Decision);
    }

    [Fact]
    public async Task Advance_InvalidTransition_Returns409()
    {
        var (userId, client) = await AuthedAsync();
        var candidateId = await SeedCandidateAsync(userId);
        var create = await client.PostAsJsonAsync("/api/interviews", new
        {
            candidatePersonId = candidateId,
            roleTitle = "Staff Engineer",
        });
        var interview = await ReadJsonAsync<InterviewBody>(create);

        var resp = await client.PostAsJsonAsync(
            $"/api/interviews/{interview!.Id}/advance",
            new { targetStage = "OnsiteScheduled" });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task UserIsolation_OtherUsersInterviewReturns404()
    {
        var (userAId, clientA) = await AuthedAsync();
        var (_, clientB) = await AuthedAsync();
        var candidateId = await SeedCandidateAsync(userAId);

        var create = await clientA.PostAsJsonAsync("/api/interviews", new
        {
            candidatePersonId = candidateId,
            roleTitle = "Staff Engineer",
        });
        var interview = await ReadJsonAsync<InterviewBody>(create);

        var resp = await clientB.GetAsync($"/api/interviews/{interview!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Create_CandidateBelongsToOtherUser_Returns404()
    {
        var (userAId, _) = await AuthedAsync();
        var candidateId = await SeedCandidateAsync(userAId);
        // user B tries to use A's candidate
        var (_, clientB) = await AuthedAsync();

        var resp = await clientB.PostAsJsonAsync("/api/interviews", new
        {
            candidatePersonId = candidateId,
            roleTitle = "Role",
        });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithoutConfiguredProvider_Returns409()
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        var candidateId = await SeedCandidateAsync(userId);

        var fakeAi = new FakeAiCompletionService();
        fakeAi.ThrowAiNotConfigured();
        var client = WithBearer(CreateClientWithFakeAi(fakeAi), token);

        var create = await client.PostAsJsonAsync("/api/interviews", new
        {
            candidatePersonId = candidateId,
            roleTitle = "Role",
        });
        var interview = await ReadJsonAsync<InterviewBody>(create);

        // Set a transcript so the analyze precondition passes. Assert the PUT itself
        // succeeds so that a regression there can't masquerade as an analyze failure.
        var transcriptResp = await client.PutAsJsonAsync(
            $"/api/interviews/{interview!.Id}/transcript",
            new { rawText = "some transcript" });
        Assert.Equal(HttpStatusCode.OK, transcriptResp.StatusCode);

        var resp = await client.PostAsync($"/api/interviews/{interview.Id}/analyze", content: null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithoutTranscript_Returns409()
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        await ConfigureAiProviderAsync(userId);
        var candidateId = await SeedCandidateAsync(userId);

        var fakeAi = new FakeAiCompletionService();
        var client = WithBearer(CreateClientWithFakeAi(fakeAi), token);

        var create = await client.PostAsJsonAsync("/api/interviews", new
        {
            candidatePersonId = candidateId,
            roleTitle = "Role",
        });
        var interview = await ReadJsonAsync<InterviewBody>(create);

        var resp = await client.PostAsync($"/api/interviews/{interview!.Id}/analyze", content: null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Scorecards_AddUpdateDelete_RoundTrip()
    {
        var (userId, client) = await AuthedAsync();
        var candidateId = await SeedCandidateAsync(userId);

        var create = await client.PostAsJsonAsync("/api/interviews", new
        {
            candidatePersonId = candidateId,
            roleTitle = "Staff Engineer",
        });
        var interview = await ReadJsonAsync<InterviewBody>(create);

        // Add
        var addResp = await client.PostAsJsonAsync(
            $"/api/interviews/{interview!.Id}/scorecards",
            new { competency = "System Design", rating = 4, notes = "Strong" });
        Assert.Equal(HttpStatusCode.Created, addResp.StatusCode);
        var scorecard = await ReadJsonAsync<ScorecardBody>(addResp);

        // Update
        var updResp = await client.PutAsJsonAsync(
            $"/api/interviews/{interview.Id}/scorecards/{scorecard!.Id}",
            new { competency = "System Design", rating = 5, notes = "Even stronger" });
        Assert.Equal(HttpStatusCode.OK, updResp.StatusCode);

        // Verify the update actually persisted before we delete - otherwise a silently
        // dropped update could still pass the delete assertions that follow.
        var afterUpdate = await client.GetAsync($"/api/interviews/{interview.Id}");
        var afterUpdateBody = await ReadJsonAsync<InterviewBody>(afterUpdate);
        var persistedCard = Assert.Single(afterUpdateBody!.Scorecards);
        Assert.Equal(5, persistedCard.Rating);
        Assert.Equal("Even stronger", persistedCard.Notes);

        // Delete
        var delResp = await client.DeleteAsync($"/api/interviews/{interview.Id}/scorecards/{scorecard.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // Final state: empty
        var getResp = await client.GetAsync($"/api/interviews/{interview.Id}");
        var final = await ReadJsonAsync<InterviewBody>(getResp);
        Assert.Empty(final!.Scorecards);
    }
}
