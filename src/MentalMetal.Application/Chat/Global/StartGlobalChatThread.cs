using MentalMetal.Application.Common;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Chat.Global;

public sealed class StartGlobalChatThreadHandler(
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<GlobalChatThreadDto> HandleAsync(CancellationToken cancellationToken)
    {
        var thread = ChatThread.Start(currentUser.UserId, ContextScope.Global());
        await threads.AddAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GlobalChatThreadDto.From(thread, includeMessages: true);
    }
}
