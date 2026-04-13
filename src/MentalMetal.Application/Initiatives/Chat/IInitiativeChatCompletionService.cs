using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Initiatives.Chat;

public interface IInitiativeChatCompletionService
{
    /// <summary>
    /// Run one chat turn: assemble context, call the AI provider, parse/sanitise the reply,
    /// append either the assistant reply or a friendly fallback to the given thread. The caller
    /// is responsible for appending the user message before invoking and for persisting after.
    /// </summary>
    Task GenerateReplyAsync(
        Guid userId,
        ChatThread thread,
        CancellationToken cancellationToken);
}
