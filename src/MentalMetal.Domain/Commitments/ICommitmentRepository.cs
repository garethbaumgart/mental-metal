namespace MentalMetal.Domain.Commitments;

public interface ICommitmentRepository
{
    Task<Commitment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Commitment>> GetAllAsync(
        Guid userId,
        CommitmentDirection? directionFilter,
        CommitmentStatus? statusFilter,
        Guid? personIdFilter,
        Guid? initiativeIdFilter,
        bool? overdueFilter,
        CancellationToken cancellationToken);
    Task AddAsync(Commitment commitment, CancellationToken cancellationToken);
}
