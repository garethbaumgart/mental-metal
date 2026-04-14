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
    /// Returns up to <paramref name="limit"/> most-recent briefing summaries for the
    /// user, optionally filtered by type. Sorted by GeneratedAtUtc descending. The
    /// projection deliberately omits MarkdownBody and PromptFactsJson - both are
    /// large columns and the only caller (recent-list endpoint) does not need them.
    /// </summary>
    Task<IReadOnlyList<BriefingListItem>> ListRecentAsync(
        Guid userId,
        BriefingType? type,
        int limit,
        CancellationToken cancellationToken);
}

/// <summary>
/// Read-side projection of a Briefing without the heavy body/facts columns.
/// </summary>
public sealed record BriefingListItem(
    Guid Id,
    Guid UserId,
    BriefingType Type,
    string ScopeKey,
    DateTimeOffset GeneratedAtUtc,
    string Model,
    int InputTokens,
    int OutputTokens);
