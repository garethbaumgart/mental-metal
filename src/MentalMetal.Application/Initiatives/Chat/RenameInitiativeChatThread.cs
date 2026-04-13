using MentalMetal.Application.Common;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class RenameInitiativeChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<ChatThreadDto?> HandleAsync(
        Guid initiativeId,
        Guid threadId,
        RenameChatThreadRequest request,
        CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Initiative
            || thread.Scope.InitiativeId != initiativeId)
            return null;

        thread.Rename(request.Title, ChatThread.RenameSourceManual);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.From(thread, includeMessages: false);
    }
}
