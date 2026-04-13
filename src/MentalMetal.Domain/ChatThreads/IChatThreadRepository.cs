namespace MentalMetal.Domain.ChatThreads;

public interface IChatThreadRepository
{
    Task<ChatThread?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatThread>> ListForInitiativeAsync(
        Guid userId,
        Guid initiativeId,
        ChatThreadStatus? statusFilter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatThread>> ListGlobalAsync(
        Guid userId,
        ChatThreadStatus? statusFilter,
        CancellationToken cancellationToken);

    Task AddAsync(ChatThread thread, CancellationToken cancellationToken);
}
