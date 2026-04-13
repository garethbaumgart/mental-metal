using MentalMetal.Application.Common;
using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Chat.Global;

public sealed class RenameGlobalChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<GlobalChatThreadDto?> HandleAsync(
        Guid threadId,
        RenameChatThreadRequest request,
        CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Global)
            return null;

        thread.Rename(request.Title, ChatThread.RenameSourceManual);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GlobalChatThreadDto.From(thread, includeMessages: false);
    }
}
