using MentalMetal.Domain.Interviews;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class InterviewRepository(MentalMetalDbContext dbContext) : IInterviewRepository
{
    public async Task<Interview?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Interviews
            .AsSplitQuery()
            .Include(i => i.Scorecards)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Interview>> GetAllAsync(
        Guid userId,
        Guid? candidatePersonIdFilter,
        InterviewStage? stageFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Interviews
            .AsNoTracking()
            .Include(i => i.Scorecards)
            .Where(i => i.UserId == userId);

        if (candidatePersonIdFilter is not null)
            query = query.Where(i => i.CandidatePersonId == candidatePersonIdFilter.Value);

        if (stageFilter is not null)
            query = query.Where(i => i.Stage == stageFilter.Value);

        return await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Interview interview, CancellationToken cancellationToken) =>
        await dbContext.Interviews.AddAsync(interview, cancellationToken);

    public void Remove(Interview interview) =>
        dbContext.Interviews.Remove(interview);

    public void MarkOwnedAdded(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Added;

    public void MarkOwnedRemoved(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Deleted;
}
