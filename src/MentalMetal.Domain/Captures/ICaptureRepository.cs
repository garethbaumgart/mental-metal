namespace MentalMetal.Domain.Captures;

public interface ICaptureRepository
{
    Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>
    /// Lists captures for the user. <paramref name="includeTriaged"/> defaults to <c>false</c>,
    /// so triaged captures (confirmed, discarded, or quick-discarded) are excluded unless the
    /// caller opts in.
    /// </summary>
    Task<IReadOnlyList<Capture>> GetAllAsync(
        Guid userId,
        CaptureType? typeFilter,
        ProcessingStatus? statusFilter,
        CancellationToken cancellationToken,
        bool includeTriaged = false);
    Task<IReadOnlyList<Capture>> GetConfirmedForInitiativeAsync(Guid userId, Guid initiativeId, int take, CancellationToken cancellationToken);

    /// <summary>
    /// Returns captures currently pending close-out triage for the user:
    /// <c>Triaged = false</c> and either <see cref="ProcessingStatus"/> is not <c>Processed</c>,
    /// or the processed capture's extraction has not yet been confirmed or discarded.
    /// </summary>
    Task<IReadOnlyList<Capture>> GetCloseOutQueueAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(Capture capture, CancellationToken cancellationToken);
}
