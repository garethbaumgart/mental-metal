namespace MentalMetal.Application.Briefings;

/// <summary>
/// Shared helper for ISO week-start (Monday) calculation used by brief
/// generation, caching, and cache invalidation.
/// </summary>
public static class WeekHelper
{
    /// <summary>
    /// Returns the Monday of the ISO week containing the given date.
    /// </summary>
    public static DateOnly GetWeekStart(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
        return date.AddDays(-dayOfWeek);
    }
}
