using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Chat.Global;

public interface IGlobalChatCompletionService
{
    /// <summary>
    /// Run one global-chat turn: classify the latest user question, assemble context,
    /// call the AI provider, sanitise / persist the assistant reply onto the thread.
    /// The caller appends the user message before calling, and persists after.
    /// </summary>
    Task GenerateReplyAsync(Guid userId, ChatThread thread, CancellationToken cancellationToken);
}
