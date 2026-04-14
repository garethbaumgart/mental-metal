using MentalMetal.Domain.Observations;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class ObservationRepository(MentalMetalDbContext dbContext) : IObservationRepository
{
    public async Task<Observation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Observations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Observation>> GetAllAsync(
        Guid userId,
        Guid? personIdFilter,
        ObservationTag? tagFilter,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Observations.AsNoTracking().Where(o => o.UserId == userId);

        if (personIdFilter is not null)
            query = query.Where(o => o.PersonId == personIdFilter.Value);

        if (tagFilter is not null)
            query = query.Where(o => o.Tag == tagFilter.Value);

        if (fromDate is not null)
            query = query.Where(o => o.OccurredAt >= fromDate.Value);

        if (toDate is not null)
            query = query.Where(o => o.OccurredAt <= toDate.Value);

        return await query
            .OrderByDescending(o => o.OccurredAt)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Observation observation, CancellationToken cancellationToken) =>
        await dbContext.Observations.AddAsync(observation, cancellationToken);

    public void Remove(Observation observation) =>
        dbContext.Observations.Remove(observation);
}
