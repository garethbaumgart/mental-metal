namespace MentalMetal.Domain.Observations;

public interface IObservationRepository
{
    Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Observation>> GetAllAsync(
        Guid userId,
        Guid? personIdFilter,
        ObservationTag? tagFilter,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken);
    Task AddAsync(Observation observation, CancellationToken cancellationToken);
    void Remove(Observation observation);
}
