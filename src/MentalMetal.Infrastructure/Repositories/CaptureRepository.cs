using MentalMetal.Domain.Captures;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class CaptureRepository(MentalMetalDbContext dbContext) : ICaptureRepository
{
    public async Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Captures.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Capture?> GetByIdWithTranscriptAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Captures
            .Include(c => c.TranscriptSegments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Capture>> GetAllAsync(
        Guid userId,
        CaptureType? typeFilter,
        ProcessingStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Captures.AsNoTracking().Where(c => c.UserId == userId);

        if (typeFilter is not null)
            query = query.Where(c => c.CaptureType == typeFilter.Value);

        if (statusFilter is not null)
            query = query.Where(c => c.ProcessingStatus == statusFilter.Value);

        return await query.OrderByDescending(c => c.CapturedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Capture>> GetByDateRangeAsync(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to,
        ProcessingStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Captures.AsNoTracking()
            .Where(c => c.UserId == userId && c.CapturedAt >= from && c.CapturedAt < to);

        if (statusFilter is not null)
            query = query.Where(c => c.ProcessingStatus == statusFilter.Value);

        return await query.OrderByDescending(c => c.CapturedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Capture capture, CancellationToken cancellationToken) =>
        await dbContext.Captures.AddAsync(capture, cancellationToken);

    public void MarkOwnedAdded(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Added;

    public void MarkOwnedRemoved(object ownedEntity) =>
        dbContext.Entry(ownedEntity).State = EntityState.Deleted;
}
