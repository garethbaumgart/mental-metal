using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class GetInitiativeChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser)
{
    public async Task<ChatThreadDto?> HandleAsync(Guid initiativeId, Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Initiative
            || thread.Scope.InitiativeId != initiativeId)
            return null;

        return ChatThreadDto.From(thread, includeMessages: true);
    }
}
