namespace MentalMetal.Domain.Initiatives;

public interface IInitiativeRepository
{
    Task<Initiative?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Initiative>> GetByIdsAsync(Guid userId, IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<Initiative>> GetAllAsync(Guid userId, InitiativeStatus? statusFilter, CancellationToken cancellationToken);
    Task AddAsync(Initiative initiative, CancellationToken cancellationToken);
}
