using MentalMetal.Application.Briefings;
using MentalMetal.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace MentalMetal.Application.Tests.Briefings;

public class MemoryBriefCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly MemoryBriefCacheService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public MemoryBriefCacheServiceTests()
    {
        _sut = new MemoryBriefCacheService(_memoryCache);
    }

    public void Dispose() => _memoryCache.Dispose();

    [Fact]
    public void GetDailyBrief_ReturnsNull_WhenNotCached()
    {
        var result = _sut.GetDailyBrief(_userId);
        Assert.Null(result);
    }

    [Fact]
    public void SetDailyBrief_ThenGet_ReturnsCachedValue()
    {
        var brief = MakeDailyBrief("Test narrative");
        _sut.SetDailyBrief(_userId, brief);

        var result = _sut.GetDailyBrief(_userId);

        Assert.Same(brief, result);
    }

    [Fact]
    public void DailyBrief_IsolatedPerUser()
    {
        var userId2 = Guid.NewGuid();
        var brief1 = MakeDailyBrief("User 1");
        var brief2 = MakeDailyBrief("User 2");

        _sut.SetDailyBrief(_userId, brief1);
        _sut.SetDailyBrief(userId2, brief2);

        Assert.Same(brief1, _sut.GetDailyBrief(_userId));
        Assert.Same(brief2, _sut.GetDailyBrief(userId2));
    }

    [Fact]
    public void GetWeeklyBrief_ReturnsNull_WhenNotCached()
    {
        var result = _sut.GetWeeklyBrief(_userId, new DateOnly(2026, 4, 20));
        Assert.Null(result);
    }

    [Fact]
    public void SetWeeklyBrief_ThenGet_ReturnsCachedValue()
    {
        var weekStart = new DateOnly(2026, 4, 20);
        var brief = MakeWeeklyBrief("Weekly narrative");
        _sut.SetWeeklyBrief(_userId, weekStart, brief);

        var result = _sut.GetWeeklyBrief(_userId, weekStart);

        Assert.Same(brief, result);
    }

    [Fact]
    public void WeeklyBrief_DifferentWeeks_AreSeparate()
    {
        var week1 = new DateOnly(2026, 4, 13);
        var week2 = new DateOnly(2026, 4, 20);
        var brief1 = MakeWeeklyBrief("Week 1");
        var brief2 = MakeWeeklyBrief("Week 2");

        _sut.SetWeeklyBrief(_userId, week1, brief1);
        _sut.SetWeeklyBrief(_userId, week2, brief2);

        Assert.Same(brief1, _sut.GetWeeklyBrief(_userId, week1));
        Assert.Same(brief2, _sut.GetWeeklyBrief(_userId, week2));
    }

    [Fact]
    public void InvalidateForUser_RemovesDailyBrief()
    {
        var brief = MakeDailyBrief("Test");
        _sut.SetDailyBrief(_userId, brief);

        _sut.InvalidateForUser(_userId);

        Assert.Null(_sut.GetDailyBrief(_userId));
    }

    [Fact]
    public void InvalidateForUser_RemovesCurrentWeekBrief()
    {
        var currentWeekStart = WeekHelper.GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var brief = MakeWeeklyBrief("This week");
        _sut.SetWeeklyBrief(_userId, currentWeekStart, brief);

        _sut.InvalidateForUser(_userId);

        Assert.Null(_sut.GetWeeklyBrief(_userId, currentWeekStart));
    }

    [Fact]
    public void InvalidateForUser_RemovesPriorWeekBriefs()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentWeek = WeekHelper.GetWeekStart(today);
        var priorWeek = WeekHelper.GetWeekStart(today.AddDays(-7));
        var twoWeeksAgo = WeekHelper.GetWeekStart(today.AddDays(-14));

        _sut.SetWeeklyBrief(_userId, currentWeek, MakeWeeklyBrief("Current"));
        _sut.SetWeeklyBrief(_userId, priorWeek, MakeWeeklyBrief("Prior"));
        _sut.SetWeeklyBrief(_userId, twoWeeksAgo, MakeWeeklyBrief("Two weeks ago"));

        _sut.InvalidateForUser(_userId);

        Assert.Null(_sut.GetWeeklyBrief(_userId, currentWeek));
        Assert.Null(_sut.GetWeeklyBrief(_userId, priorWeek));
        Assert.Null(_sut.GetWeeklyBrief(_userId, twoWeeksAgo));
    }

    [Fact]
    public void InvalidateForUser_DoesNotAffectOtherUsers()
    {
        var userId2 = Guid.NewGuid();
        var brief1 = MakeDailyBrief("User 1");
        var brief2 = MakeDailyBrief("User 2");

        _sut.SetDailyBrief(_userId, brief1);
        _sut.SetDailyBrief(userId2, brief2);

        _sut.InvalidateForUser(_userId);

        Assert.Null(_sut.GetDailyBrief(_userId));
        Assert.Same(brief2, _sut.GetDailyBrief(userId2));
    }

    [Fact]
    public void InvalidateForUser_DoesNotAffectOtherUsersWeeklyBriefs()
    {
        var userId2 = Guid.NewGuid();
        var weekStart = WeekHelper.GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var brief1 = MakeWeeklyBrief("User 1 weekly");
        var brief2 = MakeWeeklyBrief("User 2 weekly");

        _sut.SetWeeklyBrief(_userId, weekStart, brief1);
        _sut.SetWeeklyBrief(userId2, weekStart, brief2);

        _sut.InvalidateForUser(_userId);

        Assert.Null(_sut.GetWeeklyBrief(_userId, weekStart));
        Assert.Same(brief2, _sut.GetWeeklyBrief(userId2, weekStart));
    }

    private static DailyBriefResponse MakeDailyBrief(string narrative) =>
        new(narrative, [], [], [], [], 0, DateTimeOffset.UtcNow);

    private static WeeklyBriefResponse MakeWeeklyBrief(string narrative) =>
        new(narrative, [], [], new CommitmentStatusSummary(0, 0, 0, 0),
            [], [], new DateRange(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow);
}
