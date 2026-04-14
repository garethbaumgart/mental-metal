using System.Net;
using System.Net.Http.Json;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.People;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.DailyCloseOut;

public sealed class DailyCloseOutEndpointsTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record CloseOutQueueItemBody(
        Guid Id,
        string RawContent,
        string CaptureType,
        string ProcessingStatus,
        string ExtractionStatus,
        bool ExtractionResolved,
        IReadOnlyList<Guid> LinkedPersonIds,
        IReadOnlyList<Guid> LinkedInitiativeIds);

    private sealed record CloseOutQueueCountsBody(int Total, int Raw, int Processing, int Processed, int Failed);
    private sealed record CloseOutQueueResponseBody(IReadOnlyList<CloseOutQueueItemBody> Items, CloseOutQueueCountsBody Counts);

    private sealed record DailyCloseOutLogBody(
        Guid Id,
        DateOnly Date,
        DateTimeOffset ClosedAtUtc,
        int ConfirmedCount,
        int DiscardedCount,
        int RemainingCount);

    private sealed record UserProfileBody(
        Guid Id,
        string Email,
        DateTimeOffset? LastCloseOutAtUtc);

    private sealed record CaptureBody(
        Guid Id,
        string ProcessingStatus,
        bool Triaged,
        bool ExtractionResolved,
        IReadOnlyList<Guid> LinkedPersonIds,
        IReadOnlyList<Guid> LinkedInitiativeIds);

    private async Task<(Guid UserId, HttpClient Client)> AuthedClientAsync()
    {
        var (userId, token) = await SeedUserWithPasswordAndSignInAsync(
            $"user-{Guid.NewGuid():N}@test.invalid", "password-123");
        return (userId, WithBearer(CreateClient(), token));
    }

    private async Task<Capture> SeedCaptureAsync(
        Guid userId,
        ProcessingStatus status = ProcessingStatus.Raw,
        bool triaged = false)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICaptureRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var capture = Capture.Create(userId, $"content {Guid.NewGuid():N}", CaptureType.QuickNote);
        if (status == ProcessingStatus.Processing)
        {
            capture.BeginProcessing();
        }
        else if (status == ProcessingStatus.Processed)
        {
            capture.BeginProcessing();
            capture.CompleteProcessing(new AiExtraction { Summary = "test" });
        }
        else if (status == ProcessingStatus.Failed)
        {
            capture.BeginProcessing();
            capture.FailProcessing("error");
        }

        if (triaged)
            capture.QuickDiscard();

        await repo.AddAsync(capture, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
        return capture;
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

    // --- GetCloseOutQueue ---

    [Fact]
    public async Task GetQueue_MixedStatuses_ReturnsPendingOnlyWithCounts()
    {
        var (userId, client) = await AuthedClientAsync();
        await SeedCaptureAsync(userId, ProcessingStatus.Raw);
        await SeedCaptureAsync(userId, ProcessingStatus.Failed);
        await SeedCaptureAsync(userId, ProcessingStatus.Processed);
        // Already triaged should be excluded:
        await SeedCaptureAsync(userId, ProcessingStatus.Raw, triaged: true);

        var resp = await client.GetAsync("/api/daily-close-out/queue");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await ReadJsonAsync<CloseOutQueueResponseBody>(resp);

        Assert.Equal(3, body!.Counts.Total);
        Assert.Equal(1, body.Counts.Raw);
        Assert.Equal(1, body.Counts.Processed);
        Assert.Equal(1, body.Counts.Failed);
        Assert.Equal(3, body.Items.Count);
    }

    [Fact]
    public async Task GetQueue_IsolatesByUser()
    {
        var (userA, clientA) = await AuthedClientAsync();
        var (_, clientB) = await AuthedClientAsync();
        await SeedCaptureAsync(userA, ProcessingStatus.Raw);

        var respB = await clientB.GetAsync("/api/daily-close-out/queue");
        var body = await ReadJsonAsync<CloseOutQueueResponseBody>(respB);
        Assert.Empty(body!.Items);
    }

    // --- QuickDiscard ---

    [Fact]
    public async Task QuickDiscard_ExistingCapture_ExcludesFromQueue()
    {
        var (userId, client) = await AuthedClientAsync();
        var capture = await SeedCaptureAsync(userId, ProcessingStatus.Raw);

        var resp = await client.PostAsync(
            $"/api/daily-close-out/captures/{capture.Id}/quick-discard", content: null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // idempotent:
        var resp2 = await client.PostAsync(
            $"/api/daily-close-out/captures/{capture.Id}/quick-discard", content: null);
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);

        var queue = await client.GetAsync("/api/daily-close-out/queue");
        var body = await ReadJsonAsync<CloseOutQueueResponseBody>(queue);
        Assert.Empty(body!.Items);
    }

    [Fact]
    public async Task QuickDiscard_ForeignCapture_Returns404()
    {
        var (userA, _) = await AuthedClientAsync();
        var capture = await SeedCaptureAsync(userA, ProcessingStatus.Raw);
        var (_, clientB) = await AuthedClientAsync();

        var resp = await clientB.PostAsync(
            $"/api/daily-close-out/captures/{capture.Id}/quick-discard", content: null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // --- Reassign ---

    [Fact]
    public async Task Reassign_AddsAndRemovesLinks()
    {
        var (userId, client) = await AuthedClientAsync();
        var capture = await SeedCaptureAsync(userId, ProcessingStatus.Raw);
        var personA = await SeedPersonAsync(userId);
        var personB = await SeedPersonAsync(userId);

        // Start by assigning person A
        var resp1 = await client.PostAsJsonAsync(
            $"/api/daily-close-out/captures/{capture.Id}/reassign",
            new { personIds = new[] { personA }, initiativeIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        // Now switch to person B
        var resp2 = await client.PostAsJsonAsync(
            $"/api/daily-close-out/captures/{capture.Id}/reassign",
            new { personIds = new[] { personB }, initiativeIds = Array.Empty<Guid>() });
        var after = await ReadJsonAsync<CloseOutQueueItemBody>(resp2);
        Assert.Single(after!.LinkedPersonIds);
        Assert.Contains(personB, after.LinkedPersonIds);

        // Clear all
        var resp3 = await client.PostAsJsonAsync(
            $"/api/daily-close-out/captures/{capture.Id}/reassign",
            new { personIds = Array.Empty<Guid>(), initiativeIds = Array.Empty<Guid>() });
        var cleared = await ReadJsonAsync<CloseOutQueueItemBody>(resp3);
        Assert.Empty(cleared!.LinkedPersonIds);
    }

    [Fact]
    public async Task Reassign_UnknownPersonId_Returns400()
    {
        var (userId, client) = await AuthedClientAsync();
        var capture = await SeedCaptureAsync(userId, ProcessingStatus.Raw);

        var resp = await client.PostAsJsonAsync(
            $"/api/daily-close-out/captures/{capture.Id}/reassign",
            new { personIds = new[] { Guid.NewGuid() }, initiativeIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // --- CloseOutDay ---

    [Fact]
    public async Task CloseOutDay_FirstCall_RecordsEntry()
    {
        var (userId, client) = await AuthedClientAsync();
        await SeedCaptureAsync(userId, ProcessingStatus.Raw);

        var resp = await client.PostAsJsonAsync("/api/daily-close-out/close", new { });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await ReadJsonAsync<DailyCloseOutLogBody>(resp);
        Assert.Equal(1, body!.RemainingCount);
        Assert.Equal(0, body.ConfirmedCount);
        Assert.Equal(0, body.DiscardedCount);
    }

    [Fact]
    public async Task CloseOutDay_SecondCall_OverwritesSameDate()
    {
        var (userId, client) = await AuthedClientAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var resp1 = await client.PostAsJsonAsync("/api/daily-close-out/close", new { date });
        var first = await ReadJsonAsync<DailyCloseOutLogBody>(resp1);

        // Seed a capture after first close-out
        await SeedCaptureAsync(userId, ProcessingStatus.Raw);

        var resp2 = await client.PostAsJsonAsync("/api/daily-close-out/close", new { date });
        var second = await ReadJsonAsync<DailyCloseOutLogBody>(resp2);

        Assert.Equal(first!.Id, second!.Id);
        Assert.Equal(1, second.RemainingCount);
    }

    [Fact]
    public async Task CloseOutDay_ExplicitDate_RecordsForThatDate()
    {
        var (_, client) = await AuthedClientAsync();
        var date = new DateOnly(2025, 12, 31);

        var resp = await client.PostAsJsonAsync("/api/daily-close-out/close", new { date });
        var body = await ReadJsonAsync<DailyCloseOutLogBody>(resp);
        Assert.Equal(date, body!.Date);
    }

    // --- GetCloseOutLog ---

    [Fact]
    public async Task GetLog_EmptyCase_ReturnsEmptyArray()
    {
        var (_, client) = await AuthedClientAsync();
        var resp = await client.GetAsync("/api/daily-close-out/log");
        var body = await ReadJsonAsync<List<DailyCloseOutLogBody>>(resp);
        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetLog_Ordering_MostRecentFirst()
    {
        var (_, client) = await AuthedClientAsync();
        await client.PostAsJsonAsync("/api/daily-close-out/close", new { date = new DateOnly(2025, 1, 1) });
        await client.PostAsJsonAsync("/api/daily-close-out/close", new { date = new DateOnly(2025, 6, 1) });
        await client.PostAsJsonAsync("/api/daily-close-out/close", new { date = new DateOnly(2025, 3, 1) });

        var resp = await client.GetAsync("/api/daily-close-out/log");
        var body = await ReadJsonAsync<List<DailyCloseOutLogBody>>(resp);
        Assert.Equal(3, body!.Count);
        Assert.Equal(new DateOnly(2025, 6, 1), body[0].Date);
        Assert.Equal(new DateOnly(2025, 3, 1), body[1].Date);
        Assert.Equal(new DateOnly(2025, 1, 1), body[2].Date);
    }

    [Fact]
    public async Task GetLog_LimitClamping_CapsAt90()
    {
        var (_, client) = await AuthedClientAsync();
        await client.PostAsJsonAsync("/api/daily-close-out/close", new { date = new DateOnly(2025, 1, 1) });

        var resp = await client.GetAsync("/api/daily-close-out/log?limit=1000");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await ReadJsonAsync<List<DailyCloseOutLogBody>>(resp);
        Assert.Single(body!);
    }

    // --- Modified capture endpoints ---

    [Fact]
    public async Task ListCaptures_DefaultExcludesTriaged()
    {
        var (userId, client) = await AuthedClientAsync();
        await SeedCaptureAsync(userId, ProcessingStatus.Raw);
        await SeedCaptureAsync(userId, ProcessingStatus.Raw, triaged: true);

        var resp = await client.GetAsync("/api/captures");
        var list = await ReadJsonAsync<List<CaptureBody>>(resp);
        Assert.Single(list!);
        Assert.False(list![0].Triaged);
    }

    [Fact]
    public async Task ListCaptures_IncludeTriagedTrue_IncludesTriaged()
    {
        var (userId, client) = await AuthedClientAsync();
        await SeedCaptureAsync(userId, ProcessingStatus.Raw);
        await SeedCaptureAsync(userId, ProcessingStatus.Raw, triaged: true);

        var resp = await client.GetAsync("/api/captures?includeTriaged=true");
        var list = await ReadJsonAsync<List<CaptureBody>>(resp);
        Assert.Equal(2, list!.Count);
    }

    [Fact]
    public async Task GetCurrentUser_IncludesLastCloseOutAtUtc_AfterCloseOut()
    {
        var (_, client) = await AuthedClientAsync();
        var before = await client.GetAsync("/api/users/me");
        var beforeBody = await ReadJsonAsync<UserProfileBody>(before);
        Assert.Null(beforeBody!.LastCloseOutAtUtc);

        await client.PostAsJsonAsync("/api/daily-close-out/close", new { });

        var after = await client.GetAsync("/api/users/me");
        var afterBody = await ReadJsonAsync<UserProfileBody>(after);
        Assert.NotNull(afterBody!.LastCloseOutAtUtc);
    }
}
