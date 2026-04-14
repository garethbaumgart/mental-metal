using MentalMetal.Domain.Captures;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class CaptureRepository(MentalMetalDbContext dbContext) : ICaptureRepository
{
    public async Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Captures.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Capture>> GetAllAsync(
        Guid userId,
        CaptureType? typeFilter,
        ProcessingStatus? statusFilter,
        CancellationToken cancellationToken,
        bool includeTriaged = false)
    {
        var query = dbContext.Captures.AsNoTracking().Where(c => c.UserId == userId);

        if (!includeTriaged)
            query = query.Where(c => !c.Triaged);

        if (typeFilter is not null)
            query = query.Where(c => c.CaptureType == typeFilter.Value);

        if (statusFilter is not null)
            query = query.Where(c => c.ProcessingStatus == statusFilter.Value);

        return await query.OrderByDescending(c => c.CapturedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Capture>> GetConfirmedForInitiativeAsync(
        Guid userId, Guid initiativeId, int take, CancellationToken cancellationToken)
    {
        // Filter at the DB level using Postgres array containment on LinkedInitiativeIds
        // and the Confirmed extraction status, ordered by most recent capture.
        return await dbContext.Captures
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Where(c => c.ExtractionStatus == ExtractionStatus.Confirmed)
            .Where(c => c.LinkedInitiativeIds.Contains(initiativeId))
            .OrderByDescending(c => c.CapturedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Capture>> GetCloseOutQueueAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        // Using List<T>.Contains (not HashSet) because EF Core Npgsql translates that to an
        // IN-clause but does not translate HashSet.Contains. Likewise we avoid ToLowerInvariant().
        var unprocessedStatuses = new List<ProcessingStatus>
        {
            ProcessingStatus.Raw,
            ProcessingStatus.Processing,
            ProcessingStatus.Failed,
        };

        return await dbContext.Captures
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Where(c => !c.Triaged)
            .Where(c =>
                unprocessedStatuses.Contains(c.ProcessingStatus) ||
                (c.ProcessingStatus == ProcessingStatus.Processed && !c.ExtractionResolved))
            .OrderByDescending(c => c.CapturedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Capture capture, CancellationToken cancellationToken) =>
        await dbContext.Captures.AddAsync(capture, cancellationToken);
}
