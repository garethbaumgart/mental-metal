using MentalMetal.Application.Briefings;
using Microsoft.Extensions.Caching.Memory;

namespace MentalMetal.Infrastructure.Caching;

/// <summary>
/// In-memory cache for generated briefs. Avoids repeated LLM calls on every page load.
///
/// <para><b>Race condition note:</b> A brief generation that started before
/// <see cref="InvalidateForUser"/> may repopulate the cache with stale data via
/// <see cref="SetDailyBrief"/> or <see cref="SetWeeklyBrief"/> after invalidation completes.
/// This is an acceptable trade-off for a non-critical cache with short TTL (1h daily / 4h weekly).
/// A distributed lock or generation-stamp would add complexity disproportionate to the risk.</para>
/// </summary>
public sealed class MemoryBriefCacheService(IMemoryCache cache, TimeProvider timeProvider) : IBriefCacheService
{
    private static readonly TimeSpan DailyBriefTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan WeeklyBriefTtl = TimeSpan.FromHours(4);

    public DailyBriefResponse? GetDailyBrief(Guid userId)
    {
        var key = DailyKey(userId);
        return cache.TryGetValue(key, out DailyBriefResponse? brief) ? brief : null;
    }

    public void SetDailyBrief(Guid userId, DailyBriefResponse brief) =>
        cache.Set(DailyKey(userId), brief, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DailyBriefTtl,
            Size = 1
        });

    public WeeklyBriefResponse? GetWeeklyBrief(Guid userId, DateOnly weekStart)
    {
        var key = WeeklyKey(userId, weekStart);
        return cache.TryGetValue(key, out WeeklyBriefResponse? brief) ? brief : null;
    }

    public void SetWeeklyBrief(Guid userId, DateOnly weekStart, WeeklyBriefResponse brief) =>
        cache.Set(WeeklyKey(userId, weekStart), brief, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = WeeklyBriefTtl,
            Size = 1
        });

    public void InvalidateForUser(Guid userId)
    {
        // Remove daily brief (keyed by UTC date so it naturally rolls over at midnight)
        cache.Remove(DailyKey(userId));

        // Remove weekly briefs for the current week and the 3 prior weeks.
        // Older weeks are left to expire via TTL (4 hours). This covers the
        // realistic window where a newly-processed capture would affect a brief.
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        for (var i = 0; i < 4; i++)
        {
            var weekStart = WeekHelper.GetWeekStart(today.AddDays(-7 * i));
            cache.Remove(WeeklyKey(userId, weekStart));
        }
    }

    private string DailyKey(Guid userId) =>
        $"brief:daily:{userId}:{DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime):yyyy-MM-dd}";

    private static string WeeklyKey(Guid userId, DateOnly weekStart) =>
        $"brief:weekly:{userId}:{weekStart:yyyy-MM-dd}";
}
