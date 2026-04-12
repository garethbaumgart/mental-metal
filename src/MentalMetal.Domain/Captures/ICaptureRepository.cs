namespace MentalMetal.Domain.Captures;

public interface ICaptureRepository
{
    Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Capture>> GetAllAsync(Guid userId, CaptureType? typeFilter, ProcessingStatus? statusFilter, CancellationToken cancellationToken);
    Task AddAsync(Capture capture, CancellationToken cancellationToken);
}
