using MentalMetal.Domain.Nudges;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class NudgeRepository(MentalMetalDbContext dbContext) : INudgeRepository
{
    public async Task<Nudge?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Nudges.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Nudge>> GetAllAsync(
        Guid userId,
        bool? isActiveFilter,
        Guid? personIdFilter,
        Guid? initiativeIdFilter,
        DateOnly? dueBeforeFilter,
        int? dueWithinDaysFilter,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Nudges.AsNoTracking().Where(n => n.UserId == userId);

        if (isActiveFilter is not null)
            query = query.Where(n => n.IsActive == isActiveFilter.Value);

        if (personIdFilter is not null)
            query = query.Where(n => n.PersonId == personIdFilter.Value);

        if (initiativeIdFilter is not null)
            query = query.Where(n => n.InitiativeId == initiativeIdFilter.Value);

        if (dueBeforeFilter is not null)
        {
            var due = dueBeforeFilter.Value;
            query = query.Where(n => n.IsActive && n.NextDueDate != null && n.NextDueDate <= due);
        }

        if (dueWithinDaysFilter is not null)
        {
            var upper = today.AddDays(dueWithinDaysFilter.Value);
            query = query.Where(n => n.IsActive && n.NextDueDate != null && n.NextDueDate <= upper);
        }

        // Order: paused last, then by NextDueDate ascending, nulls last.
        return await query
            .OrderByDescending(n => n.IsActive)
            .ThenBy(n => n.NextDueDate == null ? 1 : 0)
            .ThenBy(n => n.NextDueDate)
            .ThenByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Nudge nudge, CancellationToken cancellationToken) =>
        await dbContext.Nudges.AddAsync(nudge, cancellationToken);

    public void Remove(Nudge nudge) => dbContext.Nudges.Remove(nudge);
}
