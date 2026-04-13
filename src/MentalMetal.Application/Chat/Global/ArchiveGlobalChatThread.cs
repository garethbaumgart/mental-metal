using MentalMetal.Application.Common;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Chat.Global;

public sealed class ArchiveGlobalChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<GlobalChatThreadDto?> HandleAsync(Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Global)
            return null;

        thread.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GlobalChatThreadDto.From(thread, includeMessages: false);
    }
}

public sealed class UnarchiveGlobalChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<GlobalChatThreadDto?> HandleAsync(Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Global)
            return null;

        thread.Unarchive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GlobalChatThreadDto.From(thread, includeMessages: false);
    }
}
