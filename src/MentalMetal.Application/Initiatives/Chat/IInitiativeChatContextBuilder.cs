using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Initiatives.Chat;

public interface IInitiativeChatContextBuilder
{
    /// <summary>
    /// Assemble a bounded, user-scoped context payload for an initiative-scoped chat turn.
    /// Returns null if the initiative is not found or belongs to a different user.
    /// </summary>
    Task<InitiativeChatContextPayload?> BuildAsync(
        Guid userId,
        Guid initiativeId,
        string userQuestion,
        IReadOnlyList<ChatMessage> recentMessages,
        CancellationToken cancellationToken);
}
