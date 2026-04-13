using MentalMetal.Application.Common;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Initiatives;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class StartInitiativeChatThreadHandler(
    IInitiativeRepository initiatives,
    IChatThreadRepository threads,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<ChatThreadDto?> HandleAsync(Guid initiativeId, CancellationToken cancellationToken)
    {
        var initiative = await initiatives.GetByIdAsync(initiativeId, cancellationToken);
        if (initiative is null || initiative.UserId != currentUser.UserId)
            return null;

        var thread = ChatThread.Start(currentUser.UserId, ContextScope.Initiative(initiativeId));
        await threads.AddAsync(thread, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ChatThreadDto.From(thread, includeMessages: true);
    }
}
