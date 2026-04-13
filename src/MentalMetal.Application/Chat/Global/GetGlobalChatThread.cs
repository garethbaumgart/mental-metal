using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Chat.Global;

public sealed class GetGlobalChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser)
{
    public async Task<GlobalChatThreadDto?> HandleAsync(Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Global)
            return null;

        return GlobalChatThreadDto.From(thread, includeMessages: true);
    }
}
