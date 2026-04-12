namespace MentalMetal.Domain.Delegations;

public interface IDelegationRepository
{
    Task<Delegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Delegation>> GetAllAsync(
        Guid userId,
        DelegationStatus? statusFilter,
        Priority? priorityFilter,
        Guid? delegatePersonIdFilter,
        Guid? initiativeIdFilter,
        CancellationToken cancellationToken);
    Task AddAsync(Delegation delegation, CancellationToken cancellationToken);
}
