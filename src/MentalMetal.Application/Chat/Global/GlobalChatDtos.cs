using MentalMetal.Application.Initiatives.Chat;
using MentalMetal.Domain.ChatThreads;

namespace MentalMetal.Application.Chat.Global;

/// <summary>
/// Re-uses the message / source-reference DTOs from initiative-ai-chat to keep wire shape
/// identical across the two chat surfaces. The thread DTO carries the same fields plus
/// the (always Global) ContextScopeType for clarity on the wire.
/// </summary>
public sealed record GlobalChatThreadDto(
    Guid Id,
    Guid UserId,
    string ContextScopeType,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    int MessageCount,
    IReadOnlyList<ChatMessageDto> Messages)
{
    public static GlobalChatThreadDto From(ChatThread t, bool includeMessages) => new(
        t.Id, t.UserId, t.Scope.Type.ToString(), t.Title, t.Status.ToString(),
        t.CreatedAt, t.LastMessageAt, t.MessageCount,
        includeMessages ? t.Messages.Select(ChatMessageDto.From).ToList() : []);
}

public sealed record GlobalChatThreadSummaryDto(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    int MessageCount)
{
    public static GlobalChatThreadSummaryDto From(ChatThread t) =>
        new(t.Id, t.Title, t.Status.ToString(), t.CreatedAt, t.LastMessageAt, t.MessageCount);
}

public sealed record PostGlobalChatMessageResponse(
    ChatMessageDto UserMessage,
    ChatMessageDto AssistantMessage,
    GlobalChatThreadSummaryDto Thread);
