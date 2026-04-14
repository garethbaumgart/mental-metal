namespace MentalMetal.Domain.Nudges;

public interface INudgeRepository
{
    Task<Nudge?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Nudge>> GetAllAsync(
        Guid userId,
        bool? isActiveFilter,
        Guid? personIdFilter,
        Guid? initiativeIdFilter,
        DateOnly? dueBeforeFilter,
        int? dueWithinDaysFilter,
        DateOnly today,
        CancellationToken cancellationToken);

    Task AddAsync(Nudge nudge, CancellationToken cancellationToken);

    void Remove(Nudge nudge);
}
