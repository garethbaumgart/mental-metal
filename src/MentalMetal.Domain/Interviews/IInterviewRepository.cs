namespace MentalMetal.Domain.Interviews;

public interface IInterviewRepository
{
    Task<Interview?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Interview>> GetAllAsync(
        Guid userId,
        Guid? candidatePersonIdFilter,
        InterviewStage? stageFilter,
        CancellationToken cancellationToken);

    Task AddAsync(Interview interview, CancellationToken cancellationToken);

    void Remove(Interview interview);

    /// <summary>
    /// EF Core's snapshot change detection for field-backed owned collections does not always
    /// recognise newly-appended items as Added, so handlers must call this helper immediately
    /// after mutating the collection on a tracked aggregate. Mirrors the pattern used by
    /// <c>OneOnOneRepository</c>.
    /// </summary>
    void MarkOwnedAdded(object ownedEntity);

    void MarkOwnedRemoved(object ownedEntity);
}
