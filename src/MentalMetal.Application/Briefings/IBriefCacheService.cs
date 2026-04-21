namespace MentalMetal.Application.Briefings;

/// <summary>
/// Caches generated briefs to avoid repeated LLM calls on every page load.
/// </summary>
public interface IBriefCacheService
{
    DailyBriefResponse? GetDailyBrief(Guid userId);
    void SetDailyBrief(Guid userId, DailyBriefResponse brief);

    WeeklyBriefResponse? GetWeeklyBrief(Guid userId, DateOnly weekStart);
    void SetWeeklyBrief(Guid userId, DateOnly weekStart, WeeklyBriefResponse brief);

    /// <summary>
    /// Invalidates the cached daily brief and recent weekly briefs (current
    /// week plus 3 prior weeks) for a user. Older weekly briefs are left to
    /// expire via TTL. Call when captures are processed so the next request
    /// regenerates fresh data.
    /// </summary>
    void InvalidateForUser(Guid userId);
}
