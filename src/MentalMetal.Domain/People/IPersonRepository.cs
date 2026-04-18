namespace MentalMetal.Domain.People;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Person>> GetByIdsAsync(Guid userId, IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<Person>> GetAllAsync(Guid userId, PersonType? typeFilter, bool includeArchived, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludeId, CancellationToken cancellationToken);
    Task<bool> AliasExistsForOtherPersonAsync(Guid userId, string alias, Guid excludePersonId, CancellationToken cancellationToken);
    Task AddAsync(Person person, CancellationToken cancellationToken);
}
