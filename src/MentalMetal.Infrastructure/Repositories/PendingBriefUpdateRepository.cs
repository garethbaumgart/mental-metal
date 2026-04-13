using MentalMetal.Domain.Initiatives.LivingBrief;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class PendingBriefUpdateRepository(MentalMetalDbContext dbContext) : IPendingBriefUpdateRepository
{
    public Task<PendingBriefUpdate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.PendingBriefUpdates.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PendingBriefUpdate>> ListForInitiativeAsync(
        Guid userId,
        Guid initiativeId,
        PendingBriefUpdateStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PendingBriefUpdates
            .Where(p => p.UserId == userId && p.InitiativeId == initiativeId);

        if (statusFilter is not null)
            query = query.Where(p => p.Status == statusFilter.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PendingBriefUpdate update, CancellationToken cancellationToken) =>
        await dbContext.PendingBriefUpdates.AddAsync(update, cancellationToken);
}
