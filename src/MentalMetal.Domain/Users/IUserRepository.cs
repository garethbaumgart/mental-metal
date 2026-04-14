namespace MentalMetal.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByExternalAuthIdAsync(string externalAuthId, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// EF Core's snapshot change detection for field-backed owned collections does not always
    /// recognise newly-appended items as Added, so handlers must call this helper immediately
    /// after mutating the collection on a tracked aggregate.
    /// </summary>
    void MarkOwnedAdded(object ownedEntity);
    void MarkOwnedRemoved(object ownedEntity);
}
