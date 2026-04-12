using MentalMetal.Domain.Commitments;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class CommitmentRepository(MentalMetalDbContext dbContext) : ICommitmentRepository
{
    public async Task<Commitment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Commitments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Commitment>> GetAllAsync(
        Guid userId,
        CommitmentDirection? directionFilter,
        CommitmentStatus? statusFilter,
        Guid? personIdFilter,
        Guid? initiativeIdFilter,
        bool? overdueFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Commitments.AsNoTracking().Where(c => c.UserId == userId);

        if (directionFilter is not null)
            query = query.Where(c => c.Direction == directionFilter.Value);

        if (statusFilter is not null)
            query = query.Where(c => c.Status == statusFilter.Value);

        if (personIdFilter is not null)
            query = query.Where(c => c.PersonId == personIdFilter.Value);

        if (initiativeIdFilter is not null)
            query = query.Where(c => c.InitiativeId == initiativeIdFilter.Value);

        if (overdueFilter == true)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(c => c.Status == CommitmentStatus.Open && c.DueDate != null && c.DueDate < today);
        }

        return await query
            .OrderBy(c => c.DueDate == null ? 1 : 0)
            .ThenBy(c => c.DueDate)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Commitment commitment, CancellationToken cancellationToken) =>
        await dbContext.Commitments.AddAsync(commitment, cancellationToken);
}
