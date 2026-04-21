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
    /// Invalidates all cached briefs for a user. Call when new captures are
    /// created or processed so the next request regenerates fresh data.
    /// </summary>
    void InvalidateForUser(Guid userId);
}
