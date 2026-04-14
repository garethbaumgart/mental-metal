using MentalMetal.Application.Common;
using MentalMetal.Domain.Briefings;
using MentalMetal.Domain.Users;
using MentalMetal.Web.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MentalMetal.Web.IntegrationTests.Briefings;

public sealed class BriefingRepositoryTests(PostgresFixture postgres) : IntegrationTestBase(postgres)
{
    private async Task<Guid> SeedUserAsync(string email = "user-briefing@test.invalid")
    {
        var (userId, _) = await SeedUserWithPasswordAndSignInAsync(email, "password-123");
        return userId;
    }

    private async Task AddBriefingAsync(
        Guid userId, BriefingType type, string scopeKey, DateTimeOffset generatedAt,
        string body = "# briefing", string facts = "{}", string model = "test-model",
        int inputTokens = 100, int outputTokens = 50)
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var backgroundScope = sp.GetRequiredService<IBackgroundUserScope>();
        backgroundScope.SetUserId(userId);

        var repo = sp.GetRequiredService<IBriefingRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var briefing = Briefing.Create(userId, type, scopeKey, generatedAt, body, facts, model, inputTokens, outputTokens);
        await repo.AddAsync(briefing, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddAndGetById_RoundTrips()
    {
        var userId = await SeedUserAsync();
        var generatedAt = DateTimeOffset.UtcNow;

        Guid id;
        using (var scope = Factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
            var repo = sp.GetRequiredService<IBriefingRepository>();
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var b = Briefing.Create(userId, BriefingType.Morning, "morning:2026-04-14", generatedAt, "# body", "{}", "model", 10, 5);
            await repo.AddAsync(b, CancellationToken.None);
            await uow.SaveChangesAsync(CancellationToken.None);
            id = b.Id;
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
            var repo = sp.GetRequiredService<IBriefingRepository>();
            var fetched = await repo.GetByIdAsync(userId, id, CancellationToken.None);
            Assert.NotNull(fetched);
            Assert.Equal("morning:2026-04-14", fetched!.ScopeKey);
            Assert.Equal("# body", fetched.MarkdownBody);
        }
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsMostRecent()
    {
        var userId = await SeedUserAsync();
        var older = DateTimeOffset.UtcNow.AddHours(-3);
        var newer = DateTimeOffset.UtcNow;

        await AddBriefingAsync(userId, BriefingType.Morning, "morning:2026-04-14", older, body: "older");
        await AddBriefingAsync(userId, BriefingType.Morning, "morning:2026-04-14", newer, body: "newer");

        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
        var repo = sp.GetRequiredService<IBriefingRepository>();

        var latest = await repo.GetLatestAsync(userId, BriefingType.Morning, "morning:2026-04-14", CancellationToken.None);
        Assert.NotNull(latest);
        Assert.Equal("newer", latest!.MarkdownBody);
    }

    [Fact]
    public async Task GetByIdAsync_ForOtherUser_ReturnsNull()
    {
        var userA = await SeedUserAsync("user-a-briefing@test.invalid");
        var userB = await SeedUserAsync("user-b-briefing@test.invalid");

        Guid id;
        using (var scope = Factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userA);
            var repo = sp.GetRequiredService<IBriefingRepository>();
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var b = Briefing.Create(userA, BriefingType.Weekly, "weekly:2026-W16", DateTimeOffset.UtcNow, "# body", "{}", "model", 1, 1);
            await repo.AddAsync(b, CancellationToken.None);
            await uow.SaveChangesAsync(CancellationToken.None);
            id = b.Id;
        }

        // User B should not see User A's briefing.
        using var scopeB = Factory.Services.CreateScope();
        var spB = scopeB.ServiceProvider;
        spB.GetRequiredService<IBackgroundUserScope>().SetUserId(userB);
        var repoB = spB.GetRequiredService<IBriefingRepository>();
        var fetched = await repoB.GetByIdAsync(userB, id, CancellationToken.None);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task ListRecentAsync_FiltersByType_AndOrders()
    {
        var userId = await SeedUserAsync();
        var t = DateTimeOffset.UtcNow;
        await AddBriefingAsync(userId, BriefingType.Morning, "morning:2026-04-13", t.AddDays(-1), body: "m1");
        await AddBriefingAsync(userId, BriefingType.Weekly, "weekly:2026-W15", t.AddDays(-2), body: "w1");
        await AddBriefingAsync(userId, BriefingType.Morning, "morning:2026-04-14", t, body: "m2");

        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IBackgroundUserScope>().SetUserId(userId);
        var repo = sp.GetRequiredService<IBriefingRepository>();

        var allRecent = await repo.ListRecentAsync(userId, type: null, limit: 10, CancellationToken.None);
        Assert.Equal(3, allRecent.Count);
        // Newest first - the most recent briefing was the second Morning row.
        Assert.Equal("morning:2026-04-14", allRecent[0].ScopeKey);
        Assert.Equal(BriefingType.Morning, allRecent[0].Type);

        var morningOnly = await repo.ListRecentAsync(userId, BriefingType.Morning, limit: 10, CancellationToken.None);
        Assert.Equal(2, morningOnly.Count);
        Assert.All(morningOnly, b => Assert.Equal(BriefingType.Morning, b.Type));
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateOnSameTimestamp()
    {
        var userId = await SeedUserAsync();
        var t = DateTimeOffset.UtcNow;

        await AddBriefingAsync(userId, BriefingType.Morning, "morning:2026-04-14", t, body: "first");

        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(async () =>
        {
            await AddBriefingAsync(userId, BriefingType.Morning, "morning:2026-04-14", t, body: "duplicate");
        });
        Assert.NotNull(ex);
    }
}
