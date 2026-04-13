namespace MentalMetal.Domain.Initiatives.LivingBrief;

public interface IPendingBriefUpdateRepository
{
    Task<PendingBriefUpdate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingBriefUpdate>> ListForInitiativeAsync(
        Guid userId,
        Guid initiativeId,
        PendingBriefUpdateStatus? statusFilter,
        CancellationToken cancellationToken);

    Task AddAsync(PendingBriefUpdate update, CancellationToken cancellationToken);
}
