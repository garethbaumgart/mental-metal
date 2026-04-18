namespace MentalMetal.Domain.Captures;

public interface ICaptureRepository
{
    Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Same as <see cref="GetByIdAsync"/> but eagerly loads the owned
    /// <c>TranscriptSegments</c> collection. Use only from audio-capture paths.
    /// </summary>
    Task<Capture?> GetByIdWithTranscriptAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Capture>> GetAllAsync(
        Guid userId,
        CaptureType? typeFilter,
        ProcessingStatus? statusFilter,
        CancellationToken cancellationToken);

    Task AddAsync(Capture capture, CancellationToken cancellationToken);

    /// <summary>
    /// EF Core's snapshot change detection for field-backed owned collections does not always
    /// recognise newly-appended items as Added, so handlers must call this helper immediately
    /// after mutating the collection on a tracked aggregate.
    /// </summary>
    void MarkOwnedAdded(object ownedEntity);
    void MarkOwnedRemoved(object ownedEntity);
}
