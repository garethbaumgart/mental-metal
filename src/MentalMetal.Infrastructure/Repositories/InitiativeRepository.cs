using MentalMetal.Domain.Initiatives;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class InitiativeRepository(MentalMetalDbContext dbContext) : IInitiativeRepository
{
    public async Task<Initiative?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Initiatives
            .Include(i => i.Milestones)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Initiative>> GetByIdsAsync(
        Guid userId, IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        // Use List<T>.Contains (translatable by EF) rather than HashSet.Contains.
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return [];

        return await dbContext.Initiatives
            .AsNoTracking()
            .Where(i => i.UserId == userId && idList.Contains(i.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Initiative>> GetAllAsync(
        Guid userId, InitiativeStatus? statusFilter, CancellationToken cancellationToken)
    {
        var query = dbContext.Initiatives
            .Include(i => i.Milestones)
            .Where(i => i.UserId == userId);

        if (statusFilter is not null)
            query = query.Where(i => i.Status == statusFilter.Value);

        return await query.OrderBy(i => i.Title).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Initiative initiative, CancellationToken cancellationToken) =>
        await dbContext.Initiatives.AddAsync(initiative, cancellationToken);
}
