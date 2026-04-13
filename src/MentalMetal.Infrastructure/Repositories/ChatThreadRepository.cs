using MentalMetal.Domain.ChatThreads;
using MentalMetal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MentalMetal.Infrastructure.Repositories;

public sealed class ChatThreadRepository(MentalMetalDbContext dbContext) : IChatThreadRepository
{
    public async Task<ChatThread?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.ChatThreads.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ChatThread>> ListForInitiativeAsync(
        Guid userId,
        Guid initiativeId,
        ChatThreadStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ChatThreads
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.Scope.Type == ContextScopeType.Initiative
                && t.Scope.InitiativeId == initiativeId);

        if (statusFilter is not null)
            query = query.Where(t => t.Status == statusFilter.Value);

        // LastMessageAt DESC with NULLs last, then CreatedAt DESC.
        return await query
            .OrderBy(t => t.LastMessageAt == null ? 1 : 0)
            .ThenByDescending(t => t.LastMessageAt)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatThread>> ListGlobalAsync(
        Guid userId,
        ChatThreadStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ChatThreads
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.Scope.Type == ContextScopeType.Global);

        if (statusFilter is not null)
            query = query.Where(t => t.Status == statusFilter.Value);

        // LastMessageAt DESC with NULLs last, then CreatedAt DESC.
        return await query
            .OrderBy(t => t.LastMessageAt == null ? 1 : 0)
            .ThenByDescending(t => t.LastMessageAt)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ChatThread thread, CancellationToken cancellationToken) =>
        await dbContext.ChatThreads.AddAsync(thread, cancellationToken);
}
