using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Chat.Global;

public interface IGlobalChatContextBuilder
{
    /// <summary>
    /// Assemble a bounded, user-scoped context payload for a global chat turn from the
    /// classified intents and any resolved entity hints.
    /// </summary>
    Task<GlobalChatContextPayload> BuildAsync(
        Guid userId,
        IntentSet intents,
        IReadOnlyList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken);
}
