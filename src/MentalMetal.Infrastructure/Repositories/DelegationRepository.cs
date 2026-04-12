using MentalMetal.Domain.Delegations;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class DelegationRepository(MentalMetalDbContext dbContext) : IDelegationRepository
{
    public async Task<Delegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Delegations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Delegation>> GetAllAsync(
        Guid userId,
        DelegationStatus? statusFilter,
        Priority? priorityFilter,
        Guid? delegatePersonIdFilter,
        Guid? initiativeIdFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Delegations.AsNoTracking().Where(d => d.UserId == userId);

        if (statusFilter is not null)
            query = query.Where(d => d.Status == statusFilter.Value);

        if (priorityFilter is not null)
            query = query.Where(d => d.Priority == priorityFilter.Value);

        if (delegatePersonIdFilter is not null)
            query = query.Where(d => d.DelegatePersonId == delegatePersonIdFilter.Value);

        if (initiativeIdFilter is not null)
            query = query.Where(d => d.InitiativeId == initiativeIdFilter.Value);

        return await query
            .OrderByDescending(d =>
                d.Priority == Priority.Urgent ? 3 :
                d.Priority == Priority.High ? 2 :
                d.Priority == Priority.Medium ? 1 : 0)
            .ThenBy(d => d.DueDate == null ? 1 : 0)
            .ThenBy(d => d.DueDate)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Delegation delegation, CancellationToken cancellationToken) =>
        await dbContext.Delegations.AddAsync(delegation, cancellationToken);
}
