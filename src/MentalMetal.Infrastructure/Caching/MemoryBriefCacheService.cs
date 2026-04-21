using MentalMetal.Application.Briefings;
using Microsoft.Extensions.Caching.Memory;

namespace MentalMetal.Infrastructure.Caching;

public sealed class MemoryBriefCacheService(IMemoryCache cache) : IBriefCacheService
{
    private static readonly TimeSpan DailyBriefTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan WeeklyBriefTtl = TimeSpan.FromHours(4);

    public DailyBriefResponse? GetDailyBrief(Guid userId)
    {
        var key = DailyKey(userId);
        return cache.TryGetValue(key, out DailyBriefResponse? brief) ? brief : null;
    }

    public void SetDailyBrief(Guid userId, DailyBriefResponse brief) =>
        cache.Set(DailyKey(userId), brief, DailyBriefTtl);

    public WeeklyBriefResponse? GetWeeklyBrief(Guid userId, DateOnly weekStart)
    {
        var key = WeeklyKey(userId, weekStart);
        return cache.TryGetValue(key, out WeeklyBriefResponse? brief) ? brief : null;
    }

    public void SetWeeklyBrief(Guid userId, DateOnly weekStart, WeeklyBriefResponse brief) =>
        cache.Set(WeeklyKey(userId, weekStart), brief, WeeklyBriefTtl);

    public void InvalidateForUser(Guid userId)
    {
        // Remove daily brief (keyed by UTC date so it naturally rolls over at midnight)
        cache.Remove(DailyKey(userId));

        // Remove weekly briefs for the current week and the 3 prior weeks.
        // Older weeks are left to expire via TTL (4 hours). This covers the
        // realistic window where a newly-processed capture would affect a brief.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 4; i++)
        {
            var weekStart = WeekHelper.GetWeekStart(today.AddDays(-7 * i));
            cache.Remove(WeeklyKey(userId, weekStart));
        }
    }

    private static string DailyKey(Guid userId) =>
        $"brief:daily:{userId}:{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}";

    private static string WeeklyKey(Guid userId, DateOnly weekStart) =>
        $"brief:weekly:{userId}:{weekStart:yyyy-MM-dd}";
}
