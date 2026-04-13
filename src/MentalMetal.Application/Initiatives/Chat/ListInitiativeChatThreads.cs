using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class ListInitiativeChatThreadsHandler(
    IInitiativeRepository initiatives,
    IChatThreadRepository threads,
    ICurrentUserService currentUser)
{
    public async Task<IReadOnlyList<ChatThreadSummaryDto>?> HandleAsync(
        Guid initiativeId,
        ChatThreadStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var initiative = await initiatives.GetByIdAsync(initiativeId, cancellationToken);
        if (initiative is null || initiative.UserId != currentUser.UserId)
            return null;

        var effectiveStatus = statusFilter ?? ChatThreadStatus.Active;
        var list = await threads.ListForInitiativeAsync(currentUser.UserId, initiativeId, effectiveStatus, cancellationToken);
        return list.Select(ChatThreadSummaryDto.From).ToList();
    }
}
