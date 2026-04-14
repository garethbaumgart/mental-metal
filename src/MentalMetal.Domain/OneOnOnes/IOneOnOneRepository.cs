namespace MentalMetal.Domain.OneOnOnes;

public interface IOneOnOneRepository
{
    Task<OneOnOne?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OneOnOne>> GetAllAsync(
        Guid userId,
        Guid? personIdFilter,
        CancellationToken cancellationToken);
    Task AddAsync(OneOnOne oneOnOne, CancellationToken cancellationToken);

    /// <summary>
    /// EF Core's snapshot change detection for field-backed owned collections does not always
    /// recognise newly-appended items as Added, so handlers must call this helper immediately
    /// after mutating the collection on a tracked aggregate.
    /// </summary>
    void MarkOwnedAdded(object ownedEntity);
    void MarkOwnedRemoved(object ownedEntity);
}
