using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.People;
using MentalMetal.Domain.Users;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Briefings;

public sealed class BriefingEndpointsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record BriefingResponseBody(
        Guid Id,
        string Type,
        string ScopeKey,
        DateTimeOffset GeneratedAtUtc,
        string MarkdownBody,
        string Model,
        int InputTokens,
        int OutputTokens,
        JsonElement FactsSummary);

    private sealed record BriefingSummaryBody(
        Guid Id,
        string Type,
        string ScopeKey,
        DateTimeOffset GeneratedAtUtc,
        string Model,
        int InputTokens,
        int OutputTokens);

    private FakeAiCompletionService CreateFakeAi() => new();

    /// <summary>
    /// Builds a client whose DI scope swaps in <paramref name="fakeAi"/> as
    /// the IAiCompletionService used by the briefing service.
    /// </summary>
    private HttpClient CreateClientWithFakeAi(FakeAiCompletionService fakeAi)
    {
        var customised = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Drop every existing IAiCompletionService registration so the test fake
                // is the only one resolved by DI.
                var existing = services
                    .Where(s => s.ServiceType == typeof(IAiCompletionService))
                    .ToList();
                foreach (var s in existing) services.Remove(s);
                services.AddScoped<IAiCompletionService>(_ => fakeAi);
            });
        });
        return customised.CreateClient();
    }

    private async Task<(Guid UserId, HttpClient Client, FakeAiCompletionService FakeAi)> AuthedClientWithFakeAiAsync(
        bool configureAiProvider = true)
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");

        if (configureAiProvider)
            await ConfigureAiProviderAsync(userId);

        var fakeAi = CreateFakeAi();
        // When the user is unconfigured, mimic the real AiCompletionService surface
        // so endpoint-level handling of AiProviderNotConfiguredException is exercised.
        if (!configureAiProvider)
            fakeAi.ThrowAiNotConfigured();

        var client = WithBearer(CreateClientWithFakeAi(fakeAi), token);
        return (userId, client, fakeAi);
    }

    private async Task ConfigureAiProviderAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
        var users = sp.GetRequiredService<IUserRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var user = await users.GetByIdAsync(userId, CancellationToken.None);
        Assert.NotNull(user);
        user!.ConfigureAiProvider(AiProvider.Anthropic, "fake-encrypted-key", "claude-test", maxTokens: 1500);
        await uow.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Guid> SeedPersonAsync(Guid userId, string name = "Sarah")
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
        var repo = sp.GetRequiredService<IPersonRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var person = Person.Create(userId, name, PersonType.DirectReport);
        await repo.AddAsync(person, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        return person.Id;
    }

    private static async Task<T?> ReadBodyAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions);

    [Fact]
    public async Task PostMorning_RequiresAuth_Returns401()
    {
        var resp = await CreateClient().PostAsync("/api/briefings/morning", null);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task PostMorning_HappyPath_Returns201ThenCachedReturns200()
    {
        var (_, client, fakeAi) = await AuthedClientWithFakeAiAsync();

        var first = await client.PostAsync("/api/briefings/morning", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstBody = await ReadBodyAsync<BriefingResponseBody>(first);
        Assert.NotNull(firstBody);
        Assert.Equal("Morning", firstBody!.Type);
        Assert.StartsWith("morning:", firstBody.ScopeKey);

        // Same client, second call → cached.
        var second = await client.PostAsync("/api/briefings/morning", null);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await ReadBodyAsync<BriefingResponseBody>(second);
        Assert.Equal(firstBody.Id, secondBody!.Id);
        Assert.Equal(1, fakeAi.CallCount);
    }

    [Fact]
    public async Task PostMorning_Force_Regenerates()
    {
        var (_, client, fakeAi) = await AuthedClientWithFakeAiAsync();

        var first = await client.PostAsync("/api/briefings/morning", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var forced = await client.PostAsync("/api/briefings/morning?force=true", null);
        Assert.Equal(HttpStatusCode.Created, forced.StatusCode);
        Assert.Equal(2, fakeAi.CallCount);
    }

    [Fact]
    public async Task PostMorning_NoAiConfig_Returns409()
    {
        var (_, client, _) = await AuthedClientWithFakeAiAsync(configureAiProvider: false);

        var resp = await client.PostAsync("/api/briefings/morning", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("ai_provider_not_configured", body);
    }

    [Fact]
    public async Task PostWeekly_HappyPath_Returns201()
    {
        var (_, client, _) = await AuthedClientWithFakeAiAsync();

        var resp = await client.PostAsync("/api/briefings/weekly", null);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await ReadBodyAsync<BriefingResponseBody>(resp);
        Assert.Equal("Weekly", body!.Type);
        Assert.StartsWith("weekly:", body.ScopeKey);
    }

    [Fact]
    public async Task PostOneOnOnePrep_UnknownPerson_Returns404()
    {
        var (_, client, _) = await AuthedClientWithFakeAiAsync();
        var unknown = Guid.NewGuid();

        var resp = await client.PostAsync($"/api/briefings/one-on-one/{unknown}", null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task PostOneOnOnePrep_OwnedPerson_Returns201()
    {
        var (userId, client, _) = await AuthedClientWithFakeAiAsync();
        var personId = await SeedPersonAsync(userId);

        var resp = await client.PostAsync($"/api/briefings/one-on-one/{personId}", null);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await ReadBodyAsync<BriefingResponseBody>(resp);
        Assert.Equal("OneOnOnePrep", body!.Type);
        Assert.Equal($"oneonone:{personId:N}", body.ScopeKey);
    }

    [Fact]
    public async Task GetRecent_BadType_Returns400()
    {
        var (_, client, _) = await AuthedClientWithFakeAiAsync();
        var resp = await client.GetAsync("/api/briefings/recent?type=monthly");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetRecent_BadLimit_Returns400()
    {
        var (_, client, _) = await AuthedClientWithFakeAiAsync();
        var resp = await client.GetAsync("/api/briefings/recent?limit=0");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetRecent_FiltersByTypeAndIsolatesByUser()
    {
        // Generate a briefing as user A.
        var (userA, clientA, _) = await AuthedClientWithFakeAiAsync();
        var morningResp = await clientA.PostAsync("/api/briefings/morning", null);
        Assert.Equal(HttpStatusCode.Created, morningResp.StatusCode);
        var morningBody = await ReadBodyAsync<BriefingResponseBody>(morningResp);

        // List as A: includes A's briefing.
        var listA = await clientA.GetAsync("/api/briefings/recent?type=Morning");
        Assert.Equal(HttpStatusCode.OK, listA.StatusCode);
        var listABody = await ReadBodyAsync<List<BriefingSummaryBody>>(listA);
        Assert.NotNull(listABody);
        Assert.Single(listABody!);
        Assert.Equal(morningBody!.Id, listABody![0].Id);

        // User B sees no briefings.
        var (_, clientB, _) = await AuthedClientWithFakeAiAsync();
        var listB = await clientB.GetAsync("/api/briefings/recent");
        Assert.Equal(HttpStatusCode.OK, listB.StatusCode);
        var listBBody = await ReadBodyAsync<List<BriefingSummaryBody>>(listB);
        Assert.Empty(listBBody!);
    }

    [Fact]
    public async Task GetById_OwnBriefing_Returns200()
    {
        var (_, client, _) = await AuthedClientWithFakeAiAsync();
        var post = await client.PostAsync("/api/briefings/morning", null);
        var posted = await ReadBodyAsync<BriefingResponseBody>(post);

        var get = await client.GetAsync($"/api/briefings/{posted!.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var body = await ReadBodyAsync<BriefingResponseBody>(get);
        Assert.Equal(posted.Id, body!.Id);
        Assert.Equal(posted.MarkdownBody, body.MarkdownBody);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var (_, client, _) = await AuthedClientWithFakeAiAsync();
        var resp = await client.GetAsync($"/api/briefings/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetById_OtherUsersBriefing_Returns404()
    {
        var (_, clientA, _) = await AuthedClientWithFakeAiAsync();
        var post = await clientA.PostAsync("/api/briefings/morning", null);
        var aBriefing = await ReadBodyAsync<BriefingResponseBody>(post);

        var (_, clientB, _) = await AuthedClientWithFakeAiAsync();
        var resp = await clientB.GetAsync($"/api/briefings/{aBriefing!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
