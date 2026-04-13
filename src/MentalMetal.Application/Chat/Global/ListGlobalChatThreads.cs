using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Chat.Global;

public sealed class ListGlobalChatThreadsHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser)
{
    public async Task<IReadOnlyList<GlobalChatThreadSummaryDto>> HandleAsync(
        ChatThreadStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var effectiveStatus = statusFilter ?? ChatThreadStatus.Active;
        var list = await threads.ListGlobalAsync(currentUser.UserId, effectiveStatus, cancellationToken);
        return list.Select(GlobalChatThreadSummaryDto.From).ToList();
    }
}
