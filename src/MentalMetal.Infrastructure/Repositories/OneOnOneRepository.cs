using MentalMetal.Domain.OneOnOnes;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class OneOnOneRepository(MentalMetalDbContext dbContext) : IOneOnOneRepository
{
    public async Task<OneOnOne?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.OneOnOnes
            .AsSplitQuery()
            .Include(o => o.ActionItems)
            .Include(o => o.FollowUps)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OneOnOne>> GetAllAsync(
        Guid userId,
        Guid? personIdFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.OneOnOnes
            .AsNoTracking()
            .Include(o => o.ActionItems)
            .Include(o => o.FollowUps)
            .Where(o => o.UserId == userId);

        if (personIdFilter is not null)
            query = query.Where(o => o.PersonId == personIdFilter.Value);

        return await query
            .OrderByDescending(o => o.OccurredAt)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OneOnOne oneOnOne, CancellationToken cancellationToken) =>
        await dbContext.OneOnOnes.AddAsync(oneOnOne, cancellationToken);

    public void MarkOwnedAdded(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Added;

    public void MarkOwnedRemoved(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Deleted;
}
