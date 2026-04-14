namespace MentalMetal.Domain.Goals;

public interface IGoalRepository
{
    Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Goal>> GetAllAsync(
        Guid userId,
        Guid? personIdFilter,
        GoalType? typeFilter,
        GoalStatus? statusFilter,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken);
    Task AddAsync(Goal goal, CancellationToken cancellationToken);

    /// <summary>
    /// EF's snapshot tracker sometimes fails to detect newly-appended owned entities as Added;
    /// handlers that mutate the CheckIns collection on a tracked aggregate must call this.
    /// </summary>
    void MarkOwnedAdded(object ownedEntity);
}
