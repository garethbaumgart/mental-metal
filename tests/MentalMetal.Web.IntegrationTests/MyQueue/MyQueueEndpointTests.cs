using System.Net;
using MentalMetal.Application.Common;
using MentalMetal.Domain.Captures;
using MentalMetal.Domain.Commitments;
using MentalMetal.Domain.Delegations;
using MentalMetal.Domain.People;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.MyQueue;

public sealed class MyQueueEndpointTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private sealed record QueueItemBody(
        string ItemType,
        Guid Id,
        string Title,
        string Status,
        DateOnly? DueDate,
        bool IsOverdue,
        Guid? PersonId,
        string? PersonName,
        Guid? InitiativeId,
        string? InitiativeName,
        int? DaysSinceCaptured,
        DateTimeOffset? LastFollowedUpAt,
        int PriorityScore,
        bool SuggestDelegate);

    private sealed record QueueCountsBody(int Overdue, int DueSoon, int StaleCaptures, int StaleDelegations, int Total);

    private sealed record QueueFiltersBody(
        string Scope,
        IReadOnlyList<string> ItemType,
        Guid? PersonId,
        Guid? InitiativeId);

    private sealed record MyQueueResponseBody(
        IReadOnlyList<QueueItemBody> Items,
        QueueCountsBody Counts,
        QueueFiltersBody Filters);

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

    private async Task SeedOverdueCommitmentAsync(Guid userId, Guid personId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICommitmentRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var c = Commitment.Create(userId, "ship spec", CommitmentDirection.MineToThem, personId, yesterday);
        await repo.AddAsync(c, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
    }

    private async Task SeedUrgentDelegationAsync(Guid userId, Guid personId)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDelegationRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var d = Delegation.Create(userId, "chase vendor", personId, priority: Priority.Urgent);
        await repo.AddAsync(d, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Get_RequiresAuth_Returns401()
    {
        var resp = await CreateClient().GetAsync("/api/my-queue");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Get_HappyPath_Returns200WithMixedItems()
    {
        var (userId, client) = await AuthedClientAsync();
        var personId = await SeedPersonAsync(userId);
        await SeedOverdueCommitmentAsync(userId, personId);
        await SeedUrgentDelegationAsync(userId, personId);

        var resp = await client.GetAsync("/api/my-queue");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await ReadJsonAsync<MyQueueResponseBody>(resp);
        Assert.NotNull(body);
        Assert.Equal(2, body!.Items.Count);
        Assert.Contains(body.Items, i => i.ItemType == "Commitment" && i.IsOverdue);
        Assert.Contains(body.Items, i => i.ItemType == "Delegation");
        Assert.Equal(2, body.Counts.Total);
    }

    [Fact]
    public async Task Get_InvalidScope_Returns400()
    {
        var (_, client) = await AuthedClientAsync();
        var resp = await client.GetAsync("/api/my-queue?scope=someday");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Get_InvalidItemType_Returns400()
    {
        var (_, client) = await AuthedClientAsync();
        var resp = await client.GetAsync("/api/my-queue?itemType=other");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Get_FiltersByItemType_Commitment()
    {
        var (userId, client) = await AuthedClientAsync();
        var personId = await SeedPersonAsync(userId);
        await SeedOverdueCommitmentAsync(userId, personId);
        await SeedUrgentDelegationAsync(userId, personId);

        var resp = await client.GetAsync("/api/my-queue?itemType=commitment");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await ReadJsonAsync<MyQueueResponseBody>(resp);
        Assert.Single(body!.Items);
        Assert.Equal("Commitment", body.Items[0].ItemType);
    }

    [Fact]
    public async Task Get_IsolatesByUser()
    {
        var (userA, _) = await AuthedClientAsync();
        var personA = await SeedPersonAsync(userA);
        await SeedOverdueCommitmentAsync(userA, personA);

        var (_, clientB) = await AuthedClientAsync();
        var resp = await clientB.GetAsync("/api/my-queue");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await ReadJsonAsync<MyQueueResponseBody>(resp);
        Assert.Empty(body!.Items);
    }

    [Fact]
    public async Task Get_ResponseShape_IncludesFiltersBlock()
    {
        var (_, client) = await AuthedClientAsync();
        var resp = await client.GetAsync("/api/my-queue?scope=overdue&itemType=commitment&itemType=delegation");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await ReadJsonAsync<MyQueueResponseBody>(resp);
        Assert.NotNull(body);
        Assert.Equal("Overdue", body!.Filters.Scope);
        Assert.Equal(2, body.Filters.ItemType.Count);
        Assert.Contains("Commitment", body.Filters.ItemType);
        Assert.Contains("Delegation", body.Filters.ItemType);
    }
}
