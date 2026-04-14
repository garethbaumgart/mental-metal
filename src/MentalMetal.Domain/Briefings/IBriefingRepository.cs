namespace MentalMetal.Domain.Briefings;

public interface IBriefingRepository
{
    Task AddAsync(Briefing briefing, CancellationToken cancellationToken);

    Task<Briefing?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the most-recent briefing (by GeneratedAtUtc) matching the given
    /// (UserId, Type, ScopeKey) tuple, or null if none exists.
    /// </summary>
    Task<Briefing?> GetLatestAsync(
        Guid userId,
        BriefingType type,
        string scopeKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns up to <paramref name="limit"/> most-recent briefings for the user,
    /// optionally filtered by type. Sorted by GeneratedAtUtc descending.
    /// </summary>
    Task<IReadOnlyList<Briefing>> ListRecentAsync(
        Guid userId,
        BriefingType? type,
        int limit,
        CancellationToken cancellationToken);
}
