using MentalMetal.Domain.Briefings;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class BriefingRepository(MentalMetalDbContext dbContext) : IBriefingRepository
{
    public async Task AddAsync(Briefing briefing, CancellationToken cancellationToken) =>
        await dbContext.Briefings.AddAsync(briefing, cancellationToken);

    public async Task<Briefing?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        // The global query filter already constrains to the current user's UserId; the
        // explicit predicate is defence-in-depth against handlers passing in an alien id.
        await dbContext.Briefings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);

    public async Task<Briefing?> GetLatestAsync(
        Guid userId, BriefingType type, string scopeKey, CancellationToken cancellationToken) =>
        await dbContext.Briefings
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Type == type && b.ScopeKey == scopeKey)
            .OrderByDescending(b => b.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Briefing>> ListRecentAsync(
        Guid userId, BriefingType? type, int limit, CancellationToken cancellationToken)
    {
        var query = dbContext.Briefings
            .AsNoTracking()
            .Where(b => b.UserId == userId);

        if (type is not null)
            query = query.Where(b => b.Type == type.Value);

        return await query
            .OrderByDescending(b => b.GeneratedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
