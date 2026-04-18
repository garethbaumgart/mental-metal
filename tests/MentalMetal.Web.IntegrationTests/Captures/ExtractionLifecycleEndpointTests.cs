using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MentalMetal.Application.Common;
using MentalMetal.Application.Common.Ai;
using MentalMetal.Domain.Users;
using MentalMetal.Infrastructure.Ai;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Captures;

/// <summary>
/// Regression coverage for GitHub issue #118: `AiExtraction` initialised its
/// `IReadOnlyList<T>` properties to `[]`, which lowers to `Array.Empty&lt;T&gt;()`.
/// EF's change tracker then tried to populate those fixed-size arrays on rehydrate,
/// causing tracking-mode reads (GET-by-id, confirm-extraction, discard-extraction,
/// retry) to throw `NotSupportedException`. These tests walk the full extraction
/// lifecycle against Postgres to lock in the fix.
/// </summary>
public sealed class ExtractionLifecycleEndpointTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record CaptureResponseBody(
        Guid Id,
        Guid UserId,
        string RawContent,
        string CaptureType,
        string ProcessingStatus,
        string ExtractionStatus,
        JsonElement? AiExtraction,
        string? FailureReason,
        List<Guid> LinkedPersonIds,
        List<Guid> LinkedInitiativeIds,
        List<Guid> SpawnedCommitmentIds,
        List<Guid> SpawnedDelegationIds,
        List<Guid> SpawnedObservationIds,
        string? Title,
        DateTimeOffset CapturedAt,
        DateTimeOffset? ProcessedAt,
        string? Source,
        DateTimeOffset UpdatedAt,
        bool Triaged,
        DateTimeOffset? TriagedAtUtc,
        bool ExtractionResolved);

    private sealed record ConfirmResponseBody(CaptureResponseBody Capture, IReadOnlyList<string> Warnings);

    private const string CannedExtractionJson = """
        {
          "summary": "Test summary",
          "commitments": [
            { "description": "Send follow-up email", "direction": "MineToThem", "personHint": "Nobody", "dueDate": null }
          ],
          "delegations": [
            { "description": "Chase vendor", "personHint": "Nobody", "dueDate": null }
          ],
          "observations": [],
          "decisions": ["Go ahead"],
          "risksIdentified": ["Timeline slippage"],
          "suggestedPersonLinks": [],
          "suggestedInitiativeLinks": [],
          "confidenceScore": 0.8
        }
        """;

    private HttpClient CreateClientWithFakeAi(FakeAiCompletionService fakeAi)
    {
        var customised = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services
                    .Where(s => s.ServiceType == typeof(IAiCompletionService))
                    .ToList();
                foreach (var s in existing) services.Remove(s);
                services.AddScoped<IAiCompletionService>(_ => fakeAi);
            });
        });
        return customised.CreateClient();
    }

    private async Task ConfigureAiProviderAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<IUserRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var user = await users.GetByIdAsync(userId, CancellationToken.None);
        user!.ConfigureAiProvider(AiProvider.Anthropic, "fake", "claude-test", maxTokens: 1500);
        await uow.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<(Guid UserId, HttpClient Client)> AuthedClientWithFakeAiAsync(string? cannedJson = null)
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        await ConfigureAiProviderAsync(userId);

        var fakeAi = new FakeAiCompletionService
        {
            ResponderAsync = (_, _) => Task.FromResult(new AiCompletionResult(
                Content: cannedJson ?? CannedExtractionJson,
                InputTokens: 100,
                OutputTokens: 50,
                Model: "test-model",
                Provider: AiProvider.Anthropic)),
        };
        var client = WithBearer(CreateClientWithFakeAi(fakeAi), token);
        return (userId, client);
    }

    private static async Task<Guid> CreateAndProcessCaptureAsync(HttpClient client, string rawContent = "Some raw note")
    {
        var create = await client.PostAsJsonAsync("/api/captures", new
        {
            rawContent,
            type = "QuickNote",
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CaptureResponseBody>(JsonOptions);
        Assert.NotNull(created);

        var process = await client.PostAsync($"/api/captures/{created!.Id}/process", content: null);
        Assert.Equal(HttpStatusCode.Accepted, process.StatusCode);

        return created.Id;
    }

    [Fact]
    public async Task GetById_AfterProcessing_Returns200_NotNotSupported()
    {
        // Repro for #118: pre-fix the AiExtraction `[]` defaults became fixed-size
        // arrays, and EF tracking-mode rehydrate of the owned JSON blob threw
        // NotSupportedException on subsequent GET-by-id.
        var (_, client) = await AuthedClientWithFakeAiAsync();
        var captureId = await CreateAndProcessCaptureAsync(client);

        var get = await client.GetAsync($"/api/captures/{captureId}");

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var body = await ReadJsonAsync<CaptureResponseBody>(get);
        Assert.NotNull(body);
        Assert.Equal("Processed", body!.ProcessingStatus);
        Assert.Equal("Pending", body.ExtractionStatus);
        Assert.NotNull(body.AiExtraction);
    }

    [Fact]
    public async Task ConfirmExtraction_AfterProcessing_Returns200()
    {
        var (_, client) = await AuthedClientWithFakeAiAsync();
        var captureId = await CreateAndProcessCaptureAsync(client);

        var confirm = await client.PostAsync($"/api/captures/{captureId}/confirm-extraction", content: null);

        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var body = await confirm.Content.ReadFromJsonAsync<ConfirmResponseBody>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Confirmed", body!.Capture.ExtractionStatus);
        Assert.True(body.Capture.ExtractionResolved);
    }

    [Fact]
    public async Task DiscardExtraction_AfterProcessing_Returns200()
    {
        var (_, client) = await AuthedClientWithFakeAiAsync();
        var captureId = await CreateAndProcessCaptureAsync(client);

        var discard = await client.PostAsync($"/api/captures/{captureId}/discard-extraction", content: null);

        Assert.Equal(HttpStatusCode.OK, discard.StatusCode);
        var body = await ReadJsonAsync<CaptureResponseBody>(discard);
        Assert.NotNull(body);
        Assert.Equal("Discarded", body!.ExtractionStatus);
        Assert.True(body.ExtractionResolved);
    }

    [Fact]
    public async Task GetById_ExtractionWithEmptyCollections_DoesNotThrowOnRehydrate()
    {
        // Exercises the fix for empty-collection rehydrate specifically: pre-fix,
        // even an extraction with zero commitments/delegations/observations would
        // blow up GET-by-id because the default `[]` arrays are fixed-size.
        const string emptyExtraction = """
            {
              "summary": "Nothing actionable",
              "commitments": [],
              "delegations": [],
              "observations": [],
              "decisions": [],
              "risksIdentified": [],
              "suggestedPersonLinks": [],
              "suggestedInitiativeLinks": [],
              "confidenceScore": 0.5
            }
            """;
        var (_, client) = await AuthedClientWithFakeAiAsync(emptyExtraction);
        var captureId = await CreateAndProcessCaptureAsync(client);

        var get = await client.GetAsync($"/api/captures/{captureId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }
}
