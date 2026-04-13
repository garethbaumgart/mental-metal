using MentalMetal.Application.Common;
using MentalMetal.Domain.ChatThreads;
using MentalMetal.Domain.Users;

namespace MentalMetal.Application.Initiatives.Chat;

public sealed class PostInitiativeChatMessageHandler(
    IChatThreadRepository threads,
    IInitiativeChatCompletionService completionService,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
{
    public sealed class ArchivedThreadException() : InvalidOperationException("Cannot post to an archived thread.");

    public async Task<PostChatMessageResponse?> HandleAsync(
        Guid initiativeId,
        Guid threadId,
        PostChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        var thread = await threads.GetByIdAsync(threadId, cancellationToken);
        if (thread is null
            || thread.UserId != currentUser.UserId
            || thread.Scope.Type != ContextScopeType.Initiative
            || thread.Scope.InitiativeId != initiativeId)
            return null;

        if (thread.Status != ChatThreadStatus.Active)
            throw new ArchivedThreadException();

        // Content validation bubbles ArgumentException upwards so the endpoint returns HTTP 400.
        var userMessage = thread.AppendUserMessage(request.Content);
        await completionService.GenerateReplyAsync(currentUser.UserId, thread, cancellationToken);

        // The reply may be either an Assistant message (success / AiProviderException fallback)
        // or a System message (TasteLimitExceededException). Either way it is the last message.
        var replyMessage = thread.Messages[^1];

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PostChatMessageResponse(
            ChatMessageDto.From(userMessage),
            ChatMessageDto.From(replyMessage));
    }
}
