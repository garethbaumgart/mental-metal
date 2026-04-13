using MentalMetal.Application.Common;
using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Chat.Global;

public sealed class PostGlobalChatMessageHandler(
    IChatThreadRepository threads,
    IGlobalChatCompletionService completionService,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public sealed class ArchivedThreadException() : InvalidOperationException("Cannot post to an archived thread.");

    public async Task<PostGlobalChatMessageResponse?> HandleAsync(
        Guid threadId,
        PostChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Global)
            return null;

        if (thread.Status != ChatThreadStatus.Active)
            throw new ArchivedThreadException();

        var userMessage = thread.AppendUserMessage(request.Content);
        await completionService.GenerateReplyAsync(currentUser.UserId, thread, cancellationToken);

        var replyMessage = thread.Messages[^1];
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PostGlobalChatMessageResponse(
            ChatMessageDto.From(userMessage),
            ChatMessageDto.From(replyMessage),
            GlobalChatThreadSummaryDto.From(thread));
    }
}
