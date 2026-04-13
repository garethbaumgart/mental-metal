using MentalMetal.Application.Common;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class ArchiveInitiativeChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<ChatThreadDto?> HandleAsync(Guid initiativeId, Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Initiative
            || thread.Scope.InitiativeId != initiativeId)
            return null;

        thread.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.From(thread, includeMessages: false);
    }
}

public sealed class UnarchiveInitiativeChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<ChatThreadDto?> HandleAsync(Guid initiativeId, Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Initiative
            || thread.Scope.InitiativeId != initiativeId)
            return null;

        thread.Unarchive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.From(thread, includeMessages: false);
    }
}
