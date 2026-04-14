using MentalMetal.Domain.Goals;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class GoalRepository(MentalMetalDbContext dbContext) : IGoalRepository
{
    public async Task<Goal?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Goals
            .Include(g => g.CheckIns)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Goal>> GetAllAsync(
        Guid userId,
        Guid? personIdFilter,
        GoalType? typeFilter,
        GoalStatus? statusFilter,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Goals
            .AsNoTracking()
            .Include(g => g.CheckIns)
            .Where(g => g.UserId == userId);

        if (personIdFilter is not null)
            query = query.Where(g => g.PersonId == personIdFilter.Value);

        if (typeFilter is not null)
            query = query.Where(g => g.Type == typeFilter.Value);

        if (statusFilter is not null)
            query = query.Where(g => g.Status == statusFilter.Value);

        if (fromDate is not null)
            query = query.Where(g => g.CreatedAt >= fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        if (toDate is not null)
            query = query.Where(g => g.CreatedAt <= toDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        return await query
            // Active goals first (enum value 0), then by CreatedAt desc
            .OrderBy(g => g.Status == GoalStatus.Active ? 0 : 1)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Goal goal, CancellationToken cancellationToken) =>
        await dbContext.Goals.AddAsync(goal, cancellationToken);

    public void MarkOwnedAdded(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Added;
}
